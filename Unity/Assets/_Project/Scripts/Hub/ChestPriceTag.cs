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
            label.text = chest.IsBusy ? "COFFRE" : $"COFFRE  {chest.Price}";
            label.color = Economy.CanAfford(chest.Price) ? new Color(1f, 0.82f, 0.2f) : new Color(0.6f, 0.6f, 0.6f);
        }
    }
}
