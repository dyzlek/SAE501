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

        // La bananeraie de la scène (pour le coop, CoopOrchard)
        public static BananaOrchard Instance { get; private set; }
        void Awake() => Instance = this;

        // Le niveau d'une amélioration : tous les arbres ont les mêmes niveaux, on lit ceux du premier
        public Bananier.Stat Stat(BananaStat stat) => stat switch
        {
            BananaStat.Frequence => First.frequence,
            BananaStat.Pourriture => First.pourriture,
            _ => First.valeur,
        };

        // Un bouton du comptoir BANANIER (UpgradeButton, ou la demande d'un invité du coop) : on paie,
        // et la stat monte d'un niveau pour TOUS les arbres ; « Arbres » plante le suivant
        public bool Upgrade(BananaStat stat, Vector3 at)
        {
            if (stat == BananaStat.Arbres) return Buy(at);
            var s = Stat(stat);
            if (s.EstAuMax || !Economy.TrySpend(s.PrixAmelioration, at)) return false;
            foreach (var tree in trees)
                switch (stat)
                {
                    case BananaStat.Frequence: tree.AmeliorerFrequence(); break;
                    case BananaStat.Pourriture: tree.AmeliorerPourriture(); break;
                    default: tree.AmeliorerValeur(); break;
                }
            return true;
        }

        // Coop, chez l'invité : les arbres et les niveaux de l'hôte remplacent les nôtres
        public void ApplyFromHost(int count, int frequence, int pourriture, int valeur)
        {
            count = Mathf.Clamp(count, 1, trees.Length);
            while (Count < count)
            {
                var spot = spots[Count++];
                spot.SetActive(true);
                StartCoroutine(Grow(spot.transform));   // il pousse aussi chez nous
            }
            while (Count > count) spots[--Count].SetActive(false);   // on avait plus d'arbres dans notre partie
            foreach (var tree in trees)
            {
                tree.frequence.niveau = frequence;
                tree.pourriture.niveau = pourriture;
                tree.valeur.niveau = valeur;
            }
        }

        // Les arbres plantés
        public IEnumerable<Bananier> Active
        {
            get { for (int i = 0; i < Count; i++) yield return trees[i]; }
        }

        // Sauvegarde : les arbres plantés repoussent tout de suite, au niveau des améliorations gardées
        void Start()
        {
            var save = SaveGame.Pending;
            if (save == null) return;
            while (Count < Mathf.Min(save.trees, trees.Length)) spots[Count++].SetActive(true);
            if (save.treeLevels != null && save.treeLevels.Length == 3)
                foreach (var tree in trees)
                {
                    tree.frequence.niveau = save.treeLevels[0];
                    tree.pourriture.niveau = save.treeLevels[1];
                    tree.valeur.niveau = save.treeLevels[2];
                }
        }

        public int[] Levels() => new[] { First.frequence.niveau, First.pourriture.niveau, First.valeur.niveau };

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
