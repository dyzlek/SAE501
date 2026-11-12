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

        public const int PracticeBalloons = 3;      // ballons d'entraînement à éclater avant la vraie vague (PracticeBalloon)

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
        static int pops;                     // entraînement : ballons d'entraînement éclatés
        static bool weakShot;                // entraînement : la dernière flèche n'était pas assez tendue
        static float nextRefusePopup;        // l'annonce « suis la flèche dorée » ne revient pas à chaque refus

        // Vague 1 ratée et finie : le tableau propose REFAIRE (relancer la vague) ou CONTINUER (passer à la suite)
        public static bool FailChoice => waveLost && Current == Step.Shoot;   // relancée (Tick) : waveLost repasse à false
        public static int PracticeLeft => PracticeBalloons - pops;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState()
        {
            Current = Step.Welcome;
            monkeyGiven = waveLost = weakShot = false;
            Confirming = false;
            moneyAtStep = pops = 0;
            nextRefusePopup = 0f;
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
            Step.TowerDefense => "TA MISSION : DÉFENDRE",
            Step.GoMap => "EN ROUTE !",
            Step.Bow => "QUINCY, C'EST TOI !",
            Step.Practice => weakShot ? "TENDS PLUS FORT !" : $"ÉCHAUFFEMENT : {pops} / {PracticeBalloons} BALLONS",
            Step.Launch => "LA VRAIE VAGUE",
            Step.Shoot => FailChoice ? "DOMMAGE !" : "PAS UN SEUL NE PASSE !",
            Step.BowUpgrades => "AMÉLIORE TON ARC",
            Step.Reward => GameState.WavesWon >= 1 ? "BRAVO, ARCHER !" : "TON PREMIER SINGE",
            Step.Money => "LE NERF DE LA GUERRE",
            Step.Library => "LA BIBLIOTHÈQUE",
            Step.Unlock => "RECRUTE DES SINGES",
            Step.Take => "TON PREMIER SINGE",
            Step.Place => "POSE-LE !",
            Step.Bananas => "À LA BANANERAIE !",
            Step.Orchard => "LE COMPTOIR DU BANANIER",
            Step.Harvesters => "DES SINGES À TON SERVICE",
            Step.Chest => "LE COFFRE MYSTÈRE",
            Step.Fusion => "LA FUSION",
            Step.Trophies => "TA SALLE DES TROPHÉES",
            Step.Darts => "UNE PETITE PAUSE ?",
            Step.NextWave => "À TOI DE JOUER !",
            Step.CasinoOpen => "LE CASINO OUVRE !",
            Step.CasinoPlay => "FAITES VOS JEUX !",
            _ => "",
        };

        // Lignes de ~36 caractères au plus (le texte 3D ne revient pas à la ligne tout seul), 3 lignes.
        // Le ton : celui de Bloons, joyeux et direct ; une action claire par tableau, en MAJUSCULES le geste ou le bouton.
        public static string Body(Step s) => s switch
        {
            Step.Welcome => "Ici, les singes coulent des jours\nheureux : bananes, siestes au soleil\net parties de fléchettes...",
            Step.Bloons => "Mais ces ballons de malheur veulent\ntraverser la vallée ! Chaque ballon\nqui passe coûte une vie.",
            Step.TowerDefense => "Ils suivent le chemin. Éclate-les\navant la sortie : avec ton arc,\net avec des singes à poser !",
            Step.GoMap => "Le chemin t'attend ! Appuie sur\nle bouton SE TP, sous la flèche\ndorée, pour filer sur la carte.",
            Step.Bow => VR
                ? "Regarde ta main GAUCHE : ton arc\nest là ! Tends le bras vers le\nchemin, comme un vrai archer."
                : "Ton arc est en bas de l'écran,\net le centre de l'écran, c'est\nton viseur. Prêt à viser ?",
            Step.Practice => weakShot
                ? (VR ? "Ta flèche est tombée ! Recule\nla main DROITE jusqu'à ta joue\navant de lâcher : l'arc à fond !"
                      : "Ta flèche est tombée ! Garde\nle clic DROIT plus longtemps :\nl'arc à fond, la flèche file !")
                : (VR ? "Éclate les 3 ballons du chemin !\nMain DROITE sur la corde, serre le\nGRIP, recule à ta joue... et LÂCHE !"
                      : "Éclate les 3 ballons du chemin !\nGarde le clic DROIT : l'arc se tend.\nVise avec la ligne, puis lâche !"),
            Step.Launch => "Tu es prêt ! Appuie sur LANCER :\nla vraie vague arrive par le\nportail, à gauche.",
            Step.Shoot => FailChoice
                ? "Un ballon s'est échappé...\nREFAIRE : retente la vague 1.\nCONTINUER : passe à la suite."
                : "Vise les ballons avant la sortie.\nArc tendu à fond = flèche rapide.\nUn seul qui passe, et c'est raté !",
            Step.BowUpgrades => "Ces boutons rendent ton arc\nredoutable : flèches qui percent,\ntir triple... contre des bananes !",
            Step.Reward => GameState.WavesWon >= 1
                ? "Pas un ballon n'est passé !\nEn récompense : ton premier singe.\nAppuie sur HUB pour le chercher."
                : "Les singes vont t'aider : le premier\nest offert ! Appuie sur HUB pour\naller le chercher à la cabane.",
            Step.Money => "Ici, tout se paie en BANANES !\nLa caisse montre ton trésor.\nChaque vague gagnée t'en rapporte.",
            Step.Library => "Ici dorment tes singes ! Une ligne\npar type, une colonne par rareté :\ndu gris au blanc, toujours plus fort.",
            Step.Unlock => "Une plaque au bout de chaque ligne :\npaie-la, et ce singe pourra sortir\ndu coffre. Canon, sniper, glace...",
            Step.Take => "Le voilà, sous la flèche ! Vise-le\net serre le GRIP pour l'attraper.\nIl est tout à toi.",
            Step.Place => "Vise le plateau et lâche-le PRÈS\ndu chemin. Vert : parfait ! Rouge :\npas ici. Ensuite, il tire tout seul.",
            Step.Bananas => "Les bananes paient tout ! Passe la\nporte DERRIÈRE TOI, attrape une\nbanane et lance-la dans le panier.",
            Step.Orchard => "PRODUCTION : plus de bananes.\nFRAÎCHEUR : elles pourrissent moins.\nVALEUR : chacune rapporte plus !",
            Step.Harvesters => "Les récolteurs ramassent et lancent\nles bananes pour toi. Plus tu les\naméliores, plus ils rapportent !",
            Step.Chest => "Il donne un singe au hasard...\nLe premier est OFFERT ! Rentre dans\nla cabane et ouvre-le !",
            Step.Fusion => "Pose ton nouveau singe SUR\nle premier : 2 singes identiques\nen font un plus rare et plus fort !",
            Step.Trophies => "Vagues gagnées, bananes récoltées :\nchaque exploit allume une coupe,\ndu bronze jusqu'au diamant !",
            Step.Darts => "Entre deux vagues, lance des\nfléchettes sur la cible du mur.\nEt vise le centre... pour voir !",
            Step.NextWave => "Tu sais tout ! Appuie sur LANCER\npour la vague suivante. 100 vagues\nt'attendent... Courage, archer !",
            Step.CasinoOpen => $"{CasinoGate.UnlockWave} vagues gagnées : les barrières\nsont tombées ! Passe la porte et\nfile à la roulette, sous la flèche.",
            Step.CasinoPlay => "Touche une couleur sur le tapis,\nmise avec les jetons, puis lance\nla roue à la main. Bonne chance !",
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
            Step.Shoot => FailChoice ? TutorialSpot.Panel : WaveRunning ? TutorialSpot.None : TutorialSpot.MapLaunch,   // ratée : le tableau propose de refaire ou de continuer
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
            // Le tableau est peut-être derrière lui : une petite annonce devant ses yeux le remet sur la piste
            if (Time.unscaledTime < nextRefusePopup) return;
            nextRefusePopup = Time.unscaledTime + 4f;
            TutorialPopup.Show("SUIS LA FLÈCHE DORÉE !", TutorialPopup.Happy, 0.14f);
        }

        // --- Les événements du jeu qui comptent pour le tutoriel ---

        // Vague 1 du tutoriel : un seul ballon qui passe, et elle est ratée (WaveSpawner.BalloonEscaped)
        public static bool StrictWave => Active && Current <= Step.Shoot;

        public static void WaveLost()
        {
            if (!StrictWave) return;
            waveLost = true;
            TutorialPopup.Show("DOMMAGE !", TutorialPopup.Sad, 0.4f);   // le son de la vague perdue joue déjà (WaveSpawner)
            Changed?.Invoke();
        }

        // Une flèche vient d'être tirée (Bow.Release) : « strong » si l'arc était assez tendu pour qu'elle parte.
        // Pendant l'entraînement, une flèche trop peu tendue (elle tombe) fait dire au tableau de tendre plus fort.
        public static void ArrowShot(bool strong)
        {
            if (Current != Step.Practice || weakShot == !strong) return;
            weakShot = !strong;
            Changed?.Invoke();
        }

        // Un ballon d'entraînement vient d'éclater (PracticeBalloon)
        public static void PracticeHit()
        {
            if (Current != Step.Practice) return;
            pops++;
            weakShot = false;
            Changed?.Invoke();   // le compteur change sur le tableau
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
            Step.Practice => pops >= PracticeBalloons,
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
            if (FailChoice) { WaveSpawner.Launch(); return; }                    // REFAIRE : la vague 1 repart
            if (Showing && NeedsNext(Current)) GoTo(After(Current));
        }

        // Bouton de gauche : PASSER (demande confirmation), puis OUI
        public static void Skip()
        {
            if (!Showing) return;
            if (FailChoice) { GoTo(Step.BowUpgrades); return; }                  // CONTINUER : on passe à la suite
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
            var from = Current;
            Current = s;
            Confirming = false;
            waveLost = weakShot = false;
            moneyAtStep = Economy.Money;
            if (s >= Step.Reward) GiveFirstMonkey();
            Changed?.Invoke();
            Cheer(from, s);
        }

        // Les grands moments réussis : une annonce surgit devant le joueur, avec un son joyeux
        static void Cheer(Step from, Step to)
        {
            string text = (from, to) switch
            {
                (Step.Practice, Step.Launch) => "PRÊT AU COMBAT !",
                (Step.Shoot, Step.BowUpgrades) when GameState.WavesWon >= 1 => "VICTOIRE !",
                (Step.Place, Step.Bananas) => "BIEN POSÉ !",
                (Step.Bananas, Step.Orchard) => "CHA-CHING !",
                (Step.Fusion, Step.Trophies) => "FUSION RÉUSSIE !",
                (Step.NextWave, Step.Done) => "C'EST PARTI !",
                (_, Step.CasinoOpen) => "CASINO OUVERT !",
                _ => null,
            };
            if (text != null) TutorialPopup.Show(text, TutorialPopup.Happy, 0.35f, Sounds.SingeObtenu);
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
