using UnityEngine;
using Sae501.Coffres;

namespace SAE
{
    // Une seule bourse pour tout le hub : GameState.Money.
    // - les bananes déposées dans le panier de Maxens rapportent de l'argent ;
    // - le coffre de Nicolas lit cet argent (son Wallet est recopié depuis GameState.Money).
    public class EconomyBridge : MonoBehaviour
    {
        public Panier panier;
        public Wallet wallet;

        void OnEnable() { if (panier) panier.OnDepotCode += OnDeposit; }
        void OnDisable() { if (panier) panier.OnDepotCode -= OnDeposit; }

        void OnDeposit(int gain, int total)
        {
            GameState.Money += gain;
            GameState.NotifyChanged();
        }

        void Update()
        {
            if (wallet && wallet.Money != GameState.Money) wallet.Set(GameState.Money);
        }
    }
}
