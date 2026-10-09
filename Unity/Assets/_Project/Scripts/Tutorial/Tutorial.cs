using UnityEngine;

namespace SAE
{
    // Les endroits que la flèche du tutoriel peut montrer (TutorialTarget les marque dans les scènes)
    public enum TutorialSpot { None, HubTeleport, MapLaunch, MapHub, Library, Board, Door, Basket, Chest, HubLaunch }

    // Le tutoriel (niveau 1) : une suite d'étapes, une consigne à la fois, écrite sur les tableaux du décor (TutorialBoard)
    // avec une flèche dorée au-dessus de ce qu'il faut toucher (TutorialPointer).
    //   1. trois tableaux pour raconter l'univers (bouton SUIVANT)
    //   2. aller sur la carte, lancer la vague 1 et la gagner À L'ARC SEUL (on n'a encore aucun singe)
    //   3. récompense : le premier singe ; retour au hub, le prendre, le poser sur le plateau
    //   4. passer la porte, ramasser une banane et la lancer dans le panier
    //   5. le coffre et la fusion (SUIVANT), puis lancer la vague 2 : fin du tutoriel
    // Une étape « à faire » passe toute seule à la suivante quand le joueur l'a faite (Done).
    // PASSER saute tout (on reçoit quand même le premier singe). Pas de tutoriel en bac à sable.
    public static class Tutorial
    {
        public enum Step
        {
            Welcome, Bloons, Quincy,          // l'univers
            GoMap, Launch, Shoot,             // la vague 1, à l'arc
            Reward, Take, Place,              // le premier singe
            Bananas,                          // la bananeraie
            Shop, NextWave,                   // le coffre, la fusion, la vague 2
            Done
        }

        public static Step Current { get; private set; }
        public static bool Active => Current != Step.Done;
        public static event System.Action Changed;   // nouvelle étape : les tableaux se réécrivent

        static bool monkeyGiven;
        static int moneyAtStep;              // l'argent au début de l'étape (pour voir qu'une banane a été vendue)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState() { Current = Step.Welcome; monkeyGiven = false; moneyAtStep = 0; Changed = null; }

        // Le texte de chaque étape : un titre, une consigne courte (lignes de ~36 caractères), et ce que montre la flèche.
        public static string Title(Step s) => s switch
        {
            Step.Welcome => "BIENVENUE À MONKEY LANE !",
            Step.Bloons => "ALERTE AUX BLOONS !",
            Step.Quincy => "TOI, C'EST QUINCY",
            Step.GoMap => "EN ROUTE !",
            Step.Launch => "VAGUE 1",
            Step.Shoot => "À L'ARC !",
            Step.Reward => "BRAVO !",
            Step.Take => "TON PREMIER SINGE",
            Step.Place => "POSE-LE",
            Step.Bananas => "LES BANANES",
            Step.Shop => "LE COFFRE ET LA FUSION",
            Step.NextWave => "À TOI DE JOUER !",
            _ => "",
        };

        public static string Body(Step s) => s switch
        {
            Step.Welcome => "Ici, les singes vivent tranquilles\ndans leur cabane, entre la bananeraie\net les montagnes.",
            Step.Bloons => "Mais des vagues de ballons arrivent\npar le chemin. S'ils passent,\nla cabane perd des vies !",
            Step.Quincy => "Tu es l'archer de la cabane.\nTon arc, tes singes et tes bananes :\nc'est tout ce qu'il te faut.",
            Step.GoMap => "Appuie sur SE TP\npour aller sur la carte.",
            Step.Launch => "Ton arc est prêt. Appuie sur LANCER\nquand tu veux : les ballons arrivent\npar le portail à gauche.",
            Step.Shoot => "L'arc est dans ta main gauche.\nMain droite sur la corde, serre le grip,\nrecule, vise... et lâche !",
            Step.Reward => "Tu as gagné ton premier singe !\nAppuie sur HUB\npour rentrer à la cabane.",
            Step.Take => "Il t'attend dans la bibliothèque,\nà gauche. Vise-le et serre le grip\npour le prendre.",
            Step.Place => "Vise le plateau et lâche-le\nprès du chemin (pas dessus).\nVert : c'est bon. Rouge : impossible.",
            Step.Bananas => "Les bananes paient tout. Passe la\nporte, attrape une banane et lance-la\ndans le panier avant qu'elle pourrisse.",
            Step.Shop => "Le coffre donne un singe au hasard.\nPose 2 singes identiques l'un sur\nl'autre : ils fusionnent en plus fort !",
            Step.NextWave => "Lance la vague 2 avec LANCER.\n100 vagues t'attendent...\nbonne chance !",
            _ => "",
        };

        // Les étapes où l'on lit, puis appuie sur SUIVANT (les autres passent toutes seules)
        public static bool NeedsNext(Step s) => s == Step.Welcome || s == Step.Bloons || s == Step.Quincy || s == Step.Shop;

        public static TutorialSpot Spot(Step s) => s switch
        {
            Step.GoMap => TutorialSpot.HubTeleport,
            Step.Launch => TutorialSpot.MapLaunch,
            Step.Reward => TutorialSpot.MapHub,
            Step.Take => TutorialSpot.Library,
            Step.Place => TutorialSpot.Board,
            Step.Bananas => Levels.Current == Level.Hub && PlayerInCabin() ? TutorialSpot.Door : TutorialSpot.Basket,
            Step.Shop => TutorialSpot.Chest,
            Step.NextWave => TutorialSpot.HubLaunch,
            _ => TutorialSpot.None,
        };

        // Le joueur est-il dans la cabane (et pas dans la bananeraie, derrière la porte) ?
        static bool PlayerInCabin()
        {
            var p = PlayerRig.Local ? PlayerRig.Local.transform.position : Vector3.zero;
            return new Vector2(p.x, p.z).magnitude < HubLayout.CabinRadius;
        }

        // L'étape est-elle faite ? (pour les étapes « à faire »)
        static bool IsDone(Step s)
        {
            var waves = WaveSpawner.Instance;
            bool running = waves && waves.Running;
            return s switch
            {
                Step.GoMap => Levels.Current == Level.Carte || running || GameState.WavesWon >= 1,
                Step.Launch => running || GameState.WavesWon >= 1,
                Step.Shoot => GameState.WavesWon >= 1,
                Step.Reward => Levels.Current == Level.Hub,
                Step.Take => GameState.Held != null || GameState.Placed.Count > 0,
                Step.Place => GameState.Placed.Count > 0,
                Step.Bananas => Economy.Money > moneyAtStep,
                Step.NextWave => running || GameState.WavesWon >= 2,
                _ => false,
            };
        }

        // Appelé à chaque image par TutorialRunner : on avance tant que les étapes sont faites
        public static void Tick()
        {
            if (!Active) return;
            if (GameState.Sandbox) { Finish(); return; }
            while (Active && !NeedsNext(Current) && IsDone(Current)) GoTo(Current + 1);
        }

        // Bouton SUIVANT
        public static void Next()
        {
            if (Active && NeedsNext(Current)) GoTo(Current + 1);
        }

        // Bouton PASSER : tout de suite à la fin, avec le premier singe
        public static void Finish() => GoTo(Step.Done);

        static void GoTo(Step s)
        {
            Current = s;
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
