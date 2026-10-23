using System;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;   // ToRelayServerData
using UnityEngine;

namespace SAE
{
    public enum CoopRole { Solo, Host, Guest }

    // Les changements de la bibliothèque (GameState) qu'un invité demande à l'hôte
    public enum InventoryOp : byte { Add, Take, Unlock, ClearBoard }

    // Ce qu'un invité fait à la roulette (RouletteTable) : choisir la couleur, les jetons, lancer la roue
    public enum RouletteOp : byte { Select, AddOne, AddTen, AddHundred, AllIn, Clear, Spin }

    // Le coop, par Internet, avec Netcode for GameObjects (NGO), la solution réseau d'Unity.
    // En pleine partie, un joueur l'OUVRE EN COOP depuis la pause et choisit un mot de passe ; la partie reçoit un code
    // à 6 caractères. Jusqu'à 3 autres la REJOIGNENT depuis le menu principal avec le code et le mot de passe.
    //
    // L'hôte fait tourner la vraie partie (vagues, ballons, tirs des singes, bourse, bibliothèque, singes posés).
    // NGO s'occupe du reste :
    //   - le NetworkManager relie les joueurs. L'hôte vérifie le mot de passe avant de laisser entrer (Approve) ;
    //   - chaque joueur a son objet réseau, CoopPlayer : sa tête et ses mains, ses flèches, et ses demandes à l'hôte
    //     (des RPC : « j'ai touché ce ballon », « j'ai vendu une banane », « je pose ce singe ici ») ;
    //   - CoopGame garde l'état partagé (NetworkVariable) : l'hôte l'écrit, les invités le lisent ;
    //   - chaque ballon a son double réseau, CoopBalloon, qui le fait apparaître et bouger chez les invités.
    // Chacun garde son arc, sa bananeraie et sa sauvegarde.
    //
    // Le réseau passe par Unity Relay, un serveur relais gratuit de Unity : deux appareils sur des réseaux différents
    // ne peuvent pas se joindre directement (box, pare-feu de l'IUT), ils se connectent tous les deux AU RELAIS.
    // Le code de la partie, c'est le « code de connexion » que le relais donne à l'hôte. On passe par WebSocket sécurisé
    // (wss, port 443, comme un site web) : c'est ce qui passe le mieux les pare-feu.
    //
    // Les objets réseau sont des prefabs dans Resources/Coop, créés par le menu SAE > Coop > Créer les prefabs réseau.
    public class Coop : MonoBehaviour
    {
        public const int MaxGuests = 3;
        const string Protocol = "wss";   // WebSocket sécurisé (voir plus haut)

        public static CoopRole Role { get; private set; }
        public static bool IsHost => Role == CoopRole.Host;
        public static bool IsGuest => Role == CoopRole.Guest;
        public static bool Busy { get; private set; }         // on attend le serveur de Unity (connexion, code)
        public static bool Connected { get; private set; }    // invité : l'hôte l'a laissé entrer
        public static byte LocalId { get; private set; }      // 0 = l'hôte, 1 à 3 = les invités
        public static string Status { get; private set; } = "";
        public static string Code { get; private set; } = "";  // le code de la partie (6 caractères, donné par le relais)
        public static string Password { get; private set; } = "";

        // Invité : chaque demande à l'hôte a un numéro (1, 2, 3…). LastStateRequest = la dernière qui change l'état
        // (bourse, bibliothèque, singes posés). L'invité la fait tout de suite chez lui ; tant que l'hôte ne l'a pas
        // faite, on n'écoute pas son état (voir CoopGame) : il nous ferait revenir en arrière une seconde.
        public static int LastStateRequest { get; private set; }

        // Invité : la dernière de nos demandes que l'hôte a faite (CoopGame la garde pour chaque joueur)
        public static int DoneRequest => CoopGame.Instance ? CoopGame.Instance.DoneFor(LocalId) : 0;

        static Coop instance;

