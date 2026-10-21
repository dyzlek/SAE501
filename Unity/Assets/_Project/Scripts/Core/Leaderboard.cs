using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace SAE
{
    // Le classement : LOCAL (gardé sur le casque) et EN LIGNE (une base MySQL sur le serveur de l'IUT, server/classement/).
    // Une ligne par partie : le nom du joueur, la plus haute vague gagnée, les bananes gagnées.
    // NOMS UNIQUES : au premier lancement, le casque tire un identifiant caché (gardé sur le casque) et un nom par défaut
    // (SINGE-4821). Le serveur réserve un nom pour le premier casque qui le prend : personne d'autre ne peut l'utiliser.
    // On change de nom au clavier du panneau : le nom n'est gardé que si le serveur dit qu'il est libre (TryRename).
    //   - À chaque vague gagnée (WaveSpawner), la partie en cours est enregistrée sur le casque (PlayerPrefs) et envoyée
    //     au serveur. Le serveur ne la fait jamais baisser.
    //   - Le tableau du hub (LeaderboardBoard) affiche les deux colonnes : ICI et MONDE.
    // Sans réseau, rien ne casse : le classement local marche toujours, la colonne MONDE dit « hors ligne ».
    // Ce ne sont que des scores : rien ne rend la partie suivante plus facile (pas de méta-progression, voir le GDD).
    public static class Leaderboard
    {
        // L'API du serveur (server/classement/api.php, copié dans le dossier « bloons » du site). HTTPS : Android refuse le HTTP.
        public const string Url = "https://web-mmi2.iutbeziers.fr/~dylan.belledent/bloons/api.php";
        const string Key = "bloons-vr-sae501";   // même valeur que GAME_KEY dans config.php
        public const int Size = 10;
        const string BoardKey = "SAE.ClassementLocal";
        const string NameKey = "SAE.Nom";
        const string DeviceKey = "SAE.Casque";

        // L'état du nom, affiché sur le panneau
        public enum NameState { Unknown, Checking, Mine, Taken, Offline }

        [Serializable]
        public class Entry
        {
            public string name;
            public int wave;
            public int bananas;
            public string game;      // identifiant de la partie (pour la retrouver dans le classement local)
        }

        [Serializable] class EntryList { public List<Entry> entries = new List<Entry>(); }
        [Serializable] class OnlineList { public List<Entry> scores = new List<Entry>(); }

        public static IReadOnlyList<Entry> Local { get { Load(); return local; } }
        public static IReadOnlyList<Entry> Online => online;
        public static bool OnlineReady { get; private set; }      // la liste en ligne a été reçue au moins une fois
        public static bool OnlineFailed { get; private set; }     // le dernier appel au serveur a échoué
        public static string GameId { get; private set; }
        public static NameState State { get; private set; }

        public static event Action Changed;

        static List<Entry> local;
        static List<Entry> online = new List<Entry>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState() { local = null; online = new List<Entry>(); OnlineReady = OnlineFailed = false; GameId = null; State = NameState.Unknown; }

        // L'identifiant caché du casque : tiré au hasard la première fois, puis toujours le même (jamais affiché)
        public static string Device
        {
            get
            {
                var id = PlayerPrefs.GetString(DeviceKey, "");
                if (id.Length == 32) return id;
                id = Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(DeviceKey, id);
                PlayerPrefs.Save();
                return id;
            }
        }

        // Le nom du joueur (gardé sur le casque). Par défaut « SINGE- » et 4 chiffres, pour être presque sûr qu'il est libre.
        public static string Name
        {
            get
            {
                var name = PlayerPrefs.GetString(NameKey, "");
                if (IsValidName(name)) return name;
                name = "SINGE-" + UnityEngine.Random.Range(1000, 10000);
                PlayerPrefs.SetString(NameKey, name);
                PlayerPrefs.Save();
                return name;
            }
        }

        // Les mêmes règles que le serveur : 3 à 12 caractères, A-Z, 0-9 et « - »
        public const int MinName = 3, MaxName = 12;
        public static bool IsValidName(string name)
        {
            if (name == null || name.Length < MinName || name.Length > MaxName) return false;
            foreach (char c in name) if (!(c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '-')) return false;
            return true;
        }

        // Au lancement : réserve le nom actuel (il l'est sans doute déjà : le serveur répond « ok » pour son propriétaire)
        public static void ClaimCurrent() => Runner.Instance.StartCoroutine(Claim(Name, null));

        // Le clavier propose un nouveau nom : on le réserve sur le serveur ; s'il est libre, il devient le nom du joueur.
        // done(true) = gardé, done(false) = pris par un autre casque ou pas de réseau (State dit lequel).
        public static void TryRename(string name, Action<bool> done) => Runner.Instance.StartCoroutine(Claim(name, done));

        static IEnumerator Claim(string name, Action<bool> done)
        {
            State = NameState.Checking;
            Changed?.Invoke();
            var form = new WWWForm();
            form.AddField("key", Key);
            form.AddField("action", "claim");
            form.AddField("device", Device);
            form.AddField("name", name);
            using (var request = UnityWebRequest.Post(Url, form))
            {
                request.timeout = 5;
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success) State = NameState.Mine;
                else State = request.responseCode == 409 ? NameState.Taken : NameState.Offline;
            }
            bool kept = State == NameState.Mine;
            if (kept)
            {
                PlayerPrefs.SetString(NameKey, name);
                PlayerPrefs.Save();
            }
            done?.Invoke(kept);
            Changed?.Invoke();
        }

        // Une nouvelle partie : un nouvel identifiant tiré au hasard (32 caractères hexadécimaux, comme l'attend le serveur)
        public static void NewGame() => GameId = Guid.NewGuid().ToString("N");

        // Une vague vient d'être gagnée : on enregistre la partie sur le casque, puis on l'envoie au serveur
        public static void WaveWon()
        {
            if (GameId == null) NewGame();
            Load();
            var entry = new Entry { name = Name, wave = GameState.WavesWon, bananas = GameState.BananasEarned, game = GameId };
            local.RemoveAll(e => e.game == GameId);
            local.Add(entry);
            Sort(local);
            if (local.Count > Size) local.RemoveRange(Size, local.Count - Size);
            PlayerPrefs.SetString(BoardKey, JsonUtility.ToJson(new EntryList { entries = local }));
            PlayerPrefs.Save();   // tout de suite : sur le Quest, l'appli peut être coupée sans prévenir
            Changed?.Invoke();
            Runner.Instance.StartCoroutine(Send(entry));
        }

        // Le rang de la partie en cours dans le classement local (0 = pas dedans)
        public static int CurrentRank()
        {
            var list = Local;
            for (int i = 0; i < list.Count; i++) if (list[i].game == GameId) return i + 1;
            return 0;
        }

        // Redemande le top 10 au serveur (au lancement, et après chaque envoi)
        public static void Refresh() => Runner.Instance.StartCoroutine(Fetch());

        static IEnumerator Send(Entry e)
        {
            var form = new WWWForm();
            form.AddField("key", Key);
            form.AddField("action", "score");
            form.AddField("device", Device);
            form.AddField("game_id", e.game);
            form.AddField("name", e.name);
            form.AddField("wave", e.wave);
            form.AddField("bananas", e.bananas);
            using (var request = UnityWebRequest.Post(Url, form))
            {
                request.timeout = 5;   // secondes : le jeu n'attend jamais le serveur, tout se passe en arrière-plan
                yield return request.SendWebRequest();
                OnlineFailed = request.result != UnityWebRequest.Result.Success;
            }
            yield return Fetch();
        }

        static IEnumerator Fetch()
        {
            using (var request = UnityWebRequest.Get(Url))
            {
                request.timeout = 5;
                yield return request.SendWebRequest();
                OnlineFailed = request.result != UnityWebRequest.Result.Success;
                if (!OnlineFailed)
                {
                    try
                    {
                        online = JsonUtility.FromJson<OnlineList>(request.downloadHandler.text).scores ?? new List<Entry>();
                        OnlineReady = true;
                    }
                    catch (ArgumentException) { OnlineFailed = true; }   // réponse qui n'est pas du JSON (page d'erreur du serveur)
                }
            }
            Changed?.Invoke();
        }

        static void Sort(List<Entry> list) =>
            list.Sort((a, b) => a.wave != b.wave ? b.wave.CompareTo(a.wave) : b.bananas.CompareTo(a.bananas));

        static void Load()
        {
            if (local != null) return;
            local = new List<Entry>();
            try
            {
                var json = PlayerPrefs.GetString(BoardKey, "");
                if (json.Length > 0) local = JsonUtility.FromJson<EntryList>(json).entries ?? local;
            }
            catch (ArgumentException) { local = new List<Entry>(); }   // donnée abîmée : classement vide
        }

        // Pour les tests (clic droit sur le tableau) : vider le classement local
        public static void ClearLocal()
        {
            PlayerPrefs.DeleteKey(BoardKey);
            local = null;
            Changed?.Invoke();
        }

        // Un objet caché qui fait tourner les envois (une classe statique ne peut pas lancer de coroutine)
        class Runner : MonoBehaviour
        {
            static Runner instance;
            public static Runner Instance
            {
                get
                {
                    if (instance) return instance;
                    var go = new GameObject("Leaderboard") { hideFlags = HideFlags.HideInHierarchy };
                    DontDestroyOnLoad(go);
                    return instance = go.AddComponent<Runner>();
                }
            }
        }
    }
}
