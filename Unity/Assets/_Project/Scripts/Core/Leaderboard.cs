using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace SAE
{
    // Le classement : LOCAL (gardé sur le casque) et EN LIGNE (une base MySQL sur le serveur de l'IUT, server/classement/).
    // Une ligne par partie : 3 lettres façon borne d'arcade, la plus haute vague gagnée, les bananes gagnées.
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
        const string NameKey = "SAE.Initiales";

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

        public static event Action Changed;

        static List<Entry> local;
        static List<Entry> online = new List<Entry>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState() { local = null; online = new List<Entry>(); OnlineReady = OnlineFailed = false; GameId = null; }

        // Les initiales choisies sur le tableau (gardées sur le casque d'une partie à l'autre)
        public static string Initials
        {
            get => PlayerPrefs.GetString(NameKey, "AAA");
            set { PlayerPrefs.SetString(NameKey, value); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        // Une nouvelle partie : un nouvel identifiant tiré au hasard (32 caractères hexadécimaux, comme l'attend le serveur)
        public static void NewGame() => GameId = Guid.NewGuid().ToString("N");

        // Une vague vient d'être gagnée : on enregistre la partie sur le casque, puis on l'envoie au serveur
        public static void WaveWon()
        {
            if (GameId == null) NewGame();
            Load();
            var entry = new Entry { name = Initials, wave = GameState.WavesWon, bananas = GameState.BananasEarned, game = GameId };
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
