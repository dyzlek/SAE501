using System;
using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Un singe posé sur la carte. pos = position (x, z) en mètres dans le repère de la carte.
    public class PlacedMonkey
    {
        public readonly Monkey monkey;
        public readonly Vector2 pos;

        public PlacedMonkey(Monkey monkey, Vector2 pos) { this.monkey = monkey; this.pos = pos; }
    }

    // État partagé du jeu : ce qui est posé, ce qu'on tient, l'argent.
    public static class GameState
    {
        public static readonly List<PlacedMonkey> Placed = new List<PlacedMonkey>();

        // Singe tenu en main (null = main vide).
        public static Monkey? Held;

        // Rareté choisie dans le sélecteur de la bibliothèque.
        public static Rarity SelectedRarity = Rarity.Gris;

        public static int Money;

        public static event Action Changed;
        public static void NotifyChanged() => Changed?.Invoke();

        public static void ClearBoard()
        {
            Placed.Clear();
            NotifyChanged();
        }

        // Singe posé le plus proche de pos, à moins de maxDistance.
        public static PlacedMonkey Nearest(Vector2 pos, float maxDistance)
        {
            PlacedMonkey best = null;
            float bestSqr = maxDistance * maxDistance;
            foreach (var p in Placed)
            {
                float d = (p.pos - pos).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = p; }
            }
            return best;
        }
    }
}
