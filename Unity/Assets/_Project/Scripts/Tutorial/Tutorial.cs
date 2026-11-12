using UnityEngine;

namespace SAE
{
    // Les endroits que la flèche du tutoriel peut montrer (TutorialTarget les marque dans les scènes).
    // Panel = le tableau du tutoriel lui-même (étapes à lire) ; FirstMonkey = la case du Classique gris de la bibliothèque.
    // Les cinq derniers (Money, Plaque, BowUpgrades, Trophies, Darts) sont posés au lancement par l'objet lui-même
    // (TutorialTarget.Add) : pas besoin de régénérer les scènes.
    public enum TutorialSpot
    {
        None, Panel, HubTeleport, MapLaunch, MapHub, Library, FirstMonkey, Board, Door, Basket, Chest, HubLaunch, Orchard, Harvesters, Casino,
        Money, Plaque, BowUpgrades, Trophies, Darts,
    }

    // Ce que le joueur essaie de faire : pendant le tutoriel, seul ce que demande l'étape en cours est permis
    public enum TutorialAction { GoMap, GoHub, Launch, ClearBoard, Door, Chest, TakeMonkey, Other }

    // Le tutoriel (niveau 1) : une suite d'étapes, une consigne à la fois, écrite sur les tableaux du décor (TutorialBoard)
    // avec une flèche dorée au-dessus de ce qu'il faut toucher (TutorialRunner).
    //   1. l'univers et le genre : des ballons arrivent, c'est un tower defense (bouton SUIVANT)
    //   2. sur la carte : où est l'arc, comment tirer (3 flèches pour s'entraîner), puis la vague 1 À L'ARC SEUL,
    //      sans laisser passer un seul ballon ; les améliorations de l'arc
    //   3. au hub : la caisse (les bananes), la bibliothèque, débloquer des types de singes et pourquoi,
    //      prendre le premier singe et le poser sur le plateau
    //   4. passer la porte, lancer une banane dans le panier ; le comptoir du bananier et les singes récolteurs
    //      (à quoi servent leurs améliorations)
    //   5. ouvrir le coffre (offert, il donne un 2e Classique gris), puis fusionner les deux singes
    //   6. le reste de la cabane (trophées, fléchettes), et lancer la vague 2 : fin du tutoriel
    // Le casino n'en fait plus partie : il est fermé par des barrières jusqu'à la vague CasinoGate.UnlockWave.
    // Quand il ouvre, deux étapes de plus (après Done) amènent le joueur à la roulette et expliquent comment jouer :
    // elles ne bloquent rien (on peut jouer normalement pendant ce temps).
    // Une étape « à faire » passe toute seule à la suivante quand le joueur l'a faite (IsDone).
    // Pendant le tutoriel, les autres interactions sont refusées (Allows) : le tableau tremble pour le rappeler.
    // PASSER saute tout (on reçoit quand même le premier singe). Pas de tutoriel en bac à sable.
    public static class Tutorial
    {
        public enum Step
        {
            Welcome, Bloons, TowerDefense,          // l'univers et le genre
            GoMap, Bow, Practice,                   // sur la carte : l'arc, et s'entraîner à tirer
            Launch, Shoot, BowUpgrades,             // la vague 1, à l'arc ; les améliorations de l'arc
            Reward, Money, Library, Unlock,         // retour au hub : la caisse, la bibliothèque, débloquer des singes
            Take, Place,                            // le premier singe
            Bananas, Orchard, Harvesters,           // la bananeraie et ce qu'on y achète
            Chest, Fusion,                          // le coffre et la fusion
            Trophies, Darts,                        // le reste de la cabane
            NextWave,                               // la vague 2
            Done,
            CasinoOpen, CasinoPlay,                 // plus tard : le casino ouvre (CasinoGate), on y va et on apprend à jouer
        }

        public const int PracticeShots = 3;         // flèches bien tendues pour finir l'entraînement