        NetworkManager network;
        UnityTransport transport;
        GameObject gamePrefab, balloonPrefab;
        bool stateReceived;   // invité : l'état de l'hôte a remplacé le nôtre
        int nextRequest = 1;
        Arrow arrowPrefab;    // pour montrer les flèches des autres

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
                var i = Ensure();
                if (i != null)
                {
                    await SignIn();
                    var allocation = await RelayService.Instance.CreateAllocationAsync(MaxGuests);
                    string code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                    i.transport.SetRelayServerData(allocation.ToRelayServerData(Protocol));
                    Password = password;
                    if (!i.network.StartHost()) throw new Exception("le NetworkManager n'a pas démarré");
                    Role = CoopRole.Host;
                    LocalId = 0;
                    Code = code;
                    Status = "En attente d'un joueur…";
                    i.SpawnShared();
                }
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
                var i = Ensure();
                if (i != null)
                {
                    await SignIn();
                    var allocation = await RelayService.Instance.JoinAllocationAsync(code);
                    i.transport.SetRelayServerData(allocation.ToRelayServerData(Protocol));
                    // Le mot de passe part avec la demande de connexion : l'hôte le lit dans Approve
                    i.network.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(password);
                    i.nextRequest = 1;
                    LastStateRequest = 0;
                    Code = code;
                    Password = password;
                    Connected = false;
                    if (!i.network.StartClient()) throw new Exception("le NetworkManager n'a pas démarré");
                    Role = CoopRole.Guest;
                    Status = "Connexion à la partie…";
                }
            }
            catch (RelayServiceException e) when (e.Reason == RelayExceptionReason.JoinCodeNotFound || e.Reason == RelayExceptionReason.InvalidArgument
                                                   || e.Reason == RelayExceptionReason.InvalidRequest || e.Reason == RelayExceptionReason.EntityNotFound)
            {
                Status = $"Aucune partie {code} : vérifie le code";
            }
            catch (Exception e) { Failed(e); }   // pas d'Internet, projet pas relié à Unity Cloud… : on le dit, le jeu continue en solo
            Busy = false;
        }

        // Le relais de Unity demande d'être identifié : un compte anonyme, créé tout seul la première fois.
        // Dans l'éditeur, chaque Unity ouvert (ou joueur de Multiplayer Play Mode) prend son propre compte : sinon, sur un
        // même PC, l'hôte et l'invité seraient le même joueur pour le relais (le profil est un nom de dossier : ≤ 30 caractères).
        static async Task SignIn()
        {
            var options = new InitializationOptions();
            if (Application.isEditor) options.SetProfile("editeur-" + System.Diagnostics.Process.GetCurrentProcess().Id);
            if (UnityServices.State != ServicesInitializationState.Initialized) await UnityServices.InitializeAsync(options);
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        static void Failed(Exception e)
        {
            Debug.LogWarning("Coop : " + e);
            const string reason = "Pas d'Internet, ou le serveur de Unity ne répond pas";
            if (Role != CoopRole.Solo) { Stop(reason); return; }
            if (instance && instance.network.IsListening) instance.network.Shutdown();   // démarré à moitié : on coupe
            Status = reason;
        }

        public static void Leave() => Stop("");

        // On quitte (ou l'hôte est parti) : NGO coupe la connexion et efface tous les objets réseau
        // (les autres joueurs, les doubles des ballons). Les vrais ballons de l'hôte restent : la partie continue en solo.
        static void Stop(string reason)
        {
            if (Role == CoopRole.Solo) return;
            var i = instance;
            bool reload = IsGuest && i.stateReceived;
            // Solo AVANT de couper : si NGO nous prévient de la coupure (OnDisconnect), on ne repasse pas ici
            Role = CoopRole.Solo;
            Connected = false;
            LocalId = 0;
            Status = reason;
            i.stateReceived = false;
            i.network.Shutdown();
            // L'invité jouait avec l'état de l'hôte : il retrouve sa propre partie, telle qu'il l'avait sauvegardée
            if (reload) SaveGame.ReloadFromDisk();
        }

        // Le NetworkManager, créé la première fois. Renvoie null (avec un message) si les prefabs réseau manquent.
        static Coop Ensure()
        {
            if (instance) return instance;

            var player = Resources.Load<GameObject>("Coop/CoopJoueur");
            var game = Resources.Load<GameObject>("Coop/CoopPartie");
            var balloon = Resources.Load<GameObject>("Coop/CoopBallon");
            if (!player || !game || !balloon)
            {
                Status = "Prefabs réseau manquants : menu SAE > Coop > Créer les prefabs réseau";
                return null;
            }

            var go = new GameObject("Coop");
            DontDestroyOnLoad(go);   // garde la connexion quand une scène se recharge (nouvelle partie)
            go.SetActive(false);     // le NetworkManager lit sa configuration en s'allumant : on la remplit d'abord
            var i = go.AddComponent<Coop>();
            i.gamePrefab = game;
            i.balloonPrefab = balloon;
            i.transport = go.AddComponent<UnityTransport>();
            i.transport.UseWebSockets = true;   // wss : voir plus haut
            i.network = go.AddComponent<NetworkManager>();
            i.network.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = i.transport,
                PlayerPrefab = player,          // NGO en crée un par joueur qui entre (l'hôte compris)
                ConnectionApproval = true,      // l'hôte vérifie le mot de passe (Approve)
                EnableSceneManagement = false,  // chacun charge ses scènes : on change de niveau sans attendre les autres
            };
            i.network.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = game });
            i.network.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = balloon });
            i.network.ConnectionApprovalCallback = i.Approve;
            i.network.OnClientDisconnectCallback += i.OnDisconnect;
            go.SetActive(true);
            instance = i;
            return i;
        }

        // Hôte : la partie partagée et le double de chaque ballon déjà sur la carte (on peut ouvrir en pleine vague)
        void SpawnShared()
        {
            Instantiate(gamePrefab).GetComponent<NetworkObject>().Spawn();
            foreach (var b in Balloon.All) ShareBalloon(b);
        }

        // Hôte : un joueur frappe à la porte. Payload = le mot de passe qu'il a tapé.
        void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            bool self = request.ClientNetworkId == NetworkManager.ServerClientId;
            bool full = network.ConnectedClientsIds.Count > MaxGuests;
            bool good = self || Encoding.UTF8.GetString(request.Payload) == Password;
            response.Approved = good && !full;
            response.CreatePlayerObject = response.Approved;
            response.Reason = full ? "Partie pleine" : good ? "" : "Mot de passe incorrect";   // l'invité le lit dans OnDisconnect
            if (response.Approved && !self) Sounds.Play(Sounds.Bouton);
        }

        // NGO : une connexion s'est coupée. Chez l'hôte, c'est un invité qui part : NGO efface son objet tout seul.
        // Chez l'invité, c'est la sienne : refusé, hôte parti, ou Internet coupé.
        void OnDisconnect(ulong clientId)
        {
            if (!IsGuest) return;
            string reason = network.DisconnectReason;
            if (string.IsNullOrEmpty(reason)) reason = Connected ? "L'hôte a quitté la partie" : "Impossible de joindre la partie";
            Stop(reason);
        }

        void OnApplicationQuit() => Leave();

        // Le panneau COOP dit où on en est, avec le nombre de ballons envoyés ou reçus (pratique pour vérifier au casque)
        // (le texte n'est refait que si un nombre change : pas de nouvelle chaîne à chaque image)
        int shownPlayers = -1, shownBalloons = -1;

        void Update()
        {
            if (Role == CoopRole.Solo || (IsGuest && !Connected)) { shownPlayers = -1; return; }
            int players = IsHost ? network.ConnectedClientsIds.Count : 0;   // la liste des joueurs n'existe que chez l'hôte
            int balloons = CoopBalloon.Count;
            if (players == shownPlayers && balloons == shownBalloons) return;
            shownPlayers = players;
            shownBalloons = balloons;
            if (IsGuest) Status = $"Connecté · tu es le joueur {LocalId + 1} · {balloons} ballons reçus";
            else Status = players <= 1 ? "En attente d'un joueur…" : $"{players} joueurs · {balloons} ballons partagés";
        }

        // Appelé par CoopPlayer quand notre propre joueur réseau apparaît : l'hôte nous a laissés entrer
        public static void Joined(byte number)
        {
            LocalId = number;
            if (!IsGuest) return;
            Connected = true;
            Status = $"Connecté · tu es le joueur {number + 1}";
            Tutorial.RestoreDone();   // l'invité rejoint une partie déjà commencée : pas de tutoriel
            Sounds.Play(Sounds.Bouton);
        }

        // Appelé par CoopGame : l'état de l'hôte a remplacé le nôtre (en quittant, on rechargera notre sauvegarde)
        public static void StateApplied() { if (instance) instance.stateReceived = true; }

        // --- Les ballons ---

        // Hôte : un ballon vient d'apparaître (Balloon.Start), on lui donne son double réseau
        public static void ShareBalloon(Balloon b)
        {
            if (!IsHost || b.Proxy) return;
            var proxy = Instantiate(instance.balloonPrefab).GetComponent<CoopBalloon>();
            proxy.Follow(b);
            proxy.NetworkObject.Spawn();
        }

        // --- Les demandes de l'invité à l'hôte (RPC de CoopPlayer) ---

        // Donne un numéro à la demande et l'envoie. Renvoie son numéro (le ballon touché le garde : Balloon.ApplyHostLayers).
        static int Request(bool changesState, Action<CoopPlayer, int> send)
        {
            if (!IsGuest || !Connected || !CoopPlayer.Local) return 0;
            int number = instance.nextRequest++;
            if (changesState) LastStateRequest = number;
            send(CoopPlayer.Local, number);
            return number;
        }

        public static int SendHit(CoopBalloon balloon, int pierce) =>
            balloon ? Request(false, (p, n) => p.HitRpc(n, balloon, pierce)) : 0;
        public static void SendSlow(CoopBalloon balloon, float factor, float seconds)
        {
            if (balloon) Request(false, (p, n) => p.SlowRpc(n, balloon, factor, seconds));
        }
        public static void SendEarn(int amount, int stakeBack) => Request(true, (p, n) => p.EarnRpc(n, amount, stakeBack));
        public static void SendSpend(int amount) => Request(true, (p, n) => p.SpendRpc(n, amount));
        public static void SendInventory(InventoryOp op, Monkey m) => Request(true, (p, n) => p.InventoryRpc(n, op, m.type, m.level));
        public static void SendLaunch() => Request(false, (p, n) => p.LaunchRpc(n));

        // Poser, fusionner ou reprendre un singe : l'hôte refait la même chose avec le singe que l'invité tient
        public static void SendPlace(Vector2 pos, Monkey? held) =>
            Request(true, (p, n) => p.PlaceRpc(n, pos, held.HasValue, held?.type ?? default, held?.level ?? default));

        // --- Le casino : une roulette pour tous, que l'hôte fait tourner (le blackjack se joue chacun chez soi) ---

        public static void SendRoulette(RouletteOp op, RouletteColor color, float speed) =>
            Request(false, (p, n) => p.RouletteRpc(n, op, color, speed));

        // Hôte : la roue vient d'être lancée (par lui ou pour un invité) : tout le monde voit le même lancer
        public static void ShareRouletteSpin(int result, float speed, RouletteColor color, int bet)
        {
            if (IsHost && CoopGame.Instance) CoopGame.Instance.RouletteSpinRpc(result, speed, color, bet);
        }

        // --- Les flèches ---

        // Une flèche vient de partir (Arrow.Launch) : les autres la voient voler
        public static void SendShot(Vector3 from, Vector3 velocity)
        {
            if (Role == CoopRole.Solo || (IsGuest && !Connected) || !CoopPlayer.Local) return;
            CoopPlayer.Local.ShotRpc(from, velocity);
        }

        // La flèche d'un autre joueur : seulement pour la voir, c'est chez le tireur qu'elle perce les ballons
        public static void ShowShot(Vector3 from, Vector3 velocity)
        {
            if (!instance) return;
            if (!instance.arrowPrefab)
            {
                var bow = FindAnyObjectByType<Bow>(FindObjectsInactive.Include);
                if (bow) instance.arrowPrefab = bow.arrowPrefab;
            }
            if (!instance.arrowPrefab) return;
            var arrow = Instantiate(instance.arrowPrefab, from, Quaternion.LookRotation(velocity));
            arrow.visualOnly = true;
            arrow.Launch(velocity);
        }
    }
}
