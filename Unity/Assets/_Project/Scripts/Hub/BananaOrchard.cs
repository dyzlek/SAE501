using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Les bananiers de la bananeraie : 3 places le long de la barrière, un seul arbre au début.
    // On achète le 2e, puis le 3e, au comptoir BANANIER (colonne « +1 ARBRE », voir UpgradeButton) : ils coûtent cher.
    // Les améliorations (production, fraîcheur, valeur) valent pour tous les arbres, même ceux pas encore achetés :
    // un nouvel arbre arrive au niveau des autres. Les singes récolteurs ramassent les bananes de tous les arbres plantés.
    public class BananaOrchard : MonoBehaviour
    {
        public Bananier[] trees;               // dans l'ordre d'achat ; le premier est là dès le début
        public GameObject[] spots;             // pour chaque arbre : l'arbre et son étal (cachés tant qu'il n'est pas acheté)
        public int[] prices = { 500, 1500 };   // en bananes : le 2e arbre, puis le 3e

        public int Count { get; private set; } = 1;
        public bool Full => Count >= trees.Length;
        public int Price => Full ? 0 : prices[Mathf.Min(Count - 1, prices.Length - 1)];
        public Bananier First => trees[0];

        // Les arbres plantés
        public IEnumerable<Bananier> Active
        {
            get { for (int i = 0; i < Count; i++) yield return trees[i]; }
        }

        // Planter l'arbre suivant, si on peut le payer
        public bool Buy(Vector3 at)
        {
            if (Full || !Economy.TrySpend(Price, at)) return false;
            var spot = spots[Count];
            Count++;
            spot.SetActive(true);
            StartCoroutine(Grow(spot.transform));
            return true;
        }

        // Il pousse : de presque rien à sa taille, en dépassant un peu à la fin
        IEnumerator Grow(Transform spot)
        {
            var size = spot.localScale;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.8f)
            {
                float bounce = 1f + 0.15f * Mathf.Sin(t * Mathf.PI) * t;
                spot.localScale = size * Mathf.Max(0.01f, Mathf.SmoothStep(0f, 1f, t) * bounce);
                yield return null;
            }
            spot.localScale = size;
        }
    }
}
