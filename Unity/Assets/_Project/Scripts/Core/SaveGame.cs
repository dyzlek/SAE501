using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SAE
{
    // La SAUVEGARDE de la partie, sur le casque (PlayerPrefs, en JSON) : on reprend où on en était au lancement suivant.
    // Ce qui est gardé : argent, vagues gagnées, bananes gagnées, singes de la bibliothèque, types débloqués, singes posés,
    // améliorations de l'arc, arbres plantés et leurs améliorations, singes récolteurs et leurs améliorations,
    // tutoriel fini ou non, et l'identifiant de la partie (le classement continue la même ligne au lieu d'en créer une).
    //   Load()  : au lancement, AVANT les scènes, remet tout ce qui est « statique » (GameState, BowUpgrades…).
    //             Ce qui vit dans la scène (bananiers, récolteurs) se restaure dans leur Start (voir Pending).
    //   Save()  : à chaque vague gagnée, toutes les 30 s, et quand le casque met le jeu en pause ou le ferme.
    //   NewGame() : le bouton NOUVELLE PARTIE du panneau du classement : on efface et on recommence de zéro.
    // Choix de périmètre : le GDD disait « pas de sauvegarde » ; Dylan l'a demandée le 21 oct. (journal GENERAL.md).
    // Coop : un invité joue la partie de l'hôte, il ne sauvegarde donc rien (sa propre partie reste intacte) ;
    //   en quittant le coop, il la retrouve (ReloadFromDisk).
    public static class SaveGame
    {
        const string Key = "SAE.Sauvegarde";
        const int Version = 1;               // à augmenter si le format change (une vieille sauvegarde est alors ignorée)
        const float AutoSaveSeconds = 30f;

        [Serializable]
        public class Data
        {
            public int version = Version;
            public string gameId;
            public int money, wavesWon, bananasEarned;
            public int[] owned;                // nombre de singes rangés, par type × rareté (à plat)
            public bool[] unlocked;            // types débloqués
            public List<Placed> placed = new List<Placed>();
            public int[] bow;                  // paliers des améliorations de l'arc
            public int trees;                  // arbres plantés (1 à 3)
            public int[] treeLevels;           // fréquence, pourriture, valeur (les arbres sont tous au même niveau)
            public int harvesters;             // singes récolteurs achetés
            public int[] harvesterLevels;      // vitesse, cadence, rendement
            public bool tutorialDone;
        }

        [Serializable]
        public class Placed { public int type, level; public float x, z; }

        // La sauvegarde lue au lancement : les objets de la scène y prennent leur part dans leur Start (null = nouvelle partie)
        public static Data Pending { get; private set; }
        public static bool HasSave => PlayerPrefs.HasKey(Key);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Load()
        {
            Pending = null;
            Runner.Ensure();
            if (GameState.Sandbox || !HasSave) return;   // le bac à sable de l'éditeur ne lit ni n'écrit la sauvegarde
            Data d;
            try { d = JsonUtility.FromJson<Data>(PlayerPrefs.GetString(Key)); }
            catch (ArgumentException) { return; }        // sauvegarde abîmée : nouvelle partie
            if (d == null || d.version != Version) return;

            GameState.Restore(d.money, d.wavesWon, d.bananasEarned, d.owned, d.unlocked);
            foreach (var p in d.placed)
                GameState.Placed.Add(new PlacedMonkey(new Monkey((MonkeyType)p.type, (Rarity)p.level), new Vector2(p.x, p.z)));
            BowUpgrades.Restore(d.bow);
            if (d.tutorialDone) Tutorial.RestoreDone();
            Leaderboard.ContinueGame(d.gameId);
            Pending = d;
        }

        public static void Save()
        {
            if (GameState.Sandbox || !Application.isPlaying || Coop.IsGuest) return;
            var d = new Data
            {
                gameId = Leaderboard.GameId,
                money = GameState.Money,
                wavesWon = GameState.WavesWon,
                bananasEarned = GameState.BananasEarned,
                owned = GameState.OwnedFlat(),
                unlocked = GameState.UnlockedCopy(),
                bow = BowUpgrades.Levels(),
                tutorialDone = !Tutorial.Active,
            };
            foreach (var p in GameState.Placed)
                d.placed.Add(new Placed { type = (int)p.monkey.type, level = (int)p.monkey.level, x = p.pos.x, z = p.pos.y });
            if (GameState.Held != null) d.owned[GameState.Index(GameState.Held.Value)]++;   // le singe tenu en main retourne à la bibliothèque

            // Ce qui vit dans la scène du hub (si elle est chargée ; sinon on garde ce qui avait été lu)
            var orchard = UnityEngine.Object.FindAnyObjectByType<BananaOrchard>();
            var keep = Pending;
            d.trees = orchard ? orchard.Count : keep?.trees ?? 1;
            d.treeLevels = orchard ? orchard.Levels() : keep?.treeLevels;
            var crew = UnityEngine.Object.FindAnyObjectByType<HarvesterCrew>();
            d.harvesters = crew ? crew.Count : keep?.harvesters ?? 0;
            d.harvesterLevels = crew ? crew.Levels() : keep?.harvesterLevels;

            PlayerPrefs.SetString(Key, JsonUtility.ToJson(d));
            PlayerPrefs.Save();   // tout de suite : sur le Quest, l'appli peut être coupée sans prévenir
        }

        // NOUVELLE PARTIE : on efface la sauvegarde, on remet tout à zéro et on recharge la cabane
        public static void NewGame()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
            Pending = null;
            GameState.ResetAll();
            BowUpgrades.Reset();
            Tutorial.Restart();
            Levels.Restart();
            Leaderboard.NewGame();
            SceneManager.LoadScene(Levels.HubScene, LoadSceneMode.Single);   // Single : la carte est déchargée avec
        }

        // Coop : l'invité quitte la partie de l'hôte. On remet tout à zéro, on relit SA sauvegarde et on recharge la cabane.
        public static void ReloadFromDisk()
        {
            GameState.ResetAll();
            BowUpgrades.Reset();
            Tutorial.Restart();
            Levels.Restart();
            Load();
            SceneManager.LoadScene(Levels.HubScene, LoadSceneMode.Single);
        }

        // Un objet caché qui sauvegarde régulièrement, et quand le casque met le jeu en pause (bouton Meta, casque retiré)
        class Runner : MonoBehaviour
        {
            static Runner instance;
            float next;

            public static void Ensure()
            {
                if (instance) return;
                var go = new GameObject("Sauvegarde") { hideFlags = HideFlags.HideInHierarchy };
                DontDestroyOnLoad(go);
                instance = go.AddComponent<Runner>();
            }

            void Update()
            {
                if (Time.unscaledTime < next) return;
                next = Time.unscaledTime + AutoSaveSeconds;
                if (Time.unscaledTime > 5f) Save();   // pas pendant le chargement
            }

            void OnApplicationPause(bool paused) { if (paused) Save(); }
            void OnApplicationQuit() => Save();
        }
    }
}
