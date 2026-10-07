using UnityEngine;

namespace SAE
{
    public enum BowUpgrade { Perforation, Transpercante, TirTriple, Explosion }

    // Les améliorations de l'arc, toutes les valeurs au même endroit (comme MonkeyData) pour équilibrer facilement.
    // Elles valent pour l'arc VR comme pour l'arc PC : c'est le joueur qui progresse, pas un objet.
    // Elles se cumulent : un tir triple explosif, perforant et transperçant tire 3 flèches qui percent
    // plusieurs couches, traversent plusieurs ballons et explosent.
    public static class BowUpgrades
    {
        // Bêta-test : toutes les améliorations sont gratuites. Les vrais prix sont déjà dans les tableaux ci-dessous.
        public static readonly bool BetaFree = true;   // readonly plutôt que const : pas d'avertissement « code inaccessible »

        // Perforante (les « dégâts ») : couches percées sur CHAQUE ballon touché (index = palier, 0 = sans amélioration)
        static readonly int[] pierceLayers = { 1, 2, 3, 4, 5, 6 };
        static readonly int[] piercePrices = { 100, 200, 400, 700, 1200 };   // prix pour passer au palier suivant

        // Transperçante (tir collatéral) : nombre de ballons qu'une flèche traverse avant de disparaître
        static readonly int[] passBalloons = { 1, 2, 3, 4, 6, 8 };
        static readonly int[] passPrices = { 150, 300, 500, 800, 1300 };

        // Tir triple : un seul palier, les deux flèches de plus partent de chaque côté, à l'horizontale
        public const float TripleSpread = 8f;   // écart avec la flèche du milieu, en degrés
        static readonly int[] triplePrices = { 500 };

        // Explosion : au contact, une onde perce aussi les ballons les plus proches de celui touché
        static readonly int[] explosionBalloons = { 0, 3, 4, 5, 6, 8 };            // ballons touchés en tout, celui visé compris
        static readonly float[] explosionRadius = { 0f, 1f, 1.5f, 2f, 2.5f, 3f };  // rayon de l'onde, en mètres
        static readonly int[] explosionLayers = { 0, 1, 1, 2, 2, 3 };              // couches percées sur chaque ballon de l'onde
        static readonly int[] explosionPrices = { 150, 300, 500, 800, 1300 };

        static readonly int[] levels = new int[System.Enum.GetValues(typeof(BowUpgrade)).Length];

        // Sans rechargement du domaine (Enter Play Mode rapide), les statiques survivent d'une partie à l'autre : on repart de zéro.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetLevels() => System.Array.Clear(levels, 0, levels.Length);

        public static int Level(BowUpgrade u) => levels[(int)u];
        public static int MaxLevel(BowUpgrade u) => Prices(u).Length;
        public static bool IsMax(BowUpgrade u) => Level(u) >= MaxLevel(u);

        // Le prix à payer maintenant (0 en bêta-test), et le prix prévu pour la version finale.
        public static int Price(BowUpgrade u) => BetaFree ? 0 : PlannedPrice(u);
        public static int PlannedPrice(BowUpgrade u) => IsMax(u) ? 0 : Prices(u)[Level(u)];

        public static bool Buy(BowUpgrade u, Vector3 where)
        {
            if (IsMax(u) || !Economy.TrySpend(Price(u), where)) return false;
            levels[(int)u]++;
            return true;
        }

        // --- Ce que donnent les paliers actuels ---
        public static int Pierce => pierceLayers[Level(BowUpgrade.Perforation)];
        public static int PassThrough => passBalloons[Level(BowUpgrade.Transpercante)];
        public static bool TripleShot => Level(BowUpgrade.TirTriple) > 0;
        public static bool Explosive => Level(BowUpgrade.Explosion) > 0;
        public static int ExplosionBalloons => explosionBalloons[Level(BowUpgrade.Explosion)];
        public static float ExplosionRadius => explosionRadius[Level(BowUpgrade.Explosion)];
        public static int ExplosionLayers => explosionLayers[Level(BowUpgrade.Explosion)];

        static int[] Prices(BowUpgrade u) => u switch
        {
            BowUpgrade.Perforation => piercePrices,
            BowUpgrade.Transpercante => passPrices,
            BowUpgrade.TirTriple => triplePrices,
            _ => explosionPrices,
        };
    }
}
