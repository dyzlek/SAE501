using System;
using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Les records, les trophées et le classement local (#139 à #142). Tout est GARDÉ SUR LE CASQUE (PlayerPrefs) :
    // on les retrouve au lancement suivant. Ce ne sont que des souvenirs : rien ne rend la partie suivante plus facile
    // (pas de méta-progression, voir le périmètre du GDD).
    //   Partie en cours : vague atteinte, meilleur combo, bananes gagnées, ballons éclatés.
    //   Classement : les 10 meilleures parties (par vague atteinte). La partie en cours y est enregistrée à chaque
    //   vague gagnée : rien n'est perdu si on enlève le casque sans « quitter ».
    //   Trophées : des paliers (vague 10, 50, 100, 1 000 bananes…), gagnés une fois pour toutes.
    // Le mur des records de la cabane (RecordWall) affiche tout ça.
    public static class Records
    {
        public const int BoardSize = 10;
        const string BoardKey = "SAE.Classement";
        const string TrophyKey = "SAE.Trophees";
        const string GameCountKey = "SAE.Parties";

        [Serializable]
        public class Entry
        {
            public string name;      // « PARTIE 12 »
            public string date;      // « 19/10 »
            public int wave;         // la plus haute vague gagnée
            public int combo;        // le meilleur combo de vagues parfaites
            public int bananas;      // les bananes gagnées pendant la partie
            public int game;         // numéro de la partie (pour retrouver la partie en cours)
        }

        [Serializable] class EntryList { public List<Entry> entries = new List<Entry>(); }

        // Un trophée : son nom sur la plaque, et le test qui le donne
        public class Trophy
        {
            public readonly string title;
            public readonly Func<bool> earned;
            public Trophy(string title, Func<bool> earned) { this.title = title; this.earned = earned; }
        }

        // La partie en cours
        public static int Bananas { get; private set; }
        public static int BalloonsPopped { get; private set; }
        public static Entry Current { get; private set; }

        public static readonly Trophy[] Trophies =
        {
            new Trophy("VAGUE 10", () => GameState.BestWave >= 10),
            new Trophy("VAGUE 25", () => GameState.BestWave >= 25),
            new Trophy("VAGUE 50", () => GameState.BestWave >= 50),
            new Trophy("VAGUE 100", () => GameState.BestWave >= 100),
            new Trophy("1 000 BANANES", () => Bananas >= 1000),
            new Trophy("10 000 BANANES", () => Bananas >= 10000),
            new Trophy("COMBO x5", () => GameState.BestCombo >= WaveSpawner.MaxCombo),
            new Trophy("1 000 BALLONS", () => BalloonsPopped >= 1000),
        };

        public static event Action Changed;              // le mur se met à jour
        public static event Action<Trophy> TrophyEarned; // un nouveau trophée : le mur le fait briller

        static List<Entry> board;
        static int trophyBits;   // un bit par trophée gagné (gardé sur le casque)

        // Sans rechargement du domaine (Enter Play Mode rapide), on repart de ce qui est enregistré
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState() { board = null; Current = null; Bananas = 0; BalloonsPopped = 0; }

        public static IReadOnlyList<Entry> Board { get { Load(); return board; } }
        public static bool Has(int trophy) { Load(); return (trophyBits & (1 << trophy)) != 0; }

        // Une nouvelle partie commence (au lancement, ou REJOUER) : un nouveau numéro, compteurs à zéro
        public static void NewGame()
        {
            Load();
            int game = PlayerPrefs.GetInt(GameCountKey, 0) + 1;
            PlayerPrefs.SetInt(GameCountKey, game);
            Current = new Entry { name = $"PARTIE {game}", date = DateTime.Now.ToString("dd/MM"), game = game };
            Bananas = 0;
            BalloonsPopped = 0;
            Changed?.Invoke();
        }

        public static void AddBananas(int amount) { Bananas += amount; CheckTrophies(); }
        public static void AddPopped(int layers) { BalloonsPopped += layers; }

        // Une vague est gagnée : on met à jour la partie dans le classement et on enregistre tout
        public static void WaveWon()
        {
            if (Current == null) NewGame();
            Current.wave = GameState.BestWave;
            Current.combo = GameState.BestCombo;
            Current.bananas = Bananas;
            board.RemoveAll(e => e.game == Current.game);
            board.Add(Current);
            board.Sort((a, b) => b.wave != a.wave ? b.wave.CompareTo(a.wave) : b.bananas.CompareTo(a.bananas));
            if (board.Count > BoardSize) board.RemoveRange(BoardSize, board.Count - BoardSize);
            CheckTrophies();
            Save();
            Changed?.Invoke();
        }

        // Le rang de la partie en cours (1 = première), 0 si elle n'est pas (encore) dans le classement
        public static int CurrentRank()
        {
            if (Current == null) return 0;
            var list = Board;
            for (int i = 0; i < list.Count; i++) if (list[i].game == Current.game) return i + 1;
            return 0;
        }

        static void CheckTrophies()
        {
            Load();
            for (int i = 0; i < Trophies.Length; i++)
            {
                if (Has(i) || !Trophies[i].earned()) continue;
                trophyBits |= 1 << i;
                PlayerPrefs.SetInt(TrophyKey, trophyBits);
                PlayerPrefs.Save();
                TrophyEarned?.Invoke(Trophies[i]);
            }
        }

        static void Load()
        {
            if (board != null) return;
            trophyBits = PlayerPrefs.GetInt(TrophyKey, 0);
            board = new List<Entry>();
            try
            {
                var json = PlayerPrefs.GetString(BoardKey, "");
                if (json.Length > 0) board = JsonUtility.FromJson<EntryList>(json).entries ?? board;
            }
            catch (ArgumentException) { board = new List<Entry>(); }   // donnée abîmée : on repart d'un classement vide
        }

        static void Save()
        {
            PlayerPrefs.SetString(BoardKey, JsonUtility.ToJson(new EntryList { entries = board }));
            PlayerPrefs.Save();   // tout de suite : sur le Quest, l'appli peut être coupée sans prévenir
        }

        // Pour les tests (menu clic droit du mur) : tout effacer
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(BoardKey);
            PlayerPrefs.DeleteKey(TrophyKey);
            board = null;
            Load();
            Changed?.Invoke();
        }
    }
}
