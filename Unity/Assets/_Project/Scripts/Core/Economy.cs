using System;
using UnityEngine;

namespace SAE
{
    // L'argent du jeu : une seule bourse (GameState.Money), on passe toujours par ici pour gagner ou dépenser.
    // Ce qui rapporte : les bananes déposées dans le panier, et chaque vague finie (pas les ballons éclatés).
    // Ce qui coûte : ouvrir un coffre, améliorer le bananier.
    // Pas d'affichage à l'écran (en VR ça donne le vertige) : la caisse du hub et les « +5 » flottants s'abonnent à MoneyChanged.
    public static class Economy
    {
        // (variation, endroit où ça s'est passé — null = pas d'endroit précis, ex. fin de vague)
        public static event Action<int, Vector3?> MoneyChanged;

        public static int Money => GameState.Money;

        public static bool CanAfford(int amount) => GameState.Money >= amount;

        public static void Earn(int amount, Vector3? where = null)
        {
            if (amount <= 0) return;
            GameState.Money += amount;
            MoneyChanged?.Invoke(amount, where);
        }

        public static bool TrySpend(int amount, Vector3? where = null)
        {
            if (amount < 0 || GameState.Money < amount) return false;
            GameState.Money -= amount;
            MoneyChanged?.Invoke(-amount, where);
            return true;
        }
    }
}
