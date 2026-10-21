using UnityEngine;

namespace SAE
{
    // Le panneau OPTIONS (menu principal et pause) : une petite ardoise avec trois lignes, réglées par des boutons
    //   MUSIQUE  [-] 8 / 10 [+]     SONS  [-] 10 / 10 [+]     VIBRATIONS  [OUI / NON]     et RETOUR.
    // Les réglages sont dans Options (gardés sur le casque). onBack : ce que fait RETOUR (fermer le panneau).
    public class OptionsPanel : MonoBehaviour
    {
        static readonly Color Slate = new Color(0.13f, 0.17f, 0.16f);
        static readonly Color Wood = new Color(0.38f, 0.24f, 0.12f);
        static readonly Color Gold = new Color(1f, 0.83f, 0.35f);
        static readonly Color Key = new Color(0.45f, 0.32f, 0.18f);
        static readonly Color Green = new Color(0.3f, 0.75f, 0.35f);

        TextMesh music, sound, haptics;

        // Construit le panneau sous parent, centré en localPos (1,4 × 1 m), et le renvoie (caché)
        public static OptionsPanel Build(Transform parent, Vector3 localPos, System.Action onBack)
        {
            var root = new GameObject("Options").transform;
            root.SetParent(parent, false);
            root.localPosition = localPos;
            var panel = root.gameObject.AddComponent<OptionsPanel>();

            Visuals.Box("Cadre", root, Vector3.zero, new Vector3(1.5f, 1.05f, 0.05f), Wood);
            Visuals.Box("Ardoise", root, new Vector3(0f, 0f, -0.03f), new Vector3(1.4f, 0.95f, 0.02f), Slate);
            Visuals.Text(root, "OPTIONS", new Vector3(0f, 0.36f, -0.05f), 0.11f, Gold, title: true);

            panel.music = panel.Row(root, "MUSIQUE", 0.16f, () => Options.Music--, () => Options.Music++);
            panel.sound = panel.Row(root, "SONS", -0.02f, () => Options.Sound--, () => Options.Sound++);
            Visuals.Text(root, "VIBRATIONS", new Vector3(-0.38f, -0.2f, -0.05f), 0.06f, Color.white, title: true);
            panel.haptics = WorldButton.Create(root, "", new Vector3(0.35f, -0.2f, -0.06f), new Vector2(0.36f, 0.12f), Key,
                () => Options.Vibrations = !Options.Vibrations, 0.06f);
            WorldButton.Create(root, "RETOUR", new Vector3(0f, -0.38f, -0.06f), new Vector2(0.45f, 0.12f), Green, onBack);

            panel.Refresh();
            root.gameObject.SetActive(false);
            return panel;
        }

        // Une ligne « NOM  [-] n / 10 [+] » ; renvoie le texte du nombre
        TextMesh Row(Transform root, string label, float y, System.Action minus, System.Action plus)
        {
            Visuals.Text(root, label, new Vector3(-0.38f, y, -0.05f), 0.06f, Color.white, title: true);
            WorldButton.Create(root, "-", new Vector3(0.12f, y, -0.06f), new Vector2(0.12f, 0.12f), Key, minus);
            WorldButton.Create(root, "+", new Vector3(0.58f, y, -0.06f), new Vector2(0.12f, 0.12f), Key, plus);
            return Visuals.Text(root, "", new Vector3(0.35f, y, -0.05f), 0.06f, Gold, title: true);
        }

        void OnEnable() { Options.Changed += Refresh; Refresh(); }
        void OnDisable() => Options.Changed -= Refresh;

        void Refresh()
        {
            if (!music) return;
            music.text = $"{Options.Music} / {Options.Steps}";
            sound.text = $"{Options.Sound} / {Options.Steps}";
            haptics.text = Options.Vibrations ? "OUI" : "NON";
        }
    }
}
