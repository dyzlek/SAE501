using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    public enum HarvesterStat { Vitesse, Cadence, Rendement }

    // L'équipe des singes récolteurs, qu'on achète au comptoir « RÉCOLTEUR » : combien on en a, et leurs améliorations
    // (communes à toute l'équipe). Les singes sont déjà dans la scène, cachés : en acheter un le fait apparaître.
    // L'équipe note aussi la banane que chaque singe est parti chercher, pour que deux singes ne courent pas après la même.
    public class HarvesterCrew : MonoBehaviour
    {
        public const bool Free = true;          // à passer à false quand on voudra faire payer
        public const int FirstPrice = 200;      // prix du 1er singe (en bananes) si Free = false ; le 2e coûte 2 fois plus, etc.
        public const int MaxLevel = 5;

        public HarvesterMonkey[] monkeys;       // tous les singes possibles, cachés au départ : leur nombre est le maximum

        public int Count { get; private set; }  // singes achetés
        public bool Full => Count >= monkeys.Length;

        readonly int[] levels = { 1, 1, 1 };
        readonly HashSet<Banane> claimed = new HashSet<Banane>();

        public int Level(HarvesterStat s) => levels[(int)s];
        public bool IsMax(HarvesterStat s) => Level(s) >= MaxLevel;
        public int Price(HarvesterStat s) => Free ? 0 : Mathf.RoundToInt(100 * Mathf.Pow(1.6f, Level(s) - 1));
        public int PriceToBuy => Free ? 0 : FirstPrice * (Count + 1);

        // Les trois améliorations, niveau 1 à 5
        public float Speed => 0.25f + 0.2f * (Level(HarvesterStat.Vitesse) - 1);       // m/s : 0,25 (il flâne) → 1,05
        public float Pause => 3f - 0.6f * (Level(HarvesterStat.Cadence) - 1);           // s entre deux trajets : 3 → 0,6
        public float Share => 0.6f + 0.1f * (Level(HarvesterStat.Rendement) - 1);       // part payée : 60 % → 100 %

        public bool Buy()
        {
            if (Full || !Economy.TrySpend(PriceToBuy, transform.position)) return false;
            monkeys[Count].StartWork();
            Count++;
            return true;
        }

        public bool Upgrade(HarvesterStat s)
        {
            if (Count == 0 || IsMax(s) || !Economy.TrySpend(Price(s), transform.position)) return false;
            levels[(int)s]++;
            return true;
        }

        // Un singe réserve la banane qu'il va chercher ; les autres la laissent tranquille
        public bool IsClaimed(Banane banana) => claimed.Contains(banana);
        public void Claim(Banane banana) => claimed.Add(banana);
        public void Unclaim(Banane banana) => claimed.Remove(banana);
    }
}
