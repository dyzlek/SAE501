using UnityEngine;

namespace SAE
{
    // Les endroits que la flèche du tutoriel peut montrer (TutorialTarget les marque dans les scènes).
    // Panel = le tableau du tutoriel lui-même (étapes à lire) ; FirstMonkey = la case du Classique gris de la bibliothèque.
    public enum TutorialSpot { None, Panel, HubTeleport, MapLaunch, MapHub, Library, FirstMonkey, Board, Door, Basket, Chest, HubLaunch, Orchard, Harvesters, Casino }

    // Ce que le joueur essaie de faire : pendant le tutoriel, seul ce que demande l'étape en cours est permis
    public enum TutorialAction { GoMap, GoHub, Launch, ClearBoard, Door, Chest, TakeMonkey, Other }

    // Le tutoriel (niveau 1) : une suite d'étapes, une consigne à la fois, écrite sur les tableaux du décor (TutorialBoard)
    // avec une flèche dorée au-dessus de ce qu'il faut toucher (TutorialRunner).
    //   1. trois tableaux pour raconter l'univers (bouton SUIVANT)
    //   2. aller sur la carte, lancer la vague 1 et la gagner À L'ARC SEUL, sans laisser passer un seul ballon
    //   3. récompense : le premier singe ; retour au hub, la bibliothèque, prendre le singe, le poser sur le plateau
    //   4. passer la porte, ramasser une banane et la lancer dans le panier ; puis, dans la bananeraie, ce qu'on y achète :
    //      le comptoir du bananier, les singes récolteurs (ils travaillent pour nous) et le casino
    //   5. ouvrir le coffre (offert, il donne un 2e Classique gris), puis fusionner les deux singes
    //   6. lancer la vague 2 : fin du tutoriel
    // Une étape « à faire » passe toute seule à la suivante quand le joueur l'a faite (IsDone).
    // Pendant le tutoriel, les autres interactions sont refusées (Allows) : le tableau tremble pour le rappeler.
    // PASSER saute tout (on reçoit quand même le premier singe). Pas de tutoriel en bac à sable.
    public static class Tutorial
    {
        public enum Step
        {
            Welcome, Bloons, Quincy,          // l'univers
            GoMap, Launch, Shoot,             // la vague 1, à l'arc
            Reward, Library, Take, Place,     // le premier singe
            Bananas, Orchard, Harvesters, Casino,   // la bananeraie et ce qu'on y achète
            Chest, Fusion,                    // le coffre et la fusion
            NextWave,                         // la vague 2
            Done
        }

        public static Step Current { get; private set; }
        public static bool Active => Current != Step.Done;
        public static event System.Action Changed;   // nouvelle étape (ou nouveau texte) : les tableaux se réécrivent
        public static event System.Action Refused;   // une interaction pas encore permise : les tableaux tremblent

        static bool monkeyGiven;
        static bool waveLost;                // vague 1 ratée : la consigne dit de la relancer
        static int moneyAtStep;              // l'argent au début de l'étape (pour voir qu'une banane a été vendue)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState()
        {
            Current = Step.Welcome;
            monkeyGiven = waveLost = false;
            Confirming = false;
            moneyAtStep = 0;
            Changed = null;
            Refused = null;
        }

        // --- Les textes ---

        public static string Title(Step s) => s switch
        {
            Step.Welcome => "BIENVENUE À MONKEY LANE !",
            Step.Bloons => "ALERTE AUX BLOONS !",
            Step.Quincy => "TOI, C'EST QUINCY",
            Step.GoMap => "EN ROUTE !",
            Step.Launch => "VAGUE 1",
            Step.Shoot => waveLost ? "RATÉ, UN BALLON EST PASSÉ !" : "À L'ARC !",
            Step.Reward => "BRAVO !",
            Step.Library => "LA BIBLIOTHÈQUE",
            Step.Take => "TON PREMIER SINGE",
            Step.Place => "POSE-LE",
            Step.Bananas => "LES BANANES",
            Step.Orchard => "LE COMPTOIR DU BANANIER",
            Step.Harvesters => "LES SINGES RÉCOLTEURS",
            Step.Casino => "LE CASINO",
            Step.Chest => "LE COFFRE",
            Step.Fusion => "LA FUSION",
            Step.NextWave => "À TOI DE JOUER !",
            _ => "",
        };

