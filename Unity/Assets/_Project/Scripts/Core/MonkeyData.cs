using UnityEngine;

namespace SAE
{
    public enum MonkeyType { Classique, Boomerang, Canon, Sniper, Punaise, Glace, Colle }

    // Rareté = niveau du singe. On monte d'un cran en fusionnant 2 singes identiques.
    public enum Rarity { Gris, Vert, Bleu, Violet, Jaune, Rouge, ArcEnCiel, Blanc }

    public readonly struct Monkey
    {
        public readonly MonkeyType type;
        public readonly Rarity level;

        public Monkey(MonkeyType type, Rarity level) { this.type = type; this.level = level; }

        public bool CanFuseWith(Monkey other) => other.type == type && other.level == level && level < Rarity.Blanc;
        public Monkey Upgraded() => new Monkey(type, level + 1);
        public override string ToString() => $"{type} {MonkeyData.RarityName(level)} (niv {(int)level + 1})";
    }

    // Toutes les valeurs de réglage au même endroit, pour équilibrer facilement.
    public static class MonkeyData
    {
        public const int TypeCount = 7;
        public const int LevelCount = 8;

        static readonly string[] shortNames = { "CL", "BO", "CA", "SN", "PU", "GL", "CO" };
        static readonly string[] rarityNames = { "Gris", "Vert", "Bleu", "Violet", "Jaune", "Rouge", "Arc-en-ciel", "Blanc" };
        static readonly Color[] rarityColors =
        {
            new Color(0.55f, 0.55f, 0.55f), new Color(0.20f, 0.80f, 0.30f), new Color(0.20f, 0.45f, 1.00f),
            new Color(0.60f, 0.25f, 0.90f), new Color(1.00f, 0.85f, 0.10f), new Color(0.90f, 0.15f, 0.15f),
            Color.magenta /* animé, voir ColorTint */, Color.white,
        };

        // Prix pour débloquer chaque type (en bananes), dans l'ordre de MonkeyType. Le Classique est offert.
        static readonly int[] unlockPrices = { 0, 40, 120, 200, 60, 80, 150 };
        public static int UnlockPrice(MonkeyType t) => unlockPrices[(int)t];

        public static string ShortName(MonkeyType t) => shortNames[(int)t];
        public static string RarityName(Rarity r) => rarityNames[(int)r];
        public static Color RarityColor(Rarity r) => rarityColors[(int)r];
        public static bool IsRainbow(Rarity r) => r == Rarity.ArcEnCiel;

        // Ce que fait chaque type, en une phrase (fiche du singe).
        public static string Effect(MonkeyType t) => t switch
        {
            MonkeyType.Classique => "Tire sur le ballon le plus avancé",
            MonkeyType.Boomerang => "Touche 2 ballons à chaque lancer",
            MonkeyType.Canon => "Explosion : touche tout autour de la cible",
            MonkeyType.Sniper => "Tire sur toute la carte, gros dégâts",
            MonkeyType.Punaise => "Touche 4 ballons proches",
            MonkeyType.Glace => "Ralentit tous les ballons à portée",
            MonkeyType.Colle => "Colle un ballon : très lent pendant 3 s",
            _ => "",
        };

        // Tirs par seconde (plus parlant que le délai entre deux tirs).
        public static float FireRate(Monkey m) => 1f / FireDelay(m);

        // --- Statistiques de combat (prototype) ---
        public static float Range(Monkey m) => m.type switch
        {
            MonkeyType.Sniper => 100f,
            MonkeyType.Punaise => 4f,
            MonkeyType.Canon => 5f,
            _ => 6f,
        } + (int)m.level * 0.3f;

        public static float FireDelay(Monkey m) => m.type switch
        {
            MonkeyType.Sniper => 2f,
            MonkeyType.Canon => 1.5f,
            MonkeyType.Glace => 1.5f,
            MonkeyType.Punaise => 0.7f,
            _ => 1f,
        } / (1f + (int)m.level * 0.2f);

        // Perforation : nombre de couches percées sur chaque ballon touché (il n'y a plus de « dégâts »).
        // +1 couche toutes les 2 raretés (Gris 1, Bleu 2, Jaune 3, Arc-en-ciel 4) ; le sniper perce 3 fois plus.
        public static int Pierce(Monkey m) => m.type switch
        {
            MonkeyType.Sniper => 3,
            MonkeyType.Glace => 0,
            MonkeyType.Colle => 0,
            _ => 1,
        } * (1 + (int)m.level / 2);

        // Nombre de ballons touchés par tir.
        public static int Targets(Monkey m) => m.type switch
        {
            MonkeyType.Boomerang => 2,
            MonkeyType.Punaise => 4,
            MonkeyType.Glace => 99,
            _ => 1,
        };
    }
}
