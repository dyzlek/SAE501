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
        public const bool Free = false;         // payant depuis le 9 oct. (true : tout gratuit, pour tester)
        public const int FirstPrice = 150;      // prix du 1er singe (en bananes) ; le 2e coûte 2 fois plus, etc.
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
        // Au maximum, ils ramènent environ 75 % de ce que donnent les 3 bananiers au maximum (~1,15 banane/s) : 4 singes,
        // 2 bananes par trajet, ~10 m d'aller-retour. Pas tout : le joueur doit encore ramasser lui-même.
        public float Speed => 0.25f + 0.35f * (Level(HarvesterStat.Vitesse) - 1);      // m/s : 0,25 (il flâne) → 1,65 (il trottine)
        public float Pause => 3f - 0.6f * (Level(HarvesterStat.Cadence) - 1);           // s entre deux trajets : 3 → 0,6
        public float Share => 0.6f + 0.1f * (Level(HarvesterStat.Rendement) - 1);       // part payée : 60 % → 100 %

        // Sauvegarde : les singes achetés se remettent au travail, avec leurs améliorations
        void Start()
        {
            var save = SaveGame.Pending;
            if (save == null) return;
            if (save.harvesterLevels != null && save.harvesterLevels.Length == levels.Length) save.harvesterLevels.CopyTo(levels, 0);
            while (Count < Mathf.Min(save.harvesters, monkeys.Length)) monkeys[Count++].StartWork();
        }

        public int[] Levels() => (int[])levels.Clone();

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

        // --- Pour que les singes ne se gênent pas (voir HarvesterMonkey) ---

        // Les singes (achetés) qui travaillent à cet arbre, sans compter 'except' : 2 au plus par arbre
        public int WorkingAt(Bananier tree, HarvesterMonkey except)
        {
            int n = 0;
            foreach (var m in monkeys) if (m && m != except && m.gameObject.activeSelf && m.Busy && m.Tree == tree) n++;
            return n;
        }

        // Les singes à cet arbre, qu'ils travaillent ou qu'ils attendent dans sa zone
        public int At(Bananier tree, HarvesterMonkey except)
        {
            int n = 0;
            foreach (var m in monkeys) if (m && m != except && m.gameObject.activeSelf && m.Tree == tree) n++;
            return n;
        }

        // Un autre singe à cet arbre (pour se tenir à côté de lui sans lui marcher dessus), ou null
        public HarvesterMonkey OtherAt(Bananier tree, HarvesterMonkey except)
        {
            foreach (var m in monkeys) if (m && m != except && m.gameObject.activeSelf && m.Tree == tree) return m;
            return null;
        }

        // Les places autour du panier : 4 en arc, côté terrasse (angles depuis la direction du milieu de la terrasse)
        public static readonly float[] BasketAngles = { -20f, 20f, -55f, 55f };
        readonly bool[] basketTaken = new bool[BasketAngles.Length];

        // Une place libre autour du panier (la plus au milieu d'abord) ; toutes prises : -1 (il se met à la place du milieu)
        public int TakeBasketSlot()
        {
            for (int i = 0; i < basketTaken.Length; i++)
                if (!basketTaken[i]) { basketTaken[i] = true; return i; }
            return -1;
        }

        public void FreeBasketSlot(int slot)
        {
            if (slot >= 0 && slot < basketTaken.Length) basketTaken[slot] = false;
        }
    }
}
