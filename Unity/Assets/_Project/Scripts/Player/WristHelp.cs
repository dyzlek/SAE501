using UnityEngine;

namespace SAE
{
    // L'aide-mémoire des commandes, comme une montre : on lève la main gauche devant les yeux et une petite ardoise
    // apparaît au-dessus d'elle, avec les touches. On baisse la main, elle disparaît. Rien n'est collé à l'écran
    // (règle de confort) : l'ardoise suit la main, à portée de bras, et se tourne vers le joueur.
    // Seulement au hub : sur la carte, on lève justement la main gauche pour viser à l'arc.
    public class WristHelp : MonoBehaviour
    {
        public Transform head;                  // la caméra du casque
        public float lookAngle = 25f;           // en degrés : la main doit être à peu près au centre du regard
        public float maxDistance = 0.7f;        // en mètres : main assez proche des yeux (bras levé, pas tendu au loin)

        const string Help =
            "<color=#FFD45A>COMMANDES</color>\n" +
            "Stick : se téléporter · tourner\n" +
            "Grip : prendre un objet\n" +
            "Gâchette : appuyer de loin\n" +
            "Lâcher en bougeant : lancer\n" +
            "A maintenu sur un singe : sa fiche\n" +
            "B : lancer la vague";

        static readonly Color Frame = new Color(0.45f, 0.3f, 0.18f);
        static readonly Color Slate = new Color(0.13f, 0.17f, 0.15f);
        static readonly Color Chalk = new Color(0.95f, 0.94f, 0.88f);

        Transform card;

        void Start()
        {
            card = new GameObject("Aide des commandes").transform;
            card.SetParent(transform, false);
            card.localPosition = new Vector3(0f, 0.16f, 0f);   // au-dessus de la main
            card.gameObject.AddComponent<Billboard>();
            Visuals.Box("Cadre", card, new Vector3(0, 0, 0.004f), new Vector3(0.36f, 0.2f, 0.004f), Frame);
            Visuals.Box("Ardoise", card, new Vector3(0, 0, 0.001f), new Vector3(0.34f, 0.18f, 0.002f), Slate);
            Visuals.Text(card, Help, Vector3.zero, 0.016f, Chalk);
            card.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!card || !head) return;
            var toHand = transform.position - head.position;
            bool looking = Levels.Current == Level.Hub
                && toHand.magnitude < maxDistance
                && Vector3.Angle(head.forward, toHand) < lookAngle;
            if (card.gameObject.activeSelf != looking) card.gameObject.SetActive(looking);
        }
    }
}
