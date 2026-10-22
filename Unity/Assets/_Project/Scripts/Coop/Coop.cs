using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace SAE
{
    public enum CoopRole { Solo, Host, Guest }

    // Le coop, par Internet : en pleine partie, un joueur l'OUVRE EN COOP depuis la pause (casque ou PC) et choisit un
    // mot de passe ; la partie reçoit un code à 6 caractères. Jusqu'à 3 autres la REJOIGNENT depuis le menu principal
    // en tapant le code et le mot de passe, de n'importe quel réseau (casque ou PC).
    //
    // L'hôte fait tourner la vraie partie : vagues, ballons, tirs des singes, bourse, bibliothèque, singes posés.
    // Un invité affiche ce que l'hôte lui envoie, et lui demande d'agir à sa place : « j'ai touché le ballon 42 »,
    // « j'ai vendu une banane », « je pose ce singe ici ». Chacun garde son arc, sa bananeraie et sa sauvegarde.
    //
    // Le réseau : Unity Relay, un serveur relais gratuit de Unity. Deux appareils sur des réseaux différents ne peuvent pas
    // se joindre directement (box, pare-feu de l'IUT) ; ils se connectent tous les deux AU RELAIS, qui fait passer les
    // paquets. Le code de la partie, c'est le « code de connexion » que le relais donne à l'hôte.
    // On passe par WebSocket sécurisé (wss, port 443, comme un site web) : c'est ce qui passe le mieux les pare-feu.
    // Nos messages restent les mêmes (CoopPacket) :
    //   - ce qui change sans arrêt (têtes et mains, ballons, état) part en continu ;
    //   - les demandes de l'invité sont numérotées et renvoyées tant que l'hôte n'a pas répondu « reçu » (Ack) :
    //     aucune n'est perdue, aucune n'est faite deux fois, et elles sont faites dans l'ordre.
    public class Coop : MonoBehaviour
    {
        public const int MaxGuests = 3;
        const string Protocol = "wss";         // WebSocket sécurisé (voir plus haut)
        const float PoseInterval = 0.05f;      // 20 fois par seconde
        const float StateInterval = 0.2f;      // 5 fois par seconde
        const float BalloonInterval = 1f / 15f;
        const float ResendInterval = 0.15f;    // une demande sans réponse repart au bout de 0,15 s

        public static CoopRole Role { get; private set; }
        public static bool IsHost => Role == CoopRole.Host;
        public static bool IsGuest => Role == CoopRole.Guest;
        public static bool Busy { get; private set; }         // on attend le serveur de Unity (connexion, code)
        public static bool Connected { get; private set; }    // invité : l'hôte a dit oui
        public static byte LocalId { get; private set; }      // 0 = l'hôte, 1 à 3 = les invités
        public static string Status { get; private set; } = "";
        public static string Code { get; private set; } = "";  // le code de la partie (6 caractères, donné par le relais)
        public static string Password { get; private set; } = "";

        // Invité : le numéro de sa dernière demande qui change l'état (bourse, bibliothèque, singes posés).
        // Tant que l'hôte ne l'a pas faite, on n'écoute pas son état : il nous ferait revenir en arrière une seconde.
        public static int LastStateRequest { get; private set; }

        static Coop instance;

        class Peer
        {
            public NetworkConnection connection;
            public byte id;
            public bool welcomed;     // il a donné le bon mot de passe
            public int lastRequest;   // la dernière demande faite pour lui (elles arrivent dans l'ordre)
        }

        NetworkDriver driver;
        NetworkPipeline pipeline;                       // découpe les gros paquets (l'état, 30 ballons) en morceaux
        readonly List<Peer> peers = new List<Peer>();   // hôte : les invités
        NetworkConnection host;                         // invité : la connexion à l'hôte (par le relais)
        bool stateReceived;                             // invité : l'état de l'hôte a remplacé le nôtre

        int nextRequest = 1;
        readonly List<(int number, byte[] packet)> pending = new List<(int, byte[])>();   // demandes pas encore reçues
        float nextResend, nextPose, nextState, nextBalloons;

        readonly Dictionary<byte, CoopAvatar> avatars = new Dictionary<byte, CoopAvatar>();
        Arrow arrowPrefab;   // pour montrer les flèches des autres

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState()
        {
            Role = CoopRole.Solo;
            Busy = Connected = false;
            LocalId = 0;
            Status = Code = Password = "";
            LastStateRequest = 0;
        }

        // --- Démarrer et arrêter ---

        // OUVRIR EN COOP (menu pause) : on réserve une place sur le relais, il nous donne le code de la partie
        public static async void Host(string password)
        {
            if (Role != CoopRole.Solo || Busy) return;
            Busy = true;
            Status = "Connexion au serveur de Unity…";
            try
            {
                await SignIn();
                var allocation = await RelayService.Instance.CreateAllocationAsync(MaxGuests);
                string code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                var relay = allocation.ToRelayServerData(Protocol);
                Ensure().Open(ref relay, listen: true);
                Role = CoopRole.Host;
                LocalId = 0;
                Code = code;
                Password = password;
                Status = "En attente d'un joueur…";
            }
            catch (Exception e) { Failed(e); }   // pas d'Internet, projet pas relié à Unity Cloud… : on le dit, le jeu continue en solo
            Busy = false;
        }

        // REJOINDRE (menu principal) : le relais retrouve la partie à partir de son code, puis on donne le mot de passe
        public static async void Join(string code, string password)
        {
            if (Role != CoopRole.Solo || Busy) return;
            Busy = true;
            Status = $"Recherche de la partie {code}…";
            try
            {
                await SignIn();
                var allocation = await RelayService.Instance.JoinAllocationAsync(code);
                var relay = allocation.ToRelayServerData(Protocol);
                Ensure().Open(ref relay, listen: false);
                Role = CoopRole.Guest;
                Connected = false;
                Code = code;
                Password = password;
                Status = "Connexion à la partie…";
            }
            catch (RelayServiceException e) when (e.Reason == RelayExceptionReason.JoinCodeNotFound || e.Reason == RelayExceptionReason.InvalidArgument
                                                   || e.Reason == RelayExceptionReason.InvalidRequest || e.Reason == RelayExceptionReason.EntityNotFound)
            {
                Status = $"Aucune partie {code} : vérifie le code";
            }
            catch (Exception e) { Failed(e); }   // pas d'Internet, projet pas relié à Unity Cloud… : on le dit, le jeu continue en solo
            Busy = false;
        }

        // Le relais de Unity demande d'être identifié : un compte anonyme, créé tout seul la première fois
        static async Task SignIn()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized) await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        static void Failed(Exception e)
        {
            Debug.LogWarning("Coop : " + e.Message);
            Status = "Pas d'Internet, ou le serveur de Unity ne répond pas";
        }

        public static void Leave() => Leave("");

        static void Leave(string reason)
        {
            if (Role == CoopRole.Solo) return;
            var i = instance;
            var bye = CoopPacket.Bytes(CoopPacket.Begin(CoopMsg.Bye));
            if (IsHost) foreach (var p in i.peers) i.Send(bye, p.connection);
            else i.Send(bye, i.host);
            i.driver.ScheduleUpdate().Complete();   // le « au revoir » part avant qu'on coupe

            bool reload = IsGuest && i.stateReceived;
            i.driver.Dispose();
            i.peers.Clear();
            i.host = default;
            i.pending.Clear();
            i.stateReceived = false;
            foreach (var a in i.avatars.Values) if (a) Destroy(a.gameObject);
            i.avatars.Clear();
            CoopBalloons.Clear();
            Role = CoopRole.Solo;
            Connected = false;
            Status = reason;
            // L'invité jouait avec l'état de l'hôte : il retrouve sa propre partie, telle qu'il l'avait sauvegardée
            if (reload) SaveGame.ReloadFromDisk();
        }

        static Coop Ensure()
        {
            if (!instance)
            {
                var go = new GameObject("Coop");
                DontDestroyOnLoad(go);   // garde la connexion quand une scène se recharge (nouvelle partie)
                instance = go.AddComponent<Coop>();
            }
            return instance;
        }

        // Le « driver » de Unity Transport : il parle au relais. L'hôte attend les invités (Listen), l'invité se connecte.
        void Open(ref RelayServerData relay, bool listen)
        {
            var settings = new NetworkSettings();
            settings.WithRelayParameters(ref relay);
            driver = NetworkDriver.Create(new WebSocketNetworkInterface(), settings);
            pipeline = driver.CreatePipeline(typeof(FragmentationPipelineStage));
            if (listen)
            {
                driver.Bind(NetworkEndpoint.AnyIpv4);
                driver.Listen();
            }
            else host = driver.Connect();
            nextRequest = 1;
            LastStateRequest = 0;
        }

        void OnApplicationQuit() => Leave();
        void OnDestroy() { if (driver.IsCreated) driver.Dispose(); }

        // --- Chaque image : lire ce qui est arrivé, envoyer ce qui doit partir ---

        void Update()
        {
            if (Role == CoopRole.Solo) return;
            driver.ScheduleUpdate().Complete();   // le driver envoie et reçoit (il travaille en tâche de fond, on l'attend)
            Receive();
            if (Role == CoopRole.Solo) return;    // on vient de quitter (l'hôte est parti)
            float now = Time.unscaledTime;        // le menu arrête le jeu (timeScale = 0), pas le réseau

            if (now >= nextPose) { nextPose = now + PoseInterval; SendPose(); }

            if (IsHost)
            {
                if (now >= nextState)
                {
                    nextState = now + StateInterval;
                    foreach (var p in peers) if (p.welcomed) Send(CoopState.Packet(p.lastRequest), p.connection);
                }
                if (now >= nextBalloons)
                {
                    nextBalloons = now + BalloonInterval;
                    foreach (var p in peers)
                        if (p.welcomed)
                            foreach (var packet in CoopBalloons.Packets(p.lastRequest)) Send(packet, p.connection);
                }
                int players = peers.FindAll(p => p.welcomed).Count + 1;
                Status = players == 1 ? "En attente d'un joueur…" : $"{players} joueurs dans la partie";
            }
            else
            {
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
            if (IsHost)
            {
                NetworkConnection c;
                while ((c = driver.Accept()) != default)   // un nouveau joueur arrive : il doit encore dire bonjour
                    peers.Add(new Peer { connection = c, id = FreeId() });
            }

            NetworkEvent.Type type;
            while (Role != CoopRole.Solo && (type = driver.PopEvent(out var connection, out var reader)) != NetworkEvent.Type.Empty)
            {
                switch (type)
                {
                    case NetworkEvent.Type.Connect:   // invité : on est relié à l'hôte, on donne le mot de passe
                        var hello = CoopPacket.Begin(CoopMsg.Hello);
                        hello.Write(Password);
                        Send(CoopPacket.Bytes(hello), host);
                        break;
                    case NetworkEvent.Type.Disconnect:
                        if (IsHost) RemovePeer(peers.Find(p => p.connection == connection));
                        else Leave(Connected ? "Connexion perdue avec l'hôte" : "Impossible de joindre la partie");
                        break;
                    case NetworkEvent.Type.Data:
                        var data = new byte[reader.Length];
                        reader.ReadBytes(data);
                        if (!CoopPacket.Open(data, out var r, out var msg)) break;
                        try
                        {
                            if (IsHost) HandleAsHost(msg, r, connection, data);
                            else HandleAsGuest(msg, r);
                        }
                        catch (EndOfStreamException) { }   // paquet trop court (autre version du jeu) : ignoré
                        break;
                }
            }
        }

        // --- L'hôte ---

        void HandleAsHost(CoopMsg type, BinaryReader r, NetworkConnection from, byte[] data)
        {
            var peer = peers.Find(p => p.connection == from);
            if (peer == null) return;

            if (type == CoopMsg.Hello)
            {
                if (r.ReadString() != Password) { Refuse(peer, "Mot de passe incorrect"); return; }
                peer.welcomed = true;
                var welcome = CoopPacket.Begin(CoopMsg.Welcome);
                welcome.Write(peer.id);
                Send(CoopPacket.Bytes(welcome), from);
                Sounds.Play(Sounds.Bouton);
                return;
            }
            if (!peer.welcomed) return;   // pas encore le bon mot de passe : on n'écoute rien d'autre

            switch (type)
            {
                case CoopMsg.Bye:
                    RemovePeer(peer);
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

        // Non (mauvais mot de passe) : on le lui dit, puis on coupe sa connexion
        void Refuse(Peer peer, string reason)
        {
            var w = CoopPacket.Begin(CoopMsg.Refused);
            w.Write(reason);
            Send(CoopPacket.Bytes(w), peer.connection);
            driver.ScheduleUpdate().Complete();
            driver.Disconnect(peer.connection);
            peers.Remove(peer);
        }

        byte FreeId()
        {
            for (byte id = 1; ; id++)
                if (!peers.Exists(p => p.id == id)) return id;
        }

        void RemovePeer(Peer p)
        {
            if (p == null) return;
            if (avatars.TryGetValue(p.id, out var a) && a) Destroy(a.gameObject);
            avatars.Remove(p.id);
            peers.Remove(p);
        }

        void Relay(byte[] data, Peer except)
        {
            foreach (var p in peers) if (p != except && p.welcomed) Send(data, p.connection);
        }

        // --- L'invité ---

        void HandleAsGuest(CoopMsg type, BinaryReader r)
        {
            switch (type)
            {
                case CoopMsg.Refused: Leave(r.ReadString()); break;
                case CoopMsg.Welcome:
                    LocalId = r.ReadByte();
                    Connected = true;
                    Status = $"Connecté · tu es le joueur {LocalId + 1}";
                    Tutorial.RestoreDone();   // l'invité rejoint une partie déjà commencée : pas de tutoriel
                    Sounds.Play(Sounds.Bouton);
                    break;
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
        static int Request(CoopRequest kind, bool changesState, Action<BinaryWriter> write)
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
            if (IsHost) { foreach (var p in peers) if (p.welcomed) Send(packet, p.connection); }
            else Send(packet, host);
        }

        void Send(byte[] packet, NetworkConnection to)
        {
            if (!driver.IsCreated || driver.GetConnectionState(to) != NetworkConnection.State.Connected) return;
            if (driver.BeginSend(pipeline, to, out var writer) != 0) return;   // file d'envoi pleine : le paquet suivant repartira
            writer.WriteBytes(packet);
            driver.EndSend(writer);
        }
    }
}
