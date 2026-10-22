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
    // En coop (Coop), c'est l'hôte qui fait foi : chez un invité, chaque changement est aussi demandé à l'hôte,
    // puis l'état de l'hôte remplace le nôtre (ApplyFromHost). Le singe tenu (Held) reste à chaque joueur.
    public static class GameState
    {
        public static readonly List<PlacedMonkey> Placed = new List<PlacedMonkey>();

        // Singe tenu en main (null = main vide).
        public static Monkey? Held;

        public static int Money;

        // Vagues vaincues : rendent le coffre plus cher, mais meilleur (raretés, nombre de singes).
        public static int WavesWon;

        // Bananes gagnées depuis le début (panier, vagues, roulette) : ne baisse jamais, même quand on dépense.
        // C'est ce que comptent les trophées des bananes (Trophy). Rempli par Economy.Earn.
        public static int BananasEarned;

        public static event Action Changed;
        public static void NotifyChanged() => Changed?.Invoke();

        // Inventaire : nombre de singes rangés dans la bibliothèque, par type et par rareté.
        static readonly int[,] owned = new int[MonkeyData.TypeCount, MonkeyData.LevelCount];

        // Types de singes débloqués : seul le Classique l'est au départ, les autres s'achètent avec des bananes
        // sur les plaques de la bibliothèque (TypeUnlockPlaque). Le coffre ne donne que des types débloqués.
        static readonly bool[] unlocked = new bool[MonkeyData.TypeCount];

        // On commence sans singe : la vague 1 se gagne à l'arc, et le tutoriel offre ensuite le premier Classique gris
        // (Tutorial.GiveFirstMonkey, aussi quand on passe le tutoriel).
        static GameState() => unlocked[(int)MonkeyType.Classique] = true;

        // Bac à sable (pour travailler les animations) : tous les types débloqués et chaque singe en stock illimité.
        // Il ne s'active que dans l'éditeur, par le menu SAE > Bac à sable (case cochée), jamais dans un build.
        public static bool Sandbox { get; private set; }
        public const string SandboxPref = "SAE.BacASable";
        const int SandboxStock = 99;

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ReadSandbox() => Sandbox = UnityEditor.EditorPrefs.GetBool(SandboxPref, false);
#endif

        public static bool IsUnlocked(MonkeyType t) => Sandbox || unlocked[(int)t];

        public static void Unlock(MonkeyType t)
        {
            if (Coop.IsGuest) Coop.SendInventory(InventoryOp.Unlock, new Monkey(t, default));
            unlocked[(int)t] = true;
            NotifyChanged();
        }

        public static int Count(Monkey m) => Sandbox ? SandboxStock : owned[(int)m.type, (int)m.level];

        public static void AddToInventory(Monkey m)
        {
            if (Coop.IsGuest) Coop.SendInventory(InventoryOp.Add, m);
            owned[(int)m.type, (int)m.level]++;
            NotifyChanged();
        }

        // Sortir un singe de la bibliothèque pour le prendre en main.
        public static bool TakeFromInventory(Monkey m)
        {
            if (Count(m) <= 0) return false;
            if (Coop.IsGuest) Coop.SendInventory(InventoryOp.Take, m);
            if (!Sandbox) owned[(int)m.type, (int)m.level]--;   // en bac à sable, le stock ne baisse jamais
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
            if (Coop.IsGuest) Coop.SendInventory(InventoryOp.ClearBoard, default);
            foreach (var p in Placed) owned[(int)p.monkey.type, (int)p.monkey.level]++;
            Placed.Clear();
            NotifyChanged();
        }

        // --- Coop ---

        // Hôte : un invité a sorti ce singe de la bibliothèque. Il le tient dans SA main : notre Held ne change pas.
        public static void TakeForPartner(Monkey m)
        {
            if (Count(m) <= 0) return;
            if (!Sandbox) owned[(int)m.type, (int)m.level]--;
            NotifyChanged();
        }

        // Invité : l'état reçu de l'hôte remplace le nôtre. Les singes posés ne sont remplacés que s'ils ont changé :
        // sinon TowerManager détruirait et recréerait tous les singes de la carte 5 fois par seconde.
        public static void ApplyFromHost(int money, int wavesWon, int bananasEarned, int[] ownedFlat, bool[] unlockedTypes, List<PlacedMonkey> placed)
        {
            bool changed = money != Money || wavesWon != WavesWon;
            if (money != Money) Economy.ChangedByHost(money - Money);   // la bourse affiche « +5 » quand un autre joueur vend
            Money = money;
            WavesWon = wavesWon;
            BananasEarned = bananasEarned;
            if (ownedFlat.Length == owned.Length)
                for (int i = 0; i < ownedFlat.Length; i++)
                {
                    ref int n = ref owned[i / MonkeyData.LevelCount, i % MonkeyData.LevelCount];
                    if (n != ownedFlat[i]) { n = ownedFlat[i]; changed = true; }
                }
            if (unlockedTypes.Length == unlocked.Length)
                for (int i = 0; i < unlocked.Length; i++)
                    if (unlocked[i] != unlockedTypes[i]) { unlocked[i] = unlockedTypes[i]; changed = true; }
            if (!SamePlaced(placed))
            {
                Placed.Clear();
                Placed.AddRange(placed);
                changed = true;
            }
            if (changed) NotifyChanged();
        }

        static bool SamePlaced(List<PlacedMonkey> other)
        {
            if (other.Count != Placed.Count) return false;
            for (int i = 0; i < other.Count; i++)
                if (!other[i].monkey.Equals(Placed[i].monkey) || (other[i].pos - Placed[i].pos).sqrMagnitude > 0.0001f) return false;
            return true;
        }

        // --- Sauvegarde (SaveGame) ---
        public static int Index(Monkey m) => (int)m.type * MonkeyData.LevelCount + (int)m.level;

        public static int[] OwnedFlat()
        {
            var flat = new int[owned.Length];
            for (int t = 0; t < MonkeyData.TypeCount; t++)
                for (int l = 0; l < MonkeyData.LevelCount; l++) flat[t * MonkeyData.LevelCount + l] = owned[t, l];
            return flat;
        }

        public static bool[] UnlockedCopy() => (bool[])unlocked.Clone();

        // Remet l'état lu dans la sauvegarde (les tableaux trop courts d'une vieille version sont ignorés)
        public static void Restore(int money, int wavesWon, int bananasEarned, int[] ownedFlat, bool[] unlockedTypes)
        {
            ResetAll();
            Money = money;
            WavesWon = wavesWon;
            BananasEarned = bananasEarned;
            if (ownedFlat != null && ownedFlat.Length == owned.Length)
                for (int i = 0; i < ownedFlat.Length; i++) owned[i / MonkeyData.LevelCount, i % MonkeyData.LevelCount] = ownedFlat[i];
            if (unlockedTypes != null && unlockedTypes.Length == unlocked.Length) unlockedTypes.CopyTo(unlocked, 0);
            unlocked[(int)MonkeyType.Classique] = true;
        }

        // Nouvelle partie : tout repart de zéro, comme au premier lancement
        public static void ResetAll()
        {
            Placed.Clear();
            Held = null;
            Money = WavesWon = BananasEarned = 0;
            Array.Clear(owned, 0, owned.Length);
            Array.Clear(unlocked, 0, unlocked.Length);
            unlocked[(int)MonkeyType.Classique] = true;
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