        public static Step Current { get; private set; }
        // Le tutoriel principal est en cours : il bloque ce qui n'est pas demandé, et la partie n'est pas « finie » (SaveGame)
        public static bool Active => Current < Step.Done;
        // Les tableaux et la flèche sont affichés : le tutoriel principal, ou les étapes du casino
        public static bool Showing => Current != Step.Done;
        public static bool CasinoIntro => Current > Step.Done;
        public static event System.Action Changed;   // nouvelle étape (ou nouveau texte) : les tableaux se réécrivent
        public static event System.Action Refused;   // une interaction pas encore permise : les tableaux tremblent

        static bool monkeyGiven;
        static bool waveLost;                // vague 1 ratée : la consigne dit de la relancer
        static int moneyAtStep;              // l'argent au début de l'étape (pour voir qu'une banane a été vendue)
        static int shots;                    // entraînement : flèches bien tendues
        static bool weakShot;                // entraînement : la dernière flèche n'était pas assez tendue

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState()
        {
            Current = Step.Welcome;
            monkeyGiven = waveLost = weakShot = false;
            Confirming = false;
            moneyAtStep = shots = 0;
            Changed = null;
            Refused = null;
        }

        // Au casque ou au PC : les gestes ne sont pas les mêmes, la consigne non plus
        static bool VR => !PlayerRig.Local || PlayerRig.Local.IsVR;

        // --- Les textes ---

        public static string Title(Step s) => s switch
        {
            Step.Welcome => "BIENVENUE À MONKEY LANE !",
            Step.Bloons => "ALERTE AUX BLOONS !",
            Step.TowerDefense => "UN TOWER DEFENSE",
            Step.GoMap => "EN ROUTE !",
            Step.Bow => "TON ARC",
            Step.Practice => weakShot ? "TENDS PLUS FORT !" : $"ENTRAÎNE-TOI : {shots} / {PracticeShots}",
            Step.Launch => "VAGUE 1",
            Step.Shoot => waveLost ? "RATÉ, UN BALLON EST PASSÉ !" : "À L'ARC !",
            Step.BowUpgrades => "AMÉLIORE TON ARC",
            Step.Reward => "BRAVO !",
            Step.Money => "TES BANANES",
            Step.Library => "LA BIBLIOTHÈQUE",
            Step.Unlock => "DE NOUVEAUX SINGES",
            Step.Take => "TON PREMIER SINGE",
            Step.Place => "POSE-LE",
            Step.Bananas => "LES BANANES",
            Step.Orchard => "LE COMPTOIR DU BANANIER",
            Step.Harvesters => "LES SINGES RÉCOLTEURS",
            Step.Chest => "LE COFFRE",
            Step.Fusion => "LA FUSION",
            Step.Trophies => "LES TROPHÉES",
            Step.Darts => "POUR SOUFFLER",
            Step.NextWave => "À TOI DE JOUER !",
            Step.CasinoOpen => "LE CASINO OUVRE !",
            Step.CasinoPlay => "LA ROULETTE",
            _ => "",
        };

