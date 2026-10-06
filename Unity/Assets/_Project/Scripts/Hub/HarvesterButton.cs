using UnityEngine;

namespace SAE
{
    // Bouton rond du panneau « RÉCOLTEUR » : acheter le singe, ou améliorer sa vitesse, sa cadence, son rendement.
    // Même fonctionnement que les boutons du bananier (UpgradeButton) : on l'enfonce avec la main ou on le vise
    // avec le rayon ; vert si on peut, gris sinon, doré au niveau max, flash rouge si refusé.
    public class HarvesterButton : MonoBehaviour, IPressable
    {
        public HarvesterMonkey monkey;
        public bool isBuyButton;               // true : bouton d'achat ; false : amélioration de « stat »
        public HarvesterStat stat;
        public Transform cap;                  // le dessus du bouton, qui s'enfonce
        public TextMesh label;

        static readonly Color Available = new Color(0.25f, 0.85f, 0.35f);
        static readonly Color Unavailable = new Color(0.45f, 0.45f, 0.48f);
        static readonly Color Done = new Color(1f, 0.8f, 0.2f);
        static readonly Color Refused = new Color(0.95f, 0.25f, 0.25f);

        Vector3 capRest;
        float pressed, refused;
        ColorTint capTint;

        public void Press()
        {
            pressed = 1f;
            bool ok = isBuyButton ? monkey.Buy() : monkey.Upgrade(stat);
            if (!ok) refused = 1f;
        }

        void Start()
        {
            capRest = cap.localPosition;
            capTint = cap.GetComponent<ColorTint>();
        }

        void Update()
        {
            pressed = Mathf.MoveTowards(pressed, 0f, Time.deltaTime * 5f);
            refused = Mathf.MoveTowards(refused, 0f, Time.deltaTime * 2f);
            cap.localPosition = capRest + Vector3.down * (0.03f * pressed);

            bool done = isBuyButton ? monkey.Bought : monkey.IsMax(stat);
            int price = isBuyButton ? monkey.PriceToBuy : monkey.Price(stat);
            bool usable = isBuyButton || monkey.Bought;   // on n'améliore pas un singe qu'on n'a pas
            var color = done ? Done : usable && Economy.CanAfford(price) ? Available : Unavailable;
            capTint.Set(Color.Lerp(color, Refused, refused));
            label.text = Text(done, price);
        }

        string Text(bool done, int price)
        {
            string cost = price == 0 ? "GRATUIT" : price.ToString();
            if (isBuyButton) return done ? "SINGE\nRÉCOLTEUR\nau travail !" : $"SINGE\nRÉCOLTEUR\nacheter\n{cost}";

            string title = stat switch
            {
                HarvesterStat.Vitesse => "VITESSE",
                HarvesterStat.Cadence => "CADENCE",
                _ => "RENDEMENT",
            };
            string effect = stat switch
            {
                HarvesterStat.Vitesse => $"{monkey.Speed:0.0} m/s",
                HarvesterStat.Cadence => $"pause {monkey.Pause:0.0} s",
                _ => $"{monkey.Share * 100f:0} % payé",
            };
            return $"{title}\nniv {monkey.Level(stat)}\n{effect}\n{(done ? "MAX" : cost)}";
        }
    }
}
