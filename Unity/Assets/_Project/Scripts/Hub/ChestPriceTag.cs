using UnityEngine;
using Sae501.Coffres;

namespace SAE
{
    // Le prix du coffre, écrit sur la petite pancarte posée devant lui : doré si on peut payer, gris sinon.
    // Si on touche le coffre sans assez d'argent, elle dit en rouge combien il manque, pendant 2 s
    // (elle remplace le texte qui flottait au-dessus du coffre).
    public class ChestPriceTag : MonoBehaviour
    {
        public ChestController chest;
        public TextMesh label;
        public float refusedDuration = 2f;   // secondes d'affichage de « IL MANQUE … »

        float refusedUntil;
        int missing;

        void OnEnable() { if (chest) chest.Refused += ShowMissing; }
        void OnDisable() { if (chest) chest.Refused -= ShowMissing; }

        void ShowMissing(int amount)
        {
            missing = amount;
            refusedUntil = Time.time + refusedDuration;
        }

        void Update()
        {
            if (!chest || !label) return;
            if (Time.time < refusedUntil)
            {
                label.text = $"IL MANQUE {missing}";
                label.color = new Color(1f, 0.35f, 0.3f);
                return;
            }
            var lid = chest.GetComponent<ChestLid>();
            bool opening = chest.IsBusy || (lid && lid.IsOpen);   // pendant l'ouverture, pas de prix : la pancarte dit juste COFFRE
            label.text = opening ? "COFFRE" : $"COFFRE  {chest.Price}";
            label.color = Economy.CanAfford(chest.Price) ? new Color(1f, 0.82f, 0.2f) : new Color(0.6f, 0.6f, 0.6f);
        }
    }
}
