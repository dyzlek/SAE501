using UnityEngine;

namespace Sae501.Coffres
{
    // Portefeuille minimal : une seule valeur "money". Pas de vraie économie pour l'instant.
    public class Wallet : MonoBehaviour
    {
        public int startMoney = 5;

        public int Money { get; private set; }

        void Awake() => Money = startMoney;

        public void Set(int value) => Money = Mathf.Max(0, value);
    }
}
