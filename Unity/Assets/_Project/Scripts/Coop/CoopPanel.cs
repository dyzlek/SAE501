using UnityEngine;

namespace SAE
{
    // Le panneau COOP du menu principal (même ardoise que les OPTIONS) :
    //   HÉBERGER : cet appareil fait tourner la partie, les autres le rejoignent ;
    //   REJOINDRE : on cherche une partie sur le même Wi-Fi ;
    //   QUITTER LE COOP, et RETOUR.
    // La ligne du milieu dit où on en est (« Tu héberges · 192.168.1.12 », « Connecté · tu es le joueur 2 »…).
    // Une fois en coop, on revient au menu et on appuie sur CONTINUER, comme d'habitude.
    public class CoopPanel : MonoBehaviour
    {
        static readonly Color Slate = new Color(0.13f, 0.17f, 0.16f);
        static readonly Color Wood = new Color(0.38f, 0.24f, 0.12f);
        static readonly Color Gold = new Color(1f, 0.83f, 0.35f);
        static readonly Color Green = new Color(0.3f, 0.75f, 0.35f);
        static readonly Color Blue = new Color(0.25f, 0.5f, 0.85f);
        static readonly Color Red = new Color(0.85f, 0.3f, 0.25f);
        static readonly Color Key = new Color(0.45f, 0.32f, 0.18f);

        TextMesh status;
        WorldButton hostButton, joinButton, leaveButton;
        CoopRole shownRole = (CoopRole)(-1);   // le rôle pour lequel les boutons sont allumés/grisés

        public static CoopPanel Build(Transform parent, Vector3 localPos, System.Action onBack)
        {
            var root = new GameObject("Coop").transform;
            root.SetParent(parent, false);
            root.localPosition = localPos;
            var panel = root.gameObject.AddComponent<CoopPanel>();

            Visuals.Box("Cadre", root, Vector3.zero, new Vector3(1.5f, 1.05f, 0.05f), Wood);
            Visuals.Box("Ardoise", root, new Vector3(0f, 0f, -0.03f), new Vector3(1.4f, 0.95f, 0.02f), Slate);
            Visuals.Text(root, "COOP · MÊME WI-FI", new Vector3(0f, 0.38f, -0.05f), 0.1f, Gold, title: true);
            panel.status = Visuals.Text(root, "", new Vector3(0f, 0.2f, -0.05f), 0.055f, Color.white);

            var size = new Vector2(0.5f, 0.13f);
            panel.hostButton = panel.Button(root, "HÉBERGER", new Vector3(-0.3f, -0.03f, -0.06f), size, Green, Coop.Host);
            panel.joinButton = panel.Button(root, "REJOINDRE", new Vector3(0.3f, -0.03f, -0.06f), size, Blue, Coop.Join);
            panel.leaveButton = panel.Button(root, "QUITTER LE COOP", new Vector3(0f, -0.2f, -0.06f), new Vector2(0.7f, 0.12f), Red, Coop.Leave);
            WorldButton.Create(root, "RETOUR", new Vector3(0f, -0.38f, -0.06f), new Vector2(0.45f, 0.12f), Key, onBack);

            root.gameObject.SetActive(false);
            return panel;
        }

        WorldButton Button(Transform root, string label, Vector3 pos, Vector2 size, Color color, System.Action action)
        {
            WorldButton.Create(root, label, pos, size, color, action);
            return root.Find("Bouton " + label).GetComponent<WorldButton>();
        }

        // Le texte change souvent (joueurs qui arrivent, connexion) : on le relit à chaque image, c'est un simple texte
        void Update()
        {
            bool solo = Coop.Role == CoopRole.Solo;
            status.text = solo
                ? (Coop.Status.Length > 0 ? Coop.Status + "\n" : "") + "Héberge sur un appareil, rejoins sur les autres."
                : Coop.Status + (Coop.IsGuest && Coop.Connected ? "\nRETOUR puis CONTINUER pour jouer" : "");
            if (Coop.Role == shownRole) return;
            shownRole = Coop.Role;
            hostButton.SetInteractable(solo, Green);
            joinButton.SetInteractable(solo, Blue);
            leaveButton.SetInteractable(!solo, Red);
        }
    }
}
