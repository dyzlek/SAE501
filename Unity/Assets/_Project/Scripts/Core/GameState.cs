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

    // État partagé du jeu : ce qui est posé, ce qu'on tient, l'inventaire, l'argent.
    // Un singe est toujours à UN endroit : rangé dans l'inventaire (la bibliothèque), tenu en main, ou posé.
    public static class GameState
    {
        public static readonly List<PlacedMonkey> Placed = new List<PlacedMonkey>();

        // Singe tenu en main (null = main vide).
        public static Monkey? Held;

        public static int Money;

        // Vagues vaincues : rendent le coffre plus cher, mais meilleur (raretés, nombre de singes).
        public static int WavesWon;

        public static event Action Changed;
        public static void NotifyChanged() => Changed?.Invoke();

        // Inventaire : nombre de singes rangés dans la bibliothèque, par type et par rareté.
        static readonly int[,] owned = new int[MonkeyData.TypeCount, MonkeyData.LevelCount];

        // Types de singes débloqués : seul le Classique l'est au départ, les autres s'achètent avec des bananes
        // sur les plaques de la bibliothèque (TypeUnlockPlaque). Le coffre ne donne que des types débloqués.
        static readonly bool[] unlocked = new bool[MonkeyData.TypeCount];

        // On commence avec un singe Classique gris, pour pouvoir défendre la première vague.
        static GameState() => ResetAll();

        // Une partie neuve : rien de posé ni en main, pas d'argent, aucune vague gagnée, un seul singe et un seul type.
        // Appelé au lancement (y compris quand Unity garde les statiques d'une partie à l'autre) et par REJOUER.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetAll()
        {
            Placed.Clear();
            Held = null;
            Money = 0;
            WavesWon = 0;
            System.Array.Clear(owned, 0, owned.Length);
            System.Array.Clear(unlocked, 0, unlocked.Length);
            owned[(int)MonkeyType.Classique, (int)Rarity.Gris] = 1;
            unlocked[(int)MonkeyType.Classique] = true;
        }

        public static bool IsUnlocked(MonkeyType t) => unlocked[(int)t];

        public static void Unlock(MonkeyType t)
        {
            unlocked[(int)t] = true;
            NotifyChanged();
        }

        public static int Count(Monkey m) => owned[(int)m.type, (int)m.level];

        public static void AddToInventory(Monkey m)
        {
            owned[(int)m.type, (int)m.level]++;
            NotifyChanged();
        }

        // Sortir un singe de la bibliothèque pour le prendre en main.
        public static bool TakeFromInventory(Monkey m)
        {
            if (Count(m) <= 0) return false;
            owned[(int)m.type, (int)m.level]--;
            Held = m;
            NotifyChanged();
            return true;
        }

        // Le singe tenu retourne dans la bibliothèque (clic droit, ou clic sur une case en le tenant).
        public static void ReturnHeld()
        {
            if (Held == null) return;
            var m = Held.Value;
            Held = null;
            AddToInventory(m);
        }

        // Vider le plateau : tous les singes posés retournent dans la bibliothèque.
        public static void ClearBoard()
        {
            foreach (var p in Placed) owned[(int)p.monkey.type, (int)p.monkey.level]++;
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
