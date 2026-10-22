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

        // stakeBack : la part de amount qui n'est que la mise rendue (roulette gagnée). Elle revient dans la bourse,
        // mais ne compte pas pour les trophées : ce ne sont pas des bananes en plus, juste les nôtres qui reviennent.
        public static void Earn(int amount, Vector3? where = null, int stakeBack = 0)
        {
            if (amount <= 0) return;
            GameState.Money += amount;
            GameState.BananasEarned += amount - stakeBack;   // avant l'événement : les trophées se mettent à jour dessus
            if (Coop.IsGuest) Coop.SendEarn(amount, stakeBack);   // en coop, la bourse est chez l'hôte
            MoneyChanged?.Invoke(amount, where);
        }

        public static bool TrySpend(int amount, Vector3? where = null)
        {
            if (amount < 0) return false;
            if (GameState.Money < amount) { Sounds.Play(Sounds.AchatRefuse, where); return false; }   // pas assez d'argent : un son d'alerte
            GameState.Money -= amount;
            if (Coop.IsGuest) Coop.SendSpend(amount);
            MoneyChanged?.Invoke(-amount, where);
            return true;
        }

        // Coop, chez l'hôte : un invité a déjà payé chez lui (il avait assez d'après la bourse qu'il voyait).
        // On ne refuse donc pas : au pire, deux achats en même temps font passer la bourse à 0, jamais en dessous.
        public static void SpendForPartner(int amount)
        {
            if (amount <= 0) return;
            int spent = Mathf.Min(amount, GameState.Money);
            GameState.Money -= spent;
            MoneyChanged?.Invoke(-spent, null);
        }

        // Coop, chez l'invité : la bourse de l'hôte a bougé (un autre joueur a vendu, acheté, ou une vague est gagnée)
        public static void ChangedByHost(int delta) => MoneyChanged?.Invoke(delta, null);
    }
}
