using UnityEngine;

namespace SAE
{
    public enum BananaStat { Frequence, Pourriture, Valeur }

    // Gros bouton rond du panneau d'amélioration du bananier (Maxens) : on appuie, on paie, la stat monte d'un niveau.
    // Le bouton est vert si on peut payer, gris sinon, doré au niveau max. Il s'enfonce quand on appuie.
    public class UpgradeButton : MonoBehaviour, IClickable
    {
        public Bananier bananier;
        public BananaStat stat;
        public Transform cap;          // le dessus du bouton, qui s'enfonce
        public TextMesh label;

        static readonly Color Affordable = new Color(0.25f, 0.85f, 0.35f);
        static readonly Color TooExpensive = new Color(0.45f, 0.45f, 0.48f);
        static readonly Color Maxed = new Color(1f, 0.8f, 0.2f);
        static readonly Color Refused = new Color(0.95f, 0.25f, 0.25f);

        Vector3 capRest;
        float pressed;      // 1 = enfoncé, revient à 0
        float refused;      // 1 = flash rouge « pas assez d'argent »
        ColorTint capTint;

        Bananier.Stat S => stat switch
        {
            BananaStat.Frequence => bananier.frequence,
            BananaStat.Pourriture => bananier.pourriture,
            _ => bananier.valeur,
        };

        string Title => stat switch
        {
            BananaStat.Frequence => "PRODUCTION",
            BananaStat.Pourriture => "FRAÎCHEUR",
            _ => "VALEUR",
        };

        string Effect => stat switch
        {
            BananaStat.Frequence => $"1 banane / {S.Valeur:0.#} s",
            BananaStat.Pourriture => $"pourrit en {S.Valeur:0} s",
            _ => $"{S.Valeur:0} par banane",
        };

        public string GetHint(Vector3 point) =>
            S.EstAuMax ? $"{Title} : niveau max" : $"Améliorer {Title.ToLower()} : {S.PrixAmelioration}";

        public void OnClick(PlayerController player, Vector3 point) => Press();

        public void Press()
        {
            pressed = 1f;
            if (S.EstAuMax) return;
            int price = S.PrixAmelioration;
            if (!Economy.TrySpend(price, transform.position)) { refused = 1f; return; }
            switch (stat)
            {
                case BananaStat.Frequence: bananier.AmeliorerFrequence(); break;
                case BananaStat.Pourriture: bananier.AmeliorerPourriture(); break;
                default: bananier.AmeliorerValeur(); break;
            }
        }

        void Start()
        {
            capRest = cap.localPosition;
            capTint = cap.GetComponent<ColorTint>();
        }

        void Update()
        {
            if (!bananier) return;
            pressed = Mathf.MoveTowards(pressed, 0f, Time.deltaTime * 5f);
            refused = Mathf.MoveTowards(refused, 0f, Time.deltaTime * 2f);
            cap.localPosition = capRest + Vector3.down * (0.03f * pressed);

            var color = S.EstAuMax ? Maxed : Economy.CanAfford(S.PrixAmelioration) ? Affordable : TooExpensive;
            capTint.Set(Color.Lerp(color, Refused, refused));

            label.text = $"{Title}\nniv {S.niveau}\n{Effect}\n{(S.EstAuMax ? "MAX" : S.PrixAmelioration.ToString())}";
        }
    }
}
