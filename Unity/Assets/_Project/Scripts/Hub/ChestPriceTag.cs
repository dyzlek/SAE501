using UnityEngine;
using Sae501.Coffres;

namespace SAE
{
    // Étiquette de prix au-dessus du coffre, toujours visible : dorée si on peut payer, grise sinon.
    public class ChestPriceTag : MonoBehaviour
    {
        public ChestController chest;
        public TextMesh label;

        void Update()
        {
            if (!chest || !label) return;
            var lid = chest.GetComponent<ChestLid>();
            label.text = chest.IsBusy || (lid && lid.IsOpen) ? "" : $"COFFRE\n{chest.Price}";
            label.color = Economy.CanAfford(chest.Price) ? new Color(1f, 0.82f, 0.2f) : new Color(0.6f, 0.6f, 0.6f);
        }
    }
}
