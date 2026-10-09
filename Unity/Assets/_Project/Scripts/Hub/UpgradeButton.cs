using UnityEngine;

namespace SAE
{
    public enum BananaStat { Frequence, Pourriture, Valeur, Arbres }

    // Gros bouton rond du comptoir d'amélioration du bananier (Maxens) : on l'enfonce avec la main, on paie, la stat monte d'un niveau
    // pour TOUS les bananiers (BananaOrchard), plantés ou pas encore. « +1 ARBRE » (Arbres) plante le bananier suivant.
    // Le bouton est vert si on peut payer, gris sinon, flash rouge si refusé. Il s'enfonce quand on appuie.
    // Au niveau max, le bouton disparaît et l'ardoise affiche MAX.
    public class UpgradeButton : MonoBehaviour, IPressable
    {
        public BananaOrchard orchard;
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

        // Tous les arbres ont les mêmes niveaux : on lit ceux du premier
        Bananier.Stat S => stat switch
        {
            BananaStat.Frequence => orchard.First.frequence,
            BananaStat.Pourriture => orchard.First.pourriture,
            _ => orchard.First.valeur,
        };

        bool Trees => stat == BananaStat.Arbres;
        int Price => Trees ? orchard.Price : S.PrixAmelioration;
        bool Done => Trees ? orchard.Full : S.EstAuMax;

        string Title => stat switch
        {
            BananaStat.Frequence => "PRODUCTION",
            BananaStat.Pourriture => "FRAÎCHEUR",
            BananaStat.Valeur => "VALEUR",
            _ => "BANANIERS",
        };

        string Level => Trees ? $"{orchard.Count} / {orchard.trees.Length}"
                      : S.niveauMax > 0 ? $"niv {S.niveau} / {S.niveauMax}" : $"niv {S.niveau}";

        string Effect => stat switch
        {
            BananaStat.Frequence => $"1 banane / {S.Valeur:0.#} s",
            BananaStat.Pourriture => $"pourrit en {S.Valeur:0} s",
            BananaStat.Valeur => $"{S.Valeur:0} par banane",
            _ => orchard.Full ? "tous plantés" : "1 arbre de plus",
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
            if (Done) return;
            if (Trees)
            {
                if (!orchard.Buy(transform.position)) refused = 1f;
                return;
            }
            if (!Economy.TrySpend(Price, transform.position)) { refused = 1f; return; }
            foreach (var tree in orchard.trees)
                switch (stat)
                {
                    case BananaStat.Frequence: tree.AmeliorerFrequence(); break;
                    case BananaStat.Pourriture: tree.AmeliorerPourriture(); break;
                    default: tree.AmeliorerValeur(); break;
                }
        }

        void Start()
        {
            capRest = cap.localPosition;
            capTint = cap.GetComponent<ColorTint>();
        }

        void Update()
        {
            if (!orchard) return;
            label.text = Board(Title, Level, Effect, Price, Done);
            if (Done) { gameObject.SetActive(false); return; }   // plus rien à améliorer : le bouton s'en va

            pressed = Mathf.MoveTowards(pressed, 0f, Time.deltaTime * 5f);
            refused = Mathf.MoveTowards(refused, 0f, Time.deltaTime * 2f);
            cap.localPosition = capRest + Vector3.down * (0.015f * pressed);

            var color = Economy.CanAfford(Price) ? Affordable : TooExpensive;
            capTint.Set(Color.Lerp(color, Refused, refused));
        }
    }
}