        // Lignes de ~36 caractères au plus (le texte 3D ne revient pas à la ligne tout seul), 3 lignes
        public static string Body(Step s) => s switch
        {
            Step.Welcome => "Ici, les singes vivent tranquilles\ndans leur cabane, entre la bananeraie\net les montagnes.",
            Step.Bloons => "Mais des vagues de ballons arrivent\npar le chemin. S'ils passent,\nla cabane perd des vies !",
            Step.TowerDefense => "Le but : éclater chaque ballon avant\nla sortie, avec ton arc et des singes\nque tu poses le long du chemin.",
            Step.GoMap => "Appuie sur le bouton SE TP\n(la flèche dorée) pour aller\nsur la carte.",
            Step.Bow => VR
                ? "Regarde ta main GAUCHE : c'est\nton arc ! Tends ce bras devant toi,\nvers le chemin, comme pour viser."
                : "Ton arc est en bas de l'écran.\nLe centre de l'écran, c'est ton\nviseur : vise avec la souris.",
            Step.Practice => weakShot
                ? (VR ? "La flèche est tombée : recule\nla main DROITE plus loin, jusqu'à\nta joue, avant de lâcher le grip."
                      : "La flèche est tombée : garde\nle clic DROIT plus longtemps\npour bien tendre l'arc.")
                : (VR ? "Main DROITE sur la corde : serre\nle GRIP, recule-la jusqu'à ta joue,\nvise avec la ligne... et LÂCHE !"
                      : "Garde le clic DROIT enfoncé :\nl'arc se tend. Vise avec la ligne\npointillée, puis LÂCHE le clic."),
            Step.Launch => "Bien tiré ! Appuie sur LANCER :\nles ballons arrivent par le portail,\nà gauche.",
            Step.Shoot => waveLost
                ? "Pour ce premier essai, aucun ballon\nne doit passer. Appuie sur LANCER\npour recommencer la vague."
                : "Vise les ballons avant la sortie.\nArc bien tendu : la flèche va plus\nloin et plus vite. Aucun ne passe !",
            Step.BowUpgrades => "Les boutons de l'arc, ici sur la\ncarte : flèches qui percent, tir\ntriple... Ils coûtent des bananes.",
            Step.Reward => "Aucun ballon n'est passé : bien joué !\nTu gagnes ton premier singe.\nAppuie sur HUB pour rentrer.",
            Step.Money => "Les bananes, c'est l'argent du jeu.\nLa caisse montre combien tu en as.\nChaque vague gagnée en rapporte.",
            Step.Library => "Chaque ligne : un type de singe.\nChaque colonne : sa rareté, du gris\nau blanc. Plus rare = plus fort.",
            Step.Unlock => "Au bout des lignes, les plaques :\npaie-en une : ce singe pourra sortir\ndu coffre. Chacun a son pouvoir !",
            Step.Take => "Ton singe est là (la flèche).\nVise-le et serre le grip\npour le prendre.",
            Step.Place => "Vise le plateau et lâche-le\nprès du chemin (pas dessus).\nVert : c'est bon. Rouge : impossible.",
            Step.Bananas => "Les bananes paient tout. Passe la\nporte DERRIÈRE TOI, et lance une\nbanane dans le panier.",
            Step.Orchard => "PRODUCTION : plus de bananes.\nFRAÎCHEUR : elles pourrissent moins.\nVALEUR : chacune rapporte plus.",
            Step.Harvesters => "Achète des singes récolteurs : ils\nramassent les bananes pour toi.\nAméliore leur vitesse et leur part.",
            Step.Chest => "Le coffre donne un singe au hasard.\nLe premier est offert : rentre\ndans la cabane et ouvre-le !",
            Step.Fusion => "Prends ton nouveau singe et pose-le\nSUR le premier : 2 singes identiques\nfusionnent en un singe plus fort !",
            Step.Trophies => "L'armoire garde tes trophées :\nvagues gagnées et bananes récoltées.\nDu bronze jusqu'au diamant !",
            Step.Darts => "Entre deux vagues, lance des\nfléchettes sur la cible du mur.\nTouche le centre, pour voir...",
            Step.NextWave => "Lance la vague 2 avec LANCER.\n100 vagues t'attendent...\nbonne chance !",
            Step.CasinoOpen => $"{CasinoGate.UnlockWave} vagues gagnées : les barrières\nde la roulette sont tombées ! Passe\nla porte et suis la flèche dorée.",
            Step.CasinoPlay => "Touche une couleur sur le tapis,\nmise avec les jetons, puis fais\ntourner la roue à la main !",
            _ => "",
        };

