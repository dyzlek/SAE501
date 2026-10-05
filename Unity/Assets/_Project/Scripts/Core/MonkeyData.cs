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

        public static string ShortName(MonkeyType t) => shortNames[(int)t];
        public static string RarityName(Rarity r) => rarityNames[(int)r];
        public static Color RarityColor(Rarity r) => rarityColors[(int)r];
        public static bool IsRainbow(Rarity r) => r == Rarity.ArcEnCiel;

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

        public static float Damage(Monkey m) => m.type switch
        {
            MonkeyType.Sniper => 3f,
            MonkeyType.Glace => 0f,
            MonkeyType.Colle => 0f,
            _ => 1f,
        } * (1f + (int)m.level * 0.5f);

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
