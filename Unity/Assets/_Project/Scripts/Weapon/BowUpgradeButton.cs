using UnityEngine;

namespace SAE
{
    // Bouton rond du pupitre « ARC », sur l'estrade de la carte : une amélioration de l'arc (voir BowUpgrades).
    // Même fonctionnement que les boutons du bananier (UpgradeButton) : on l'enfonce avec la main ou on le vise
    // avec le rayon ; vert si on peut payer, gris sinon, flash rouge si refusé. Au palier max, il disparaît.
    public class BowUpgradeButton : MonoBehaviour, IPressable
    {
        public BowUpgrade upgrade;
        public Transform cap;          // le dessus du bouton, qui s'enfonce
        public TextMesh label;         // le texte sur l'ardoise, au-dessus du bouton

        Vector3 capRest;
        float pressed, refused;
        ColorTint capTint;

        public void Press()
        {
            pressed = 1f;
            if (!BowUpgrades.Buy(upgrade, transform.position)) refused = 1f;
        }

        void Start()
        {
            capRest = cap.localPosition;
            capTint = cap.GetComponent<ColorTint>();
        }

        void Update()
        {
            bool done = BowUpgrades.IsMax(upgrade);
            label.text = Text(done);
            if (done) { gameObject.SetActive(false); return; }   // plus rien à améliorer : le bouton s'en va

            pressed = Mathf.MoveTowards(pressed, 0f, Time.deltaTime * 5f);
            refused = Mathf.MoveTowards(refused, 0f, Time.deltaTime * 2f);
            cap.localPosition = capRest + Vector3.down * (0.015f * pressed);

            var color = Economy.CanAfford(BowUpgrades.Price(upgrade)) ? UpgradeButton.Affordable : UpgradeButton.TooExpensive;
            capTint.Set(Color.Lerp(color, UpgradeButton.Refused, refused));
        }

        string Text(bool done)
        {
            int level = BowUpgrades.Level(upgrade);
            string title = upgrade switch
            {
                BowUpgrade.Perforation => "PERFORANTE",
                BowUpgrade.Transpercante => "TRANSPERÇANTE",
                BowUpgrade.TirTriple => "TIR TRIPLE",
                _ => "EXPLOSIVE",
            };
            string effect = upgrade switch
            {
                BowUpgrade.Perforation => $"{BowUpgrades.Pierce} couche(s) / ballon",
                BowUpgrade.Transpercante => $"traverse {BowUpgrades.PassThrough} ballon(s)",
                BowUpgrade.TirTriple => level > 0 ? "3 flèches" : "1 flèche",
                _ => level > 0 ? $"{BowUpgrades.ExplosionBalloons} ballons · {BowUpgrades.ExplosionRadius:0.#} m · {BowUpgrades.ExplosionLayers} c." : "aucune",
            };
            string text = UpgradeButton.Board(title, $"niv {level} / {BowUpgrades.MaxLevel(upgrade)}", effect, BowUpgrades.Price(upgrade), done);
            // Bêta-test : on affiche aussi le prix prévu, pour régler l'économie
            if (BowUpgrades.BetaFree && !done) text += $"\n<size=30><color=#9AA89A>prévu : {BowUpgrades.PlannedPrice(upgrade)}</color></size>";
            return text;
        }
    }
}
