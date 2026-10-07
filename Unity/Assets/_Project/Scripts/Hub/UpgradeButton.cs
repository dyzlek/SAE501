using UnityEngine;

namespace SAE
{
    public enum BananaStat { Frequence, Pourriture, Valeur }

    // Gros bouton rond du comptoir d'amélioration du bananier (Maxens) : on l'enfonce avec la main, on paie, la stat monte d'un niveau.
    // Le bouton est vert si on peut payer, gris sinon, flash rouge si refusé. Il s'enfonce quand on appuie.
    // Au niveau max, le bouton disparaît et l'ardoise affiche MAX.
    public class UpgradeButton : MonoBehaviour, IPressable
    {
        public Bananier bananier;
        public BananaStat stat;
        public Transform cap;          // le dessus du bouton, qui s'enfonce
        public TextMesh label;         // le texte sur l'ardoise, au-dessus du bouton

        // Couleurs communes aux boutons d'amélioration (bananier et récolteur)
        public static readonly Color Affordable = new Color(0.25f, 0.8f, 0.3f);
        public static readonly Color TooExpensive = new Color(0.4f, 0.4f, 0.42f);
        public static readonly Color Refused = new Color(0.95f, 0.25f, 0.25f);

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

        // Le texte d'une colonne de l'ardoise : titre doré, niveau, effet, puis le prix (ou MAX en vert).
        public static string Board(string title, string level, string effect, int price, bool done)
        {
            string cost = done ? "<color=#8CE08C>MAX</color>"
                        : price == 0 ? "<color=#FFD45A>GRATUIT</color>"
                        : $"<color=#FFD45A>{price}</color> bananes";
            return $"<color=#FFD45A>{title}</color>\n{level}\n<color=#C8D8C8>{effect}</color>\n{cost}";
        }

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
            string level = S.niveauMax > 0 ? $"niv {S.niveau} / {S.niveauMax}" : $"niv {S.niveau}";
            label.text = Board(Title, level, Effect, S.PrixAmelioration, S.EstAuMax);
            if (S.EstAuMax) { gameObject.SetActive(false); return; }   // plus rien à améliorer : le bouton s'en va

            pressed = Mathf.MoveTowards(pressed, 0f, Time.deltaTime * 5f);
            refused = Mathf.MoveTowards(refused, 0f, Time.deltaTime * 2f);
            cap.localPosition = capRest + Vector3.down * (0.03f * pressed);

            var color = Economy.CanAfford(S.PrixAmelioration) ? Affordable : TooExpensive;
            capTint.Set(Color.Lerp(color, Refused, refused));
        }
    }
}
