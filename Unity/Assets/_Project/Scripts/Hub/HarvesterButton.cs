using UnityEngine;

namespace SAE
{
    // Bouton rond du comptoir « RÉCOLTEUR » : acheter un singe de plus, ou améliorer la vitesse, la cadence, le rendement
    // de toute l'équipe. Même fonctionnement que les boutons du bananier (UpgradeButton) : on l'enfonce avec la main
    // ou on le vise avec le rayon ; vert si on peut, gris sinon, flash rouge si refusé.
    // Quand c'est fini (niveau max, ou équipe complète), le bouton disparaît et l'ardoise affiche MAX.
    public class HarvesterButton : MonoBehaviour, IPressable
    {
        public HarvesterCrew crew;
        public bool isBuyButton;               // true : acheter un singe ; false : amélioration de « stat »
        public HarvesterStat stat;
        public Transform cap;                  // le dessus du bouton, qui s'enfonce
        public TextMesh label;                 // le texte sur l'ardoise, au-dessus du bouton

        Vector3 capRest;
        float pressed, refused;
        ColorTint capTint;

        public void Press()
        {
            pressed = 1f;
            bool ok = isBuyButton ? crew.Buy() : crew.Upgrade(stat);
            if (!ok) refused = 1f;
        }

        void Start()
        {
            capRest = cap.localPosition;
            capTint = cap.GetComponent<ColorTint>();
        }

        void Update()
        {
            bool done = isBuyButton ? crew.Full : crew.IsMax(stat);
            label.text = Text(done, isBuyButton ? crew.PriceToBuy : crew.Price(stat));
            if (done) { gameObject.SetActive(false); return; }   // plus rien à acheter ici : le bouton s'en va

            pressed = Mathf.MoveTowards(pressed, 0f, Time.deltaTime * 5f);
            refused = Mathf.MoveTowards(refused, 0f, Time.deltaTime * 2f);
            cap.localPosition = capRest + Vector3.down * (0.015f * pressed);

            int price = isBuyButton ? crew.PriceToBuy : crew.Price(stat);
            bool usable = isBuyButton || HarvesterCrew.Count > 0;   // on n'améliore pas une équipe vide
            var color = usable && Economy.CanAfford(price) ? UpgradeButton.Affordable : UpgradeButton.TooExpensive;
            capTint.Set(Color.Lerp(color, UpgradeButton.Refused, refused));
        }

        string Text(bool done, int price)
        {
            if (isBuyButton)
                return UpgradeButton.Board("SINGES", $"{HarvesterCrew.Count} / {crew.monkeys.Length}", "un de plus", price, done);

            string title = stat switch
            {
                HarvesterStat.Vitesse => "VITESSE",
                HarvesterStat.Cadence => "CADENCE",
                _ => "RENDEMENT",
            };
            string effect = stat switch
            {
                HarvesterStat.Vitesse => $"{crew.Speed:0.0} m/s",
                HarvesterStat.Cadence => $"pause {crew.Pause:0.0} s",
                _ => $"{crew.Share * 100f:0} % payé",
            };
            return UpgradeButton.Board(title, $"niv {crew.Level(stat)} / {HarvesterCrew.MaxLevel}", effect, price, done);
        }
    }
}
