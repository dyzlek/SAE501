using UnityEngine;

namespace SAE
{
    // Un bouton rond du pupitre de la roulette : régler la mise (- 10 / + 10 / tout), lancer la roue, ou le « 100 % » du bêta-test.
    // Même fonctionnement que les boutons d'amélioration (BowUpgradeButton) : on l'enfonce avec la main ou on le vise
    // avec le rayon ; il est gris quand il ne peut rien faire, flash rouge si l'appui est refusé.
    public class RouletteButton : MonoBehaviour, IPressable
    {
        public enum Action { StakeLess, StakeMore, AllIn, Spin, Guarantee }

        public RouletteTable table;
        public Action action;
        public Transform cap;          // le dessus du bouton, qui s'enfonce

        static readonly Color Launch = new Color(0.95f, 0.45f, 0.15f);   // comme LANCER au pupitre de la carte
        static readonly Color Gold = new Color(1f, 0.8f, 0.2f);          // « 100 % » allumé

        Vector3 capRest;
        float pressed, refused;
        ColorTint capTint;

        void Start()
        {
            capRest = cap.localPosition;
            capTint = cap.GetComponent<ColorTint>();
        }

        public void Press()
        {
            pressed = 1f;
            bool ok = true;
            switch (action)
            {
                case Action.StakeLess: ok = table.Lower(); break;
                case Action.StakeMore: ok = table.Raise(); break;
                case Action.AllIn: ok = table.AllIn(); break;
                case Action.Spin: ok = table.Spin(); break;
                default: table.ToggleGuarantee(); break;
            }
            if (!ok) refused = 1f;
        }

        void Update()
        {
            pressed = Mathf.MoveTowards(pressed, 0f, Time.deltaTime * 5f);
            refused = Mathf.MoveTowards(refused, 0f, Time.deltaTime * 2f);
            cap.localPosition = capRest + Vector3.down * (0.015f * pressed);
            capTint.Set(Color.Lerp(IdleColor(), UpgradeButton.Refused, refused));
        }

        // La couleur au repos dit si le bouton peut servir maintenant
        Color IdleColor() => action switch
        {
            Action.StakeLess => table.Stake > 1 ? UpgradeButton.Affordable : UpgradeButton.TooExpensive,
            Action.StakeMore or Action.AllIn => table.CanRaise ? UpgradeButton.Affordable : UpgradeButton.TooExpensive,
            Action.Spin => table.CanSpin ? Launch : UpgradeButton.TooExpensive,
            _ => table.Guaranteed ? Gold : UpgradeButton.TooExpensive,
        };
    }
}
