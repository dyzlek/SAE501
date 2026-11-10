using UnityEngine;
using UnityEngine.SceneManagement;

namespace SAE
{
    // Deux boutons de test au casque, posés derrière chaque bouton LANCER (hub et carte) :
    //   « 200 SINGES » : remplit la carte de 200 singes blancs (types au hasard), pour voir si ça lague ;
    //   « VAGUE CHEAT » : la vague 100 ultra cheatée (AdminCheats.UltraWave).
    // On les enfonce avec la main ou on les vise au rayon, comme les autres boutons.
    // Seulement dans l'éditeur et les builds « Development Build » (comme la montre FPS) : jamais dans le build rendu.
    public class AdminButton : MonoBehaviour, IPressable
    {
        public enum Action { Monkeys, UltraWave }

        const int MonkeyCount = 200;
        const float Size = 0.09f;                       // 9 cm : se touche facilement du bout de la manette
        static readonly Color MonkeysColor = new Color(0.95f, 0.6f, 0.1f);
        static readonly Color WaveColor = new Color(0.6f, 0.15f, 0.8f);

        public Action action;
        Vector3 rest;
        float pressed;                                  // 1 = enfoncé, revient à 0

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Setup()
        {
            if (!Debug.isDebugBuild) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;   // pas de rechargement du code au Play : on évite de s'abonner deux fois
            SceneManager.sceneLoaded += OnSceneLoaded;
            for (int i = 0; i < SceneManager.sceneCount; i++) AddTo(SceneManager.GetSceneAt(i));
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => AddTo(scene);

        // Derrière chaque bouton LANCER de la scène, côte à côte, un peu au-dessus du pupitre (à portée de main)
        static void AddTo(Scene scene)
        {
            foreach (var cube in Object.FindObjectsByType<ActionCube>(FindObjectsSortMode.None))
            {
                if (cube.gameObject.scene != scene || cube.action != ActionCube.Action.StartWave) continue;
                if (cube.transform.parent.Find("Admin 200 SINGES")) continue;   // déjà posés
                Create(cube.transform, Action.Monkeys, "200 SINGES", MonkeysColor, -0.07f);
                Create(cube.transform, Action.UltraWave, "VAGUE CHEAT", WaveColor, 0.07f);
            }
        }

        // Un cube de couleur avec son nom au-dessus, dans le repère du dessus du pupitre (1 unité = 1 m)
        static void Create(Transform launch, Action action, string text, Color color, float side)
        {
            var top = launch.parent;
            var pos = launch.localPosition + new Vector3(side, 0.08f, 0.17f);
            var go = Visuals.Solid($"Admin {text}", top, pos, Vector3.one * Size, color);
            go.tag = Tags.Bouton;
            go.AddComponent<AdminButton>().action = action;   // avant RayPress : il cherche l'IPressable dans son Awake
            go.AddComponent<RayPress>();
            Visuals.Text(top, text, pos + Vector3.up * 0.08f, 0.03f, Color.white);   // texte de 3 cm, tourné vers le joueur comme le pupitre
        }

        void Start() => rest = transform.localPosition;

        void Update()
        {
            pressed = Mathf.MoveTowards(pressed, 0f, Time.deltaTime * 5f);
            transform.localPosition = rest + Vector3.down * (0.012f * pressed);
        }

        public void Press()
        {
            pressed = 1f;
            if (action == Action.Monkeys) AdminCheats.FillMap(MonkeyCount, null, Rarity.Blanc);
            else AdminCheats.UltraWave();
        }
    }
}