        // Lignes de ~36 caractères au plus (le texte 3D ne revient pas à la ligne tout seul)
        public static string Body(Step s) => s switch
        {
            Step.Welcome => "Ici, les singes vivent tranquilles\ndans leur cabane, entre la bananeraie\net les montagnes.",
            Step.Bloons => "Mais des vagues de ballons arrivent\npar le chemin. S'ils passent,\nla cabane perd des vies !",
            Step.Quincy => "Tu es l'archer de la cabane.\nTon arc, tes singes et tes bananes :\nc'est tout ce qu'il te faut.",
            Step.GoMap => "Appuie sur le bouton SE TP\n(la flèche dorée) pour aller\nsur la carte.",
            Step.Launch => "Ton arc est prêt. Appuie sur LANCER :\nles ballons arrivent par le portail,\nà gauche.",
            Step.Shoot => waveLost
                ? "Pour ce premier essai, aucun ballon\nne doit passer. Appuie sur LANCER\npour recommencer la vague."
                : "L'arc est dans ta main gauche.\nMain droite sur la corde, serre le grip,\nrecule, vise... et lâche !",
            Step.Reward => "Aucun ballon n'est passé : bien joué !\nTu gagnes ton premier singe.\nAppuie sur HUB pour rentrer.",
            Step.Library => "Chaque ligne : un type de singe.\nChaque colonne : sa rareté, du gris\nau blanc. Les plaques débloquent.",
            Step.Take => "Ton singe est là (la flèche).\nVise-le et serre le grip\npour le prendre.",
            Step.Place => "Vise le plateau et lâche-le\nprès du chemin (pas dessus).\nVert : c'est bon. Rouge : impossible.",
            Step.Bananas => "Les bananes paient tout. Passe la\nporte DERRIÈRE TOI, et lance une\nbanane dans le panier.",
            Step.Orchard => "Au fond, sous l'abri : améliore tes\nbananiers (production, fraîcheur,\nvaleur) ou plante un nouvel arbre.",
            Step.Harvesters => "À côté, achète des singes récolteurs :\nils ramassent les bananes et les\nlancent dans le panier pour toi !",
            Step.Casino => "À droite, la roulette : mise tes\nbananes sur une couleur. Tu peux\ngagner gros... ou tout perdre !",
            Step.Chest => "Le coffre donne un singe au hasard.\nLe premier est offert : rentre\ndans la cabane et ouvre-le !",
            Step.Fusion => "Prends ton nouveau singe et pose-le\nSUR le premier : 2 singes identiques\nfusionnent en un singe plus fort !",
            Step.NextWave => "Lance la vague 2 avec LANCER.\n100 vagues t'attendent...\nbonne chance !",
            _ => "",
        };

        // Les étapes où l'on lit, puis appuie sur SUIVANT (les autres passent toutes seules)
        public static bool NeedsNext(Step s) => s == Step.Welcome || s == Step.Bloons || s == Step.Quincy || s == Step.Library
                                              || s == Step.Orchard || s == Step.Harvesters || s == Step.Casino;

        // Ce que montre la flèche
        public static TutorialSpot Spot(Step s) => s switch
        {
            Step.Welcome or Step.Bloons or Step.Quincy => TutorialSpot.Panel,
            Step.GoMap => TutorialSpot.HubTeleport,
            Step.Launch => TutorialSpot.MapLaunch,
            Step.Shoot => WaveRunning ? TutorialSpot.None : TutorialSpot.MapLaunch,   // ratée : on relance
            Step.Reward => TutorialSpot.MapHub,
            Step.Library => TutorialSpot.Library,
            Step.Take => TutorialSpot.FirstMonkey,
            Step.Place => TutorialSpot.Board,
            Step.Bananas => PlayerInCabin() ? TutorialSpot.Door : TutorialSpot.Basket,
            Step.Orchard => TutorialSpot.Orchard,
            Step.Harvesters => TutorialSpot.Harvesters,
            Step.Casino => TutorialSpot.Casino,
            Step.Chest => PlayerInCabin() ? TutorialSpot.Chest : TutorialSpot.Door,
            Step.Fusion => GameState.Held != null || !PlayerInCabin() ? TutorialSpot.Board : TutorialSpot.FirstMonkey,
            Step.NextWave => TutorialSpot.HubLaunch,
            _ => TutorialSpot.None,
        };

