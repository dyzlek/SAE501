using UnityEngine;

namespace SAE
{
    // Un jeton du pupitre de la roulette : ajouter à la mise (+1, +10, +100), tout miser, ou tout retirer (la mise revient à 0).
    // La roue, elle, se lance à la main (WheelGrab).
    // Même fonctionnement que les boutons d'amélioration (BowUpgradeButton) : on l'enfonce avec la main ou on le vise
    // avec le rayon ; il est gris quand il ne peut rien faire, flash rouge si l'appui est refusé.
    public class RouletteButton : MonoBehaviour, IPressable
    {
        public enum Action { AddOne, AddTen, AddHundred, AllIn, Clear }

        public RouletteTable table;
        public Action action;
        public Transform cap;          // le dessus du bouton, qui s'enfonce

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
                case Action.AddOne: ok = table.Add(1); break;
                case Action.AddTen: ok = table.Add(10); break;
                case Action.AddHundred: ok = table.Add(100); break;
                case Action.AllIn: ok = table.AllIn(); break;
                default: ok = table.Clear(); break;
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
            Action.Clear => table.CanClear ? UpgradeButton.Affordable : UpgradeButton.TooExpensive,
            _ => table.CanRaise ? UpgradeButton.Affordable : UpgradeButton.TooExpensive,
        };
    }
}
