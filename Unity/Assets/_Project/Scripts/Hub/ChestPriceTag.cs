using UnityEngine;
using Sae501.Coffres;

namespace SAE
{
    // Le prix du coffre, écrit sur la petite pancarte posée devant lui : doré si on peut payer, gris sinon.
    public class ChestPriceTag : MonoBehaviour
    {
        public ChestController chest;
        public TextMesh label;

        void Update()
        {
            if (!chest || !label) return;
            var lid = chest.GetComponent<ChestLid>();
            bool opening = chest.IsBusy || (lid && lid.IsOpen);   // pendant l'ouverture, pas de prix : la pancarte dit juste COFFRE
            label.text = opening ? "COFFRE" : $"COFFRE  {chest.Price}";
            label.color = Economy.CanAfford(chest.Price) ? new Color(1f, 0.82f, 0.2f) : new Color(0.6f, 0.6f, 0.6f);
        }
    }
}