        // --- Ce qui est permis ---

        public static bool Allows(TutorialAction a)
        {
            if (!Active) return true;
            return Current switch
            {
                Step.GoMap => a == TutorialAction.GoMap,
                Step.Launch or Step.Shoot => a == TutorialAction.Launch,
                Step.Reward => a == TutorialAction.GoHub,
                Step.Take or Step.Place => a == TutorialAction.TakeMonkey,
                Step.Bananas => a == TutorialAction.Door,
                Step.Chest => a == TutorialAction.Chest || a == TutorialAction.Door,
                Step.Fusion => a == TutorialAction.TakeMonkey || a == TutorialAction.Door,
                Step.NextWave => a == TutorialAction.Launch || a == TutorialAction.TakeMonkey || a == TutorialAction.Door || a == TutorialAction.GoMap,
                _ => false,   // les étapes à lire : seulement SUIVANT / PASSER
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

        // Appuyer sur un bouton (HandPress, RayPress, DesktopPlayer) : refusé si l'étape ne le demande pas.
        // Les boutons du tableau du tutoriel (SUIVANT, PASSER) marchent toujours.
        public static bool TryPress(IPressable p)
        {
            if (p is TutorialButton || Allows(ActionOf(p))) { p.Press(); return true; }
            Refuse();
            return false;
        }

        public static void Refuse() => Refused?.Invoke();

        // --- Les événements du jeu qui comptent pour le tutoriel ---

        // Vague 1 du tutoriel : un seul ballon qui passe, et elle est ratée (WaveSpawner.BalloonEscaped)
        public static bool StrictWave => Active && Current <= Step.Shoot;

        public static void WaveLost()
        {
            if (!StrictWave) return;
            waveLost = true;
            Changed?.Invoke();
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

        // --- Le déroulé ---

        static bool WaveRunning => WaveSpawner.Instance && WaveSpawner.Instance.Running;

        // Le joueur est-il dans la cabane (et pas dans la bananeraie, derrière la porte) ?
        static bool PlayerInCabin()
        {
            if (Levels.Current != Level.Hub) return false;
            var p = PlayerRig.Local ? PlayerRig.Local.HeadPosition : Vector3.zero;
            return new Vector2(p.x, p.z).magnitude < HubLayout.CabinRadius;
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
            Step.Launch => WaveRunning,
            Step.Shoot => GameState.WavesWon >= 1,
            Step.Reward => Levels.Current == Level.Hub,
            Step.Take => GameState.Held != null || GameState.Placed.Count > 0,
            Step.Place => GameState.Placed.Count > 0,
            Step.Bananas => Economy.Money > moneyAtStep,
            Step.Fusion => HasFusedMonkey(),
            Step.NextWave => WaveRunning || GameState.WavesWon >= 2,
            _ => false,   // à lire (SUIVANT), ou le coffre (ChestOpened)
        };

        // Appelé à chaque image par TutorialRunner : on avance tant que les étapes sont faites
        public static void Tick()
        {
            if (!Active) return;
            if (GameState.Sandbox) { Finish(); return; }
            if (waveLost && WaveRunning) { waveLost = false; Changed?.Invoke(); }   // relancée : on retire « RATÉ »
            while (Active && !NeedsNext(Current) && IsDone(Current)) GoTo(Current + 1);
        }

        // PASSER demande d'abord une confirmation : le tableau affiche « TU ES SÛR ? »,
        // le bouton de droite devient NON (on reprend), celui de gauche OUI (on saute tout).
        public static bool Confirming { get; private set; }

        // Bouton de droite : SUIVANT, ou NON pendant la confirmation
        public static void Next()
        {
            if (Confirming) { Confirming = false; Changed?.Invoke(); return; }   // on reprend où on en était
            if (Active && NeedsNext(Current)) GoTo(Current + 1);
        }

        // Bouton de gauche : PASSER (demande confirmation), puis OUI
        public static void Skip()
        {
            if (!Active) return;
            if (Confirming) Finish();
            else { Confirming = true; Changed?.Invoke(); }
        }

        // Fin du tutoriel, avec le premier singe
        public static void Finish() => GoTo(Step.Done);

        static void GoTo(Step s)
        {
            Current = s;
            Confirming = false;
            waveLost = false;
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
