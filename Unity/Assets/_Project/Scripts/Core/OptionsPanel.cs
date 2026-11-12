using UnityEngine;

namespace SAE
{
    // Le panneau OPTIONS (menu principal et pause) : une ardoise à deux pages.
    //   Page « réglages » : MUSIQUE [-] 8 / 10 [+] · SONS [-] 10 / 10 [+] · VIBRATIONS [OUI / NON] ·
    //                       DÉPLACEMENT [TÉLÉPORT / STICK] · boutons COMMANDES et RETOUR.
    //   Page « commandes » : toutes les touches, au casque et au clavier (remplace l'aide qui était en bas de l'écran
    //                        en mode PC), et un bouton RETOUR vers les réglages.
    // Les réglages sont dans Options (gardés sur le casque). onBack : ce que fait RETOUR (fermer le panneau).
    public class OptionsPanel : MonoBehaviour
    {
        static readonly Color Slate = new Color(0.13f, 0.17f, 0.16f);
        static readonly Color Wood = new Color(0.38f, 0.24f, 0.12f);
        static readonly Color Gold = new Color(1f, 0.83f, 0.35f);
        static readonly Color Key = new Color(0.45f, 0.32f, 0.18f);
        static readonly Color Green = new Color(0.3f, 0.75f, 0.35f);
        static readonly Color Blue = new Color(0.25f, 0.5f, 0.85f);

        // Le texte de la page COMMANDES, une colonne par façon de jouer
        const string HeadsetControls =
            "Stick : se téléporter\n(ou marcher, option STICK)\n" +
            "Stick droit : tourner\n" +
            "GRIP : prendre, lâcher\n" +
            "Gâchette : appuyer\n" +
            "A : infos du singe\n" +
            "Menu (gauche) : pause";
        const string KeyboardControls =
            "ZQSD : marcher\n" +
            "Souris : regarder\n" +
            "Clic : appuyer, prendre\n" +
            "Relâcher : lâcher, lancer\n" +
            "A (maintenu) : infos du singe\n" +
            "Échap : pause";

        TextMesh music, sound, haptics, move;
        GameObject settingsPage, controlsPage;

        // Construit le panneau sous parent, centré en localPos (1,4 × 1 m), et le renvoie (caché)
        public static OptionsPanel Build(Transform parent, Vector3 localPos, System.Action onBack)
        {
            var root = new GameObject("Options").transform;
            root.SetParent(parent, false);
            root.localPosition = localPos;
            var panel = root.gameObject.AddComponent<OptionsPanel>();

            Visuals.Box("Cadre", root, new Vector3(0f, -0.09f, 0f), new Vector3(1.5f, 1.23f, 0.05f), Wood);
            Visuals.Box("Ardoise", root, new Vector3(0f, -0.09f, -0.03f), new Vector3(1.4f, 1.13f, 0.02f), Slate);
            panel.settingsPage = panel.BuildSettings(root, onBack);
            panel.controlsPage = panel.BuildControls(root);

            panel.ShowControls(false);
            panel.Refresh();
            root.gameObject.SetActive(false);
            return panel;
        }

        GameObject BuildSettings(Transform root, System.Action onBack)
        {
            var page = Page(root, "Réglages");
            Visuals.Text(page, "OPTIONS", new Vector3(0f, 0.36f, -0.05f), 0.11f, Gold, title: true);
            music = Row(page, "MUSIQUE", 0.16f, () => Options.Music--, () => Options.Music++);
            sound = Row(page, "SONS", -0.02f, () => Options.Sound--, () => Options.Sound++);
            Visuals.Text(page, "VIBRATIONS", new Vector3(-0.38f, -0.2f, -0.05f), 0.06f, Color.white, title: true);
            haptics = WorldButton.Create(page, "", new Vector3(0.35f, -0.2f, -0.06f), new Vector2(0.36f, 0.12f), Key,
                () => Options.Vibrations = !Options.Vibrations, 0.06f);
            // Au casque : téléportation (par défaut, le plus confortable) ou marche au stick gauche (comme ZQSD au PC)
            Visuals.Text(page, "DÉPLACEMENT", new Vector3(-0.38f, -0.38f, -0.05f), 0.06f, Color.white, title: true);
            move = WorldButton.Create(page, "", new Vector3(0.35f, -0.38f, -0.06f), new Vector2(0.36f, 0.12f), Key,
                () => Options.StickMove = !Options.StickMove, 0.05f);
            WorldButton.Create(page, "COMMANDES", new Vector3(-0.3f, -0.56f, -0.06f), new Vector2(0.5f, 0.12f), Blue, () => ShowControls(true));
            WorldButton.Create(page, "RETOUR", new Vector3(0.3f, -0.56f, -0.06f), new Vector2(0.5f, 0.12f), Green, onBack);
            return page.gameObject;
        }

        GameObject BuildControls(Transform root)
        {
            var page = Page(root, "Commandes");
            Visuals.Text(page, "COMMANDES", new Vector3(0f, 0.36f, -0.05f), 0.11f, Gold, title: true);
            Visuals.Text(page, "CASQUE", new Vector3(-0.33f, 0.2f, -0.05f), 0.07f, Gold, title: true);
            Visuals.Text(page, "CLAVIER", new Vector3(0.33f, 0.2f, -0.05f), 0.07f, Gold, title: true);
            Visuals.Text(page, HeadsetControls, new Vector3(-0.33f, -0.13f, -0.05f), 0.045f, Color.white);
            Visuals.Text(page, KeyboardControls, new Vector3(0.33f, -0.13f, -0.05f), 0.045f, Color.white);
            WorldButton.Create(page, "RETOUR", new Vector3(0f, -0.56f, -0.06f), new Vector2(0.45f, 0.12f), Green, () => ShowControls(false));
            return page.gameObject;
        }

        static Transform Page(Transform root, string name)
        {
            var page = new GameObject(name).transform;
            page.SetParent(root, false);
            return page;
        }

        // true = page des commandes, false = page des réglages
        void ShowControls(bool on)
        {
            settingsPage.SetActive(!on);
            controlsPage.SetActive(on);
        }

        // Une ligne « NOM  [-] n / 10 [+] » ; renvoie le texte du nombre
        TextMesh Row(Transform page, string label, float y, System.Action minus, System.Action plus)
        {
            Visuals.Text(page, label, new Vector3(-0.38f, y, -0.05f), 0.06f, Color.white, title: true);
            WorldButton.Create(page, "-", new Vector3(0.12f, y, -0.06f), new Vector2(0.12f, 0.12f), Key, minus);
            WorldButton.Create(page, "+", new Vector3(0.58f, y, -0.06f), new Vector2(0.12f, 0.12f), Key, plus);
            return Visuals.Text(page, "", new Vector3(0.35f, y, -0.05f), 0.06f, Gold, title: true);
        }

        // Le panneau s'ouvre toujours sur les réglages
        void OnEnable() { Options.Changed += Refresh; Refresh(); if (settingsPage) ShowControls(false); }
        void OnDisable() => Options.Changed -= Refresh;

        void Refresh()
        {
            if (!music) return;
            music.text = $"{Options.Music} / {Options.Steps}";
            sound.text = $"{Options.Sound} / {Options.Steps}";
            haptics.text = Options.Vibrations ? "OUI" : "NON";
            move.text = Options.StickMove ? "STICK" : "TÉLÉPORT";
        }
    }
}
