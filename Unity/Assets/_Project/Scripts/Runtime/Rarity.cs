using UnityEngine;

namespace Sae501.Coffres
{
    // Les 7 raretés, de la plus commune à la plus rare. L'ordre compte : (int)Rarity sert d'index.
    public enum Rarity { Gris, Vert, Bleu, Violet, Jaune, Rouge, LGBT }

    public static class RarityInfo
    {
        public const int Count = 7;

        static readonly Color[] colors =
        {
            new Color(0.62f, 0.64f, 0.68f), // Gris
            new Color(0.25f, 0.75f, 0.30f), // Vert
            new Color(0.20f, 0.45f, 0.95f), // Bleu
            new Color(0.62f, 0.30f, 0.90f), // Violet
            new Color(0.97f, 0.85f, 0.15f), // Jaune
            new Color(0.90f, 0.18f, 0.18f), // Rouge
            new Color(1f, 0.4f, 0.85f),     // LGBT (couleur du texte, le rectangle est un arc-en-ciel)
        };

        public static Color ColorOf(Rarity r) => colors[(int)r];
    }
}
