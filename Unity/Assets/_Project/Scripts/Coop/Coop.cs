using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace SAE
{
    public enum CoopRole { Solo, Host, Guest }

    // Le coop en réseau local (même Wi-Fi) : en pleine partie, un joueur l'OUVRE EN COOP depuis la pause (casque ou PC) ;
    // elle reçoit un code, il choisit un mot de passe. Jusqu'à 3 autres la REJOIGNENT depuis le menu principal en tapant
    // le code et le mot de passe (casque ou PC).
    // L'hôte fait tourner la vraie partie : vagues, ballons, tirs des singes, bourse, bibliothèque, singes posés.
    // Un invité affiche ce que l'hôte lui envoie, et lui demande d'agir à sa place : « j'ai touché le ballon 42 »,
    // « j'ai vendu une banane », « je pose ce singe ici ». Chacun garde son arc, sa bananeraie et sa sauvegarde.
    //
    // Le réseau : de l'UDP tout simple, lu dans Update (pas de thread). Pour trouver l'hôte, l'invité envoie « Hello »
    // à tout le réseau (broadcast) ; l'hôte répond « Welcome ». Ensuite :
    //   - ce qui change sans arrêt (têtes et mains, ballons, état) part en continu : un paquet perdu est vite remplacé ;
    //   - les demandes de l'invité sont numérotées et renvoyées tant que l'hôte n'a pas répondu « reçu » (Ack) :
    //     aucune n'est perdue, aucune n'est faite deux fois, et elles sont faites dans l'ordre.
    public class Coop : MonoBehaviour
    {
        public const int Port = 47777;
        public const int MaxGuests = 3;
        const float PoseInterval = 0.05f;      // 20 fois par seconde
        const float StateInterval = 0.2f;      // 5 fois par seconde
        const float BalloonInterval = 1f / 15f;
        const float HelloInterval = 1f;
        const float ResendInterval = 0.15f;    // une demande sans réponse repart au bout de 0,15 s
        const float Timeout = 5f;              // plus rien reçu depuis 5 s : le joueur est parti
        const float SearchTime = 10f;          // invité : on cherche la partie pendant 10 s, puis on abandonne

        public static CoopRole Role { get; private set; }
        public static bool IsHost => Role == CoopRole.Host;
        public static bool IsGuest => Role == CoopRole.Guest;
        public static bool Connected { get; private set; }   // invité : l'hôte a répondu
        public static byte LocalId { get; private set; }     // 0 = l'hôte, 1 à 3 = les invités
        public static string Status { get; private set; } = "";
        public static int Code { get; private set; }            // le code de la partie (4 chiffres)
        public static string Password { get; private set; } = "";
        // Invité : le numéro de sa dernière demande qui change l'état (bourse, bibliothèque, singes posés).
        // Tant que l'hôte ne l'a pas faite, on n'écoute pas son état : il nous ferait revenir en arrière une seconde.
        public static int LastStateRequest { get; private set; }

        static Coop instance;

        class Peer
        {
            public IPEndPoint endPoint;
            public byte id;
            public float lastHeard;
            public int lastRequest;   // la dernière demande faite pour lui (elles arrivent dans l'ordre)
        }

        UdpClient udp;
        readonly List<Peer> peers = new List<Peer>();   // hôte : les invités
        IPEndPoint host;                                // invité : l'hôte
        float hostLastHeard;
        bool stateReceived;                             // invité : l'état de l'hôte a remplacé le nôtre
        string address;                                 // hôte : son adresse sur le Wi-Fi

        int nextRequest = 1;
        readonly List<(int number, byte[] packet)> pending = new List<(int, byte[])>();   // demandes pas encore reçues
        float nextResend, nextPose, nextState, nextBalloons, nextHello, searchUntil;

        readonly Dictionary<byte, CoopAvatar> avatars = new Dictionary<byte, CoopAvatar>();
        Arrow arrowPrefab;   // pour montrer les flèches des autres

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState()
        {
            Role = CoopRole.Solo;
            Connected = false;
            LocalId = 0;
            Status = "";
            LastStateRequest = 0;
        }

        // --- Démarrer et arrêter ---

        // OUVRIR EN COOP (menu pause, en pleine partie) : la partie reçoit un code à 4 chiffres, tiré au hasard,
        // et garde le mot de passe choisi. Les autres joueurs tapent les deux dans le menu principal (REJOINDRE).
        public static void Host(string password)
        {
            if (Role != CoopRole.Solo || !Open(Port)) return;
            Role = CoopRole.Host;
            LocalId = 0;
            Code = Random.Range(1000, 10000);
            Password = password;
            instance.address = LocalAddress();   // lue une fois : elle sert au texte du panneau (Update)
        }

        // REJOINDRE (menu principal) : on cherche sur le Wi-Fi la partie qui a ce code, et on donne le mot de passe
        public static void Join(int code, string password)
        {
            if (Role != CoopRole.Solo || !Open(0)) return;   // 0 : le système choisit un port libre
            Role = CoopRole.Guest;
            Connected = false;
            Code = code;
            Password = password;
            instance.searchUntil = Time.unscaledTime + SearchTime;
            Status = $"Recherche de la partie {code}…";
        }

        public static void Leave() => Leave("");

        static void Leave(string reason)
        {
            if (Role == CoopRole.Solo) return;
            var i = instance;
            var bye = CoopPacket.Bytes(CoopPacket.Begin(CoopMsg.Bye));
            if (IsHost) foreach (var p in i.peers) i.Send(bye, p.endPoint);
            else if (i.host != null) i.Send(bye, i.host);

            bool reload = IsGuest && i.stateReceived;
            i.udp.Close();
            i.udp = null;
            i.peers.Clear();
            i.host = null;
            i.pending.Clear();
            i.stateReceived = false;
            foreach (var a in i.avatars.Values) if (a) Destroy(a.gameObject);
            i.avatars.Clear();
            CoopBalloons.Clear();
            HoldMulticast(false);
            Role = CoopRole.Solo;
            Connected = false;
            Status = reason;
            // L'invité jouait avec l'état de l'hôte : il retrouve sa propre partie, telle qu'il l'avait sauvegardée
            if (reload) SaveGame.ReloadFromDisk();
        }

        static bool Open(int port)
        {
            if (!instance)
            {
                var go = new GameObject("Coop");
                DontDestroyOnLoad(go);   // garde la connexion quand une scène se recharge (nouvelle partie)
                instance = go.AddComponent<Coop>();
            }
            try
            {
                instance.udp = new UdpClient(port) { EnableBroadcast = true };
            }
            catch (SocketException e)
            {
                Status = $"Réseau indisponible ({e.SocketErrorCode})";
                return false;
            }
            // Windows : sans ça, un paquet vers un joueur parti fait planter la lecture suivante (erreur 10054)
            if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
                instance.udp.Client.IOControl(-1744830452, new byte[] { 0 }, null);
            instance.nextRequest = 1;
            LastStateRequest = 0;
            HoldMulticast(true);
            return true;
        }

        void OnApplicationQuit() => Leave();

        // --- Chaque image : lire ce qui est arrivé, envoyer ce qui doit partir ---

        void Update()
        {
            if (udp == null) return;
            Receive();
            if (udp == null) return;   // on vient de quitter (l'hôte est parti)
            float now = Time.unscaledTime;   // le menu arrête le jeu (timeScale = 0), pas le réseau

            if (now >= nextPose) { nextPose = now + PoseInterval; SendPose(); }

            if (IsHost)
            {
                // Un invité silencieux depuis 5 s est parti (casque éteint, Wi-Fi perdu)
                for (int k = peers.Count - 1; k >= 0; k--)
                    if (now - peers[k].lastHeard > Timeout) { RemovePeer(peers[k]); peers.RemoveAt(k); }
                if (now >= nextState)
                {
                    nextState = now + StateInterval;
                    foreach (var p in peers) Send(CoopState.Packet(p.lastRequest), p.endPoint);
                }
                if (now >= nextBalloons)
                {
                    nextBalloons = now + BalloonInterval;
                    foreach (var p in peers)
                        foreach (var packet in CoopBalloons.Packets(p.lastRequest)) Send(packet, p.endPoint);
                }
                Status = peers.Count == 0 ? $"En attente d'un joueur… ({address})" : $"{peers.Count + 1} joueurs dans la partie";
            }
            else
            {
                if (!Connected && now > searchUntil) { Leave($"Aucune partie {Code} sur ce Wi-Fi"); return; }
                if (!Connected && now >= nextHello)
                {
                    nextHello = now + HelloInterval;
                    var hello = CoopPacket.Begin(CoopMsg.Hello);
                    hello.Write(Code);
                    hello.Write(Password);
                    Send(CoopPacket.Bytes(hello), new IPEndPoint(IPAddress.Broadcast, Port));
                }
                if (Connected && now - hostLastHeard > Timeout) { Leave("Connexion perdue avec l'hôte"); return; }
                if (pending.Count > 0 && now >= nextResend)
                {
                    nextResend = now + ResendInterval;
                    foreach (var r in pending) Send(r.packet, host);
                }
                CoopBalloons.Tick();
            }
        }

        void Receive()
        {
            while (udp != null && udp.Available > 0)
            {
                IPEndPoint from = null;
                byte[] data;
                try { data = udp.Receive(ref from); }
                catch (SocketException) { continue; }   // un paquet abîmé ou refusé : on passe au suivant
                if (!CoopPacket.Open(data, out var r, out var type)) continue;
                try
                {
                    if (IsHost) HandleAsHost(type, r, from, data);
                    else HandleAsGuest(type, r, from);
                }
                catch (EndOfStreamException) { }   // paquet trop court (autre version du jeu) : ignoré
            }
        }

        // --- L'hôte ---

        void HandleAsHost(CoopMsg type, BinaryReader r, IPEndPoint from, byte[] data)
        {
            var peer = peers.Find(p => p.endPoint.Equals(from));
            if (type == CoopMsg.Hello)
            {
                if (r.ReadInt32() != Code) return;   // il cherche une autre partie (un autre groupe sur le même Wi-Fi)
                if (r.ReadString() != Password) { Refuse(from, "Mot de passe incorrect"); return; }
                if (peer == null)
                {
                    if (peers.Count >= MaxGuests) { Refuse(from, "La partie est pleine (4 joueurs)"); return; }
                    peer = new Peer { endPoint = from, id = FreeId() };
                    peers.Add(peer);
                    Sounds.Play(Sounds.Bouton);
                }
                var welcome = CoopPacket.Begin(CoopMsg.Welcome);
                welcome.Write(peer.id);
                Send(CoopPacket.Bytes(welcome), from);
            }
            if (peer == null) return;   // pas encore dit bonjour
            peer.lastHeard = Time.unscaledTime;

            switch (type)
            {
                case CoopMsg.Bye:
                    RemovePeer(peer);
                    peers.Remove(peer);
                    break;
                case CoopMsg.Pose:
                    ReadPose(r);
                    Relay(data, peer);   // les autres invités le voient aussi
                    break;
                case CoopMsg.Shot:
                    ReadShot(r);
                    Relay(data, peer);
                    break;
                case CoopMsg.Request:
                    int number = r.ReadInt32();
                    if (number == peer.lastRequest + 1)   // la suivante : on la fait (une déjà faite ou en avance est ignorée)
                    {
                        CoopHost.Handle((CoopRequest)r.ReadByte(), r);
                        peer.lastRequest = number;
                    }
                    var ack = CoopPacket.Begin(CoopMsg.Ack);
                    ack.Write(peer.lastRequest);
                    Send(CoopPacket.Bytes(ack), from);
                    break;
            }
        }

        void Refuse(IPEndPoint to, string reason)
        {
            var w = CoopPacket.Begin(CoopMsg.Refused);
            w.Write(reason);
            Send(CoopPacket.Bytes(w), to);
        }

        byte FreeId()
        {
            for (byte id = 1; ; id++)
                if (!peers.Exists(p => p.id == id)) return id;
        }

        void RemovePeer(Peer p)
        {
            if (avatars.TryGetValue(p.id, out var a) && a) Destroy(a.gameObject);
            avatars.Remove(p.id);
        }

        void Relay(byte[] data, Peer except)
        {
            foreach (var p in peers) if (p != except) Send(data, p.endPoint);
        }

        // --- L'invité ---

        void HandleAsGuest(CoopMsg type, BinaryReader r, IPEndPoint from)
        {
            if (type == CoopMsg.Refused && !Connected) { Leave(r.ReadString()); return; }
            if (type == CoopMsg.Welcome && !Connected)
            {
                host = from;
                LocalId = r.ReadByte();
                Connected = true;
                Status = $"Connecté · tu es le joueur {LocalId + 1}";
                Tutorial.RestoreDone();   // l'invité rejoint une partie déjà commencée : pas de tutoriel
                Sounds.Play(Sounds.Bouton);
            }
            if (host == null || !from.Equals(host)) return;
            hostLastHeard = Time.unscaledTime;

            switch (type)
            {
                case CoopMsg.Bye: Leave("L'hôte a quitté la partie"); break;
                case CoopMsg.Pose: ReadPose(r); break;
                case CoopMsg.Shot: ReadShot(r); break;
                case CoopMsg.State: CoopState.Read(r); stateReceived = true; break;
                case CoopMsg.Balloons: CoopBalloons.Read(r); break;
                case CoopMsg.Ack:
                    int done = r.ReadInt32();
                    pending.RemoveAll(p => p.number <= done);
                    break;
            }
        }

        // Une demande à l'hôte. Renvoie son numéro (le ballon touché le garde : voir Balloon.ApplyRemote).
        static int Request(CoopRequest kind, bool changesState, System.Action<BinaryWriter> write)
        {
            if (!IsGuest || !Connected) return 0;
            var i = instance;
            int number = i.nextRequest++;
            var w = CoopPacket.Begin(CoopMsg.Request);
            w.Write(number);
            w.Write((byte)kind);
            write?.Invoke(w);
            var packet = CoopPacket.Bytes(w);
            i.pending.Add((number, packet));
            i.Send(packet, i.host);
            if (changesState) LastStateRequest = number;
            return number;
        }

        public static int SendHit(int balloon, int pierce) => Request(CoopRequest.Hit, false, w => { w.Write(balloon); w.Write(pierce); });
        public static void SendSlow(int balloon, float factor, float seconds) =>
            Request(CoopRequest.Slow, false, w => { w.Write(balloon); w.Write(factor); w.Write(seconds); });
        public static void SendEarn(int amount, int stakeBack) => Request(CoopRequest.Earn, true, w => { w.Write(amount); w.Write(stakeBack); });
        public static void SendSpend(int amount) => Request(CoopRequest.Spend, true, w => w.Write(amount));
        public static void SendInventory(InventoryOp op, Monkey m) =>
            Request(CoopRequest.Inventory, true, w => { w.Write((byte)op); w.Write((byte)m.type); w.Write((byte)m.level); });
        public static void SendLaunch() => Request(CoopRequest.Launch, false, null);

        // Poser, fusionner ou reprendre un singe : l'hôte refait la même chose avec le singe que l'invité tient
        public static void SendPlace(Vector2 pos, Monkey? held) => Request(CoopRequest.Place, true, w =>
        {
            w.Write(pos.x);
            w.Write(pos.y);
            w.Write(held.HasValue);
            w.Write((byte)(held?.type ?? 0));
            w.Write((byte)(held?.level ?? 0));
        });

        // --- Les joueurs : tête, mains, flèches ---

        void SendPose()
        {
            var rig = PlayerRig.Local;
            if (!rig || (IsGuest && !Connected)) return;
            var w = CoopPacket.Begin(CoopMsg.Pose);
            w.Write(LocalId);
            w.Write((byte)Levels.Current);
            w.Write(rig.IsVR);
            w.Write(rig.head.position); w.Write(rig.head.rotation);
            w.Write(rig.leftHand.position); w.Write(rig.leftHand.rotation);
            w.Write(rig.rightHand.position); w.Write(rig.rightHand.rotation);
            SendToOthers(CoopPacket.Bytes(w));
        }

        void ReadPose(BinaryReader r)
        {
            byte id = r.ReadByte();
            if (id == LocalId) return;
            var level = (Level)r.ReadByte();
            bool vr = r.ReadBoolean();
            var hp = r.ReadVector3(); var hr = r.ReadQuaternion();
            var lp = r.ReadVector3(); var lr = r.ReadQuaternion();
            var rp = r.ReadVector3(); var rr = r.ReadQuaternion();
            if (!avatars.TryGetValue(id, out var avatar) || !avatar)
                avatars[id] = avatar = CoopAvatar.Create(id, transform);
            avatar.SetPose(level, vr, hp, hr, lp, lr, rp, rr);
        }

        // Une flèche vient de partir (Arrow.Launch) : les autres la voient voler
        public static void SendShot(Vector3 from, Vector3 velocity)
        {
            if (Role == CoopRole.Solo || (IsGuest && !Connected)) return;
            var w = CoopPacket.Begin(CoopMsg.Shot);
            w.Write(LocalId);
            w.Write(from);
            w.Write(velocity);
            instance.SendToOthers(CoopPacket.Bytes(w));
        }

        void ReadShot(BinaryReader r)
        {
            r.ReadByte();   // le tireur (pas utile pour l'instant)
            var from = r.ReadVector3();
            var velocity = r.ReadVector3();
            if (!arrowPrefab)
            {
                var bow = FindAnyObjectByType<Bow>(FindObjectsInactive.Include);
                if (bow) arrowPrefab = bow.arrowPrefab;
            }
            if (!arrowPrefab) return;
            var arrow = Instantiate(arrowPrefab, from, Quaternion.LookRotation(velocity));
            arrow.visualOnly = true;   // seulement pour la voir : c'est chez le tireur qu'elle perce les ballons
            arrow.Launch(velocity);
        }

        // --- Envoyer ---

        void SendToOthers(byte[] packet)
        {
            if (IsHost) foreach (var p in peers) Send(packet, p.endPoint);
            else if (host != null) Send(packet, host);
        }

        void Send(byte[] packet, IPEndPoint to)
        {
            try { udp?.Send(packet, packet.Length, to); }
            catch (SocketException) { }   // réseau coupé un instant : le paquet suivant repartira
        }

        // L'adresse de cet appareil sur le Wi-Fi (affichée par l'hôte). On « connecte » une socket UDP vers une adresse
        // extérieure sans rien envoyer : le système choisit alors la bonne carte réseau, et on lit son adresse.
        static string LocalAddress()
        {
            try
            {
                using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                s.Connect("8.8.8.8", 65530);
                return ((IPEndPoint)s.LocalEndPoint).Address.ToString();
            }
            catch (SocketException) { return "adresse inconnue"; }
        }

        // Le Quest (Android) jette les paquets envoyés à tout le réseau, sauf si l'appli tient ce « verrou » :
        // sans lui, l'hôte au casque n'entendrait jamais le « Hello » des invités.
        static void HoldMulticast(bool on)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (on && multicastLock == null)
                {
                    using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    using var wifi = activity.Call<AndroidJavaObject>("getSystemService", "wifi");
                    multicastLock = wifi.Call<AndroidJavaObject>("createMulticastLock", "SAE501-coop");
                    multicastLock.Call("acquire");
                }
                else if (!on && multicastLock != null)
                {
                    multicastLock.Call("release");
                    multicastLock.Dispose();
                    multicastLock = null;
                }
            }
            catch (AndroidJavaException e) { Debug.LogWarning("Coop : verrou Wi-Fi refusé : " + e.Message); }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject multicastLock;
#endif
    }
}
