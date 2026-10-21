using UnityEngine;
using UnityEngine.InputSystem;

namespace SAE
{
    // La PAUSE, comme dans un vrai jeu (#158) : le bouton Menu de la manette gauche (ou Échap en mode PC) l'ouvre et la ferme.
    // Le jeu s'arrête (Time.timeScale = 0) et un panneau apparaît à 1,5 m devant le joueur, posé dans le décor (il ne suit
    // pas la tête : confort VR) : REPRENDRE, OPTIONS, MENU PRINCIPAL (la partie est sauvegardée avant d'y aller).
    // Pas de pause dans le menu principal (on y est déjà). Créé une fois par LevelLoader, survit aux changements de scène.
    public class PauseMenu : MonoBehaviour
    {
        const float Distance = 1.5f;     // en mètres devant les yeux
        const float Below = 0.2f;        // un peu sous le regard

        static readonly Color Wood = new Color(0.38f, 0.24f, 0.12f);
        static readonly Color Slate = new Color(0.13f, 0.17f, 0.16f);
        static readonly Color Gold = new Color(1f, 0.83f, 0.35f);
        static readonly Color Green = new Color(0.3f, 0.75f, 0.35f);
        static readonly Color Blue = new Color(0.25f, 0.5f, 0.85f);
        static readonly Color Red = new Color(0.85f, 0.3f, 0.25f);

        static PauseMenu instance;
        InputAction toggle;
        Transform panel;
        GameObject buttons;
        OptionsPanel options;

        public static bool Open => instance && instance.panel && instance.panel.gameObject.activeSelf;

        public static void Ensure()
        {
            if (instance) return;
            var go = new GameObject("Pause");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<PauseMenu>();
        }

        void Awake()
        {
            // Le bouton Menu (☰) de la manette gauche selon le profil de manette, et Échap au clavier
            toggle = new InputAction("Pause", InputActionType.Button);
            toggle.AddBinding("<XRController>{LeftHand}/menuButton");
            toggle.AddBinding("<XRController>{LeftHand}/menu");
            toggle.AddBinding("<Keyboard>/escape");
            toggle.performed += _ => Toggle();
            toggle.Enable();
            Build();
        }

        void OnDestroy() => toggle?.Dispose();

        void Build()
        {
            panel = new GameObject("Panneau de pause").transform;
            panel.SetParent(transform, false);
            Visuals.Box("Cadre", panel, Vector3.zero, new Vector3(1.2f, 0.95f, 0.05f), Wood);
            Visuals.Box("Ardoise", panel, new Vector3(0f, 0f, -0.03f), new Vector3(1.1f, 0.85f, 0.02f), Slate);
            buttons = new GameObject("Boutons");
            buttons.transform.SetParent(panel, false);
            var b = buttons.transform;
            Visuals.Text(b, "PAUSE", new Vector3(0f, 0.3f, -0.05f), 0.12f, Gold, title: true);
            var size = new Vector2(0.7f, 0.15f);
            WorldButton.Create(b, "REPRENDRE", new Vector3(0f, 0.1f, -0.06f), size, Green, Close);
            WorldButton.Create(b, "OPTIONS", new Vector3(0f, -0.1f, -0.06f), size, Blue, () => ShowOptions(true));
            WorldButton.Create(b, "MENU PRINCIPAL", new Vector3(0f, -0.3f, -0.06f), size, Red, ToMainMenu);
            options = OptionsPanel.Build(panel, new Vector3(0f, 0f, -0.08f), () => ShowOptions(false));
            panel.gameObject.SetActive(false);
        }

        void Toggle()
        {
            if (GameMenu.InMenu || ScreenCurtain.Busy) return;   // pas de pause dans le menu, ni pendant une téléportation
            if (Open) Close(); else Show();
        }

        void Show()
        {
            // Devant les yeux, à l'horizontale (pas penché même si on regarde en bas), tourné vers le joueur
            var cam = Camera.main;
            if (!cam) return;
            var forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.ProjectOnPlane(cam.transform.up, Vector3.up);
            forward.Normalize();
            panel.SetPositionAndRotation(cam.transform.position + forward * Distance + Vector3.down * Below, Quaternion.LookRotation(forward));
            ShowOptions(false);
            panel.gameObject.SetActive(true);
            Time.timeScale = 0f;
        }

        void Close()
        {
            panel.gameObject.SetActive(false);
            if (!GameMenu.InMenu) Time.timeScale = 1f;
        }

        void ShowOptions(bool on)
        {
            buttons.SetActive(!on);
            options.gameObject.SetActive(on);
        }

        void ToMainMenu()
        {
            panel.gameObject.SetActive(false);
            GameMenu.ToMenu();
        }
    }
}
