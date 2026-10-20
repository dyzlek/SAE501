using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SAE
{
    // Mode « PC admin » : un panneau de triche à l'écran pour tester les performances (est-ce que ça lag ?).
    // Il ne s'ouvre que dans l'éditeur, en choisissant SAE → Mode de jeu → PC admin. Jamais dans le build casque.
    // F1 ouvre / ferme le panneau (la souris est libérée tant qu'il est ouvert).
    // Écrit en IMGUI (OnGUI) : c'est un outil de test, pas une interface du jeu, donc pas besoin d'UI en VR.
    public class AdminPanel : MonoBehaviour
    {
        public const string Pref = "SAE.ModeAdmin";

        public static bool Open { get; private set; }

        bool unlimitedMoney;
        int monkeyCount = 50;
        int monkeyType = -1;              // -1 = types au hasard
        Rarity monkeyLevel = Rarity.Blanc;
        int targetWave = 100;
        float fps;
        float fpsTimer;
        int fpsFrames;
        Rect window = new Rect(20, 20, 360, 0);

        static readonly string[] typeNames = new[] { "Hasard" }.Concat(System.Enum.GetNames(typeof(MonkeyType))).ToArray();
        static readonly float[] speeds = { 0.5f, 1f, 2f, 5f };

#if UNITY_EDITOR
        // Créé tout seul au lancement si le mode admin est choisi, et gardé d'une scène à l'autre
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (!UnityEditor.EditorPrefs.GetBool(Pref, false)) return;
            var go = new GameObject("Admin");
            DontDestroyOnLoad(go);
            go.AddComponent<AdminPanel>();
        }
#endif

        void OnDestroy()
        {
            // On remet le jeu normal : ces réglages sont statiques et resteraient au prochain Play
            Open = false;
            Time.timeScale = 1f;
            WaveSpawner.CheatCount = 1;
            WaveSpawner.CheatLayers = 0;
            WaveSpawner.Invincible = false;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f1Key.wasPressedThisFrame) Open = !Open;

            if (unlimitedMoney && Economy.Money < 1_000_000) Economy.Earn(1_000_000 - Economy.Money);

            // FPS moyen sur une demi-seconde (un chiffre qui change à chaque image est illisible)
            fpsFrames++;
            fpsTimer += Time.unscaledDeltaTime;
            if (fpsTimer >= 0.5f) { fps = fpsFrames / fpsTimer; fpsFrames = 0; fpsTimer = 0f; }
        }

        void OnGUI()
        {
            // Toujours affiché en haut à droite : FPS (vert à 72, la cible du Quest) et la charge de la scène
            var style = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 };
            style.normal.textColor = fps >= 72 ? Color.green : fps >= 60 ? Color.yellow : Color.red;
            GUI.Label(new Rect(Screen.width - 270, 10, 260, 70),
                $"<b>{fps:0} fps</b>  ({1000f / Mathf.Max(fps, 1f):0.0} ms)\n" +
                $"Ballons : {Balloon.All.Count}   Singes : {GameState.Placed.Count}\nF1 : panneau admin", style);

            if (Open) window = GUILayout.Window(4242, window, DrawWindow, "ADMIN (tests de perfs)");
        }

        void DrawWindow(int id)
        {
            var rich = new GUIStyle(GUI.skin.label) { richText = true };
            var spawner = WaveSpawner.Instance;

            GUILayout.Label("<b>Argent</b>", rich);
            unlimitedMoney = GUILayout.Toggle(unlimitedMoney, " Argent illimité");
            if (GUILayout.Button("+ 100 000")) Economy.Earn(100_000);

            GUILayout.Label("<b>Singes</b>", rich);
            if (GUILayout.Button("Tout débloquer"))
                foreach (MonkeyType t in System.Enum.GetValues(typeof(MonkeyType))) GameState.Unlock(t);
            monkeyType = GUILayout.SelectionGrid(monkeyType + 1, typeNames, 4) - 1;
            GUILayout.Label($"Rareté : {monkeyLevel}");
            monkeyLevel = (Rarity)Mathf.RoundToInt(GUILayout.HorizontalSlider((int)monkeyLevel, 0, MonkeyData.LevelCount - 1));
            GUILayout.Label($"Nombre : {monkeyCount}");
            monkeyCount = Mathf.RoundToInt(GUILayout.HorizontalSlider(monkeyCount, 1, 300));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Poser sur la carte")) FillMap();
            if (GUILayout.Button("Vider la carte")) { GameState.Placed.Clear(); GameState.NotifyChanged(); }
            GUILayout.EndHorizontal();

            GUILayout.Label("<b>Vagues</b>", rich);
            if (!spawner) GUILayout.Label("(la carte n'est pas chargée)");
            else
            {
                GUILayout.Label($"Vague {spawner.Wave} · vies {spawner.Lives} · {(spawner.Running ? "en cours" : "en préparation")}");
                GUILayout.Label($"Aller à la vague : {targetWave}");
                targetWave = Mathf.RoundToInt(GUILayout.HorizontalSlider(targetWave, 1, 150));
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Aller")) spawner.SetWave(targetWave);
                if (GUILayout.Button("LANCER")) WaveSpawner.Launch();
                if (GUILayout.Button("Tout éclater")) foreach (var b in Balloon.All.ToList()) b.Pop(9999);
                GUILayout.EndHorizontal();
            }
            GUILayout.Label($"Ballons × {WaveSpawner.CheatCount}   couches + {WaveSpawner.CheatLayers}");
            WaveSpawner.CheatCount = Mathf.RoundToInt(GUILayout.HorizontalSlider(WaveSpawner.CheatCount, 1, 20));
            WaveSpawner.CheatLayers = Mathf.RoundToInt(GUILayout.HorizontalSlider(WaveSpawner.CheatLayers, 0, 50));
            WaveSpawner.Invincible = GUILayout.Toggle(WaveSpawner.Invincible, " Vies infinies");
            if (spawner && GUILayout.Button("VAGUE 100 ULTRA CHEATÉE (× 10, + 20 couches)"))
            {
                WaveSpawner.CheatCount = 10;
                WaveSpawner.CheatLayers = 20;
                WaveSpawner.Invincible = true;
                spawner.SetWave(100);
                WaveSpawner.Launch();
            }

            GUILayout.Label("<b>Jeu</b>", rich);
            GUILayout.Label($"Vitesse : × {Time.timeScale:0.#}");
            GUILayout.BeginHorizontal();
            foreach (float speed in speeds)
                if (GUILayout.Button($"× {speed}")) Time.timeScale = speed;
            GUILayout.EndHorizontal();
            if (Tutorial.Active && GUILayout.Button("Finir le tutoriel")) Tutorial.Finish();

            GUI.DragWindow();
        }

        // Pose monkeyCount singes à des places libres tirées au hasard (mêmes règles que la pose à la main)
        void FillMap()
        {
            int placed = 0;
            for (int tries = 0; tries < monkeyCount * 50 && placed < monkeyCount; tries++)
            {
                var pos = new Vector2(Random.Range(-MapLayout.HalfExtent, MapLayout.HalfExtent), Random.Range(-MapLayout.HalfExtent, MapLayout.HalfExtent));
                if (!MapLayout.CanPlace(pos, Placement.PathMargin) || GameState.Nearest(pos, Placement.Spacing) != null) continue;
                var type = monkeyType < 0 ? (MonkeyType)Random.Range(0, MonkeyData.TypeCount) : (MonkeyType)monkeyType;
                GameState.Placed.Add(new PlacedMonkey(new Monkey(type, monkeyLevel), pos));
                placed++;
            }
            GameState.NotifyChanged();
            Debug.Log($"Admin : {placed} singes posés (plus de place libre au-delà).");
        }
    }
}