        // Les étapes où l'on lit, puis appuie sur SUIVANT (les autres passent toutes seules)
        public static bool NeedsNext(Step s) => s switch
        {
            Step.Welcome or Step.Bloons or Step.TowerDefense or Step.Bow or Step.BowUpgrades or Step.Money or Step.Library
                or Step.Unlock or Step.Orchard or Step.Harvesters or Step.Trophies or Step.Darts or Step.CasinoPlay => true,
            _ => false,
        };

        // Le numéro affiché en bas du tableau (« 5 / 23 », ou « CASINO 1 / 2 »)
        public static string Progress(Step s) => s > Step.Done
            ? $"CASINO {s - Step.Done} / {Step.CasinoPlay - Step.Done}"
            : $"{(int)s + 1} / {(int)Step.Done}";

        // Ce que montre la flèche
        public static TutorialSpot Spot(Step s) => s switch
        {
            Step.Welcome or Step.Bloons or Step.TowerDefense or Step.Bow => TutorialSpot.Panel,
            Step.GoMap => TutorialSpot.HubTeleport,
            Step.Practice => TutorialSpot.None,                       // il regarde le chemin et tire
            Step.Launch => TutorialSpot.MapLaunch,
            Step.Shoot => WaveRunning ? TutorialSpot.None : TutorialSpot.MapLaunch,   // ratée : on relance
            Step.BowUpgrades => TutorialSpot.BowUpgrades,
            Step.Reward => TutorialSpot.MapHub,
            Step.Money => TutorialSpot.Money,
            Step.Library => TutorialSpot.Library,
            Step.Unlock => TutorialSpot.Plaque,
            Step.Take => TutorialSpot.FirstMonkey,
            Step.Place => TutorialSpot.Board,
            Step.Bananas => PlayerInCabin() ? TutorialSpot.Door : TutorialSpot.Basket,
            Step.Orchard => TutorialSpot.Orchard,
            Step.Harvesters => TutorialSpot.Harvesters,
            Step.Chest => PlayerInCabin() ? TutorialSpot.Chest : TutorialSpot.Door,
            Step.Fusion => GameState.Held != null || !PlayerInCabin() ? TutorialSpot.Board : TutorialSpot.FirstMonkey,
            Step.Trophies => PlayerInCabin() ? TutorialSpot.Trophies : TutorialSpot.Door,
            Step.Darts => PlayerInCabin() ? TutorialSpot.Darts : TutorialSpot.Door,
            Step.NextWave => TutorialSpot.HubLaunch,
            Step.CasinoOpen => PlayerInCabin() ? TutorialSpot.Door : TutorialSpot.Casino,
            Step.CasinoPlay => TutorialSpot.Casino,
            _ => TutorialSpot.None,
        };

        // --- Ce qui est permis ---

        public static bool Allows(TutorialAction a)
        {
            if (!Active) return true;   // tutoriel fini (même pendant les étapes du casino : elles ne bloquent rien)
            return Current switch
            {
                Step.GoMap => a == TutorialAction.GoMap,
                Step.Launch or Step.Shoot => a == TutorialAction.Launch,
                Step.Reward => a == TutorialAction.GoHub,
                Step.Take or Step.Place => a == TutorialAction.TakeMonkey,
                Step.Bananas => a == TutorialAction.Door,
                Step.Chest => a == TutorialAction.Chest || a == TutorialAction.Door,
                Step.Fusion => a == TutorialAction.TakeMonkey || a == TutorialAction.Door,
                Step.Trophies or Step.Darts => a == TutorialAction.Door,
                Step.NextWave => a == TutorialAction.Launch || a == TutorialAction.TakeMonkey || a == TutorialAction.Door || a == TutorialAction.GoMap,
                _ => false,   // les étapes à lire et l'entraînement : seulement SUIVANT / PASSER
            };
        }

        // Ce qu'un bouton (IPressable) fait, pour savoir s'il est permis
        static TutorialAction ActionOf(IPressable p) => p switch
        {
            ActionCube c when c.action == ActionCube.Action.StartWave => TutorialAction.Launch,
            ActionCube c when c.action == ActionCube.Action.ClearBoard => TutorialAction.ClearBoard,
            ActionCube c => c.destination == Level.Carte ? TutorialAction.GoMap : TutorialAction.GoHub,
            PortalDoor => TutorialAction.Door,
            ChestClickable => TutorialAction.Chest,
            _ => TutorialAction.Other,
        };

        // Appuyer sur un bouton (HandPress, RayPress, DesktopPlayer) : refusé si l'étape ne le demande pas,
        // ou si c'est un bouton de la roulette tant que le casino est fermé (CasinoGate).
        // Les boutons du tableau du tutoriel (SUIVANT, PASSER) marchent toujours.
        public static bool TryPress(IPressable p)
        {
            if (CasinoGate.Blocks(p)) { CasinoGate.Refuse(); return false; }
            // Les boutons des menus (pause, options, classement, menu principal) et les ballons du menu marchent toujours
            if (p is TutorialButton || p is WorldButton || p is MenuBalloon || Allows(ActionOf(p))) { p.Press(); return true; }
            Refuse();
            return false;
        }

        // Une action qui ne suit pas le tutoriel : le tableau tremble (Refused) et le son d'alerte retentit
        public static void Refuse()
        {
            Sounds.Play(Sounds.AchatRefuse);
            Refused?.Invoke();
        }

        // --- Les événements du jeu qui comptent pour le tutoriel ---

        // Vague 1 du tutoriel : un seul ballon qui passe, et elle est ratée (WaveSpawner.BalloonEscaped)
        public static bool StrictWave => Active && Current <= Step.Shoot;

        public static void WaveLost()
        {
            if (!StrictWave) return;
            waveLost = true;
            Changed?.Invoke();
        }

        // Une flèche vient d'être tirée (Bow.Release) : « strong » si l'arc était assez tendu pour qu'elle parte
        public static void ArrowShot(bool strong)
        {
            if (Current != Step.Practice) return;
            weakShot = !strong;
            if (strong) shots++;
            Changed?.Invoke();   // le compteur et la consigne changent sur le tableau
        }

        // Le coffre du tutoriel : gratuit, et il donne un Classique gris (pour la fusion qui suit)
        public static bool ForcedChest => Active && Current == Step.Chest;

        // Étape de la fusion : le singe tenu ne peut que fusionner, pas se poser à côté (Placement.Evaluate).
        // Seulement si un singe à fusionner est déjà posé ; sinon (le premier a été rangé), on peut en reposer un.
        public static bool FusionOnly
        {
            get
            {
                if (!Active || Current != Step.Fusion || GameState.Held == null) return false;
                foreach (var p in GameState.Placed) if (p.monkey.CanFuseWith(GameState.Held.Value)) return true;
                return false;
            }
        }

        public static void ChestOpened()
        {
            if (Current == Step.Chest) GoTo(Step.Fusion);
        }

        // Le casino vient d'ouvrir (CasinoGate) : si le tutoriel principal est fini, on amène le joueur à la roulette
        public static void CasinoOpened()
        {
            if (Current == Step.Done && !GameState.Sandbox) GoTo(Step.CasinoOpen);
        }

        // --- Le déroulé ---

        static bool WaveRunning => WaveSpawner.Instance && WaveSpawner.Instance.Running;

        // Le joueur est-il dans la cabane (et pas dans la bananeraie, derrière la porte) ?
        static bool PlayerInCabin()
        {
            if (Levels.Current != Level.Hub) return false;
            var p = PlayerRig.Local ? PlayerRig.Local.HeadPosition : Vector3.zero;
            return new Vector2(p.x, p.z).magnitude < HubLayout.CabinRadius;
        }

        // Le joueur est-il arrivé devant la roulette ?
        static bool NearRoulette()
        {
            var table = RouletteTable.Instance;
            if (!table || Levels.Current != Level.Hub || !PlayerRig.Local) return false;
            var d = PlayerRig.Local.HeadPosition - table.transform.position;
            return new Vector2(d.x, d.z).magnitude < 2.5f;   // en mètres : à portée de main de la table
        }

        static bool HasFusedMonkey()
        {
            foreach (var p in GameState.Placed) if (p.monkey.level > Rarity.Gris) return true;
            return GameState.Count(new Monkey(MonkeyType.Classique, Rarity.Vert)) > 0;
        }

        // L'étape est-elle faite ? (pour les étapes « à faire »)
        static bool IsDone(Step s) => s switch
        {
            Step.GoMap => Levels.Current == Level.Carte,
            Step.Practice => shots >= PracticeShots,
            Step.Launch => WaveRunning,
            Step.Shoot => GameState.WavesWon >= 1,
            Step.Reward => Levels.Current == Level.Hub,
            Step.Take => GameState.Held != null || GameState.Placed.Count > 0,
            Step.Place => GameState.Placed.Count > 0,
            Step.Bananas => Economy.Money > moneyAtStep,
            Step.Fusion => HasFusedMonkey(),
            Step.NextWave => WaveRunning || GameState.WavesWon >= 2,
            Step.CasinoOpen => NearRoulette(),
            _ => false,   // à lire (SUIVANT), ou le coffre (ChestOpened)
        };

        // Appelé à chaque image par TutorialRunner : on avance tant que les étapes sont faites
        public static void Tick()
        {
            if (!Showing) return;
            if (GameState.Sandbox) { Finish(); return; }
            if (waveLost && WaveRunning) { waveLost = false; Changed?.Invoke(); }   // relancée : on retire « RATÉ »
            while (Showing && !NeedsNext(Current) && IsDone(Current)) GoTo(After(Current));
        }

        // L'étape d'après (la dernière du casino ramène à « fini »)
        static Step After(Step s) => s == Step.CasinoPlay ? Step.Done : s + 1;

        // PASSER demande d'abord une confirmation : le tableau affiche « TU ES SÛR ? »,
        // le bouton de droite devient NON (on reprend), celui de gauche OUI (on saute tout).
        public static bool Confirming { get; private set; }

        // Bouton de droite : SUIVANT, ou NON pendant la confirmation
        public static void Next()
        {
            if (Confirming) { Confirming = false; Changed?.Invoke(); return; }   // on reprend où on en était
            if (Showing && NeedsNext(Current)) GoTo(After(Current));
        }

        // Bouton de gauche : PASSER (demande confirmation), puis OUI
        public static void Skip()
        {
            if (!Showing) return;
            if (Confirming) Finish();
            else { Confirming = true; Changed?.Invoke(); }
        }

        // Sauvegarde : le tutoriel était fini (le premier singe a déjà été donné, il est dans la bibliothèque sauvegardée)
        public static void RestoreDone()
        {
            Current = Step.Done;
            monkeyGiven = true;
            Changed?.Invoke();   // coop : un invité qui rejoint passe le tutoriel en cours de partie, les tableaux se réécrivent
        }

        // Nouvelle partie : le tutoriel recommence
        public static void Restart() => ResetState();

        // Fin du tutoriel (ou des étapes du casino), avec le premier singe
        public static void Finish() => GoTo(Step.Done);

        static void GoTo(Step s)
        {
            Current = s;
            Confirming = false;
            waveLost = weakShot = false;
            moneyAtStep = Economy.Money;
            if (s >= Step.Reward) GiveFirstMonkey();
            Changed?.Invoke();
        }

        // Le premier singe : un Classique gris, rangé dans la bibliothèque (une seule fois)
        static void GiveFirstMonkey()
        {
            if (monkeyGiven) return;
            monkeyGiven = true;
            GameState.AddToInventory(new Monkey(MonkeyType.Classique, Rarity.Gris));
        }
    }
}
