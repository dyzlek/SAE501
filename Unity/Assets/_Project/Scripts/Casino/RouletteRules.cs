namespace SAE
{
    public enum RouletteColor { Rouge, Noir, Vert }

    // Les règles de la roulette européenne (un seul zéro), toutes au même endroit : l'ordre des cases sur la roue,
    // leurs couleurs, et ce que paie chaque couleur, comme au casino.
    // On ne parie que sur une couleur : rouge ou noir (18 cases chacune) paient 1 contre 1, le vert (le zéro, 1 case)
    // paie 35 contre 1 comme un numéro plein.
    public static class RouletteRules
    {
        public const int Numbers = 37;   // de 0 à 36

        // L'ordre des cases sur une vraie roue européenne, dans le sens des aiguilles d'une montre
        public static readonly int[] WheelOrder =
        {
            0, 32, 15, 19, 4, 21, 2, 25, 17, 34, 6, 27, 13, 36, 11, 30, 8, 23, 10,
            5, 24, 16, 33, 1, 20, 14, 31, 9, 22, 18, 29, 7, 28, 12, 35, 3, 26,
        };

        static readonly int[] reds = { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 };

        public static RouletteColor ColorOf(int n) =>
            n == 0 ? RouletteColor.Vert : System.Array.IndexOf(reds, n) >= 0 ? RouletteColor.Rouge : RouletteColor.Noir;

        // Le gain pour 1 banane misée, mise non comprise (« 35 contre 1 ») : on récupère aussi sa mise
        public static int Payout(RouletteColor c) => c == RouletteColor.Vert ? 35 : 1;

        // La teinte de chaque couleur sur l'ardoise (texte riche), et son nom écrit dans cette teinte
        public static string Hex(RouletteColor c) => c switch
        {
            RouletteColor.Rouge => "#E8463C",
            RouletteColor.Noir => "#C8C8C8",
            _ => "#3CC864",
        };

        public static string Colored(RouletteColor c) => $"<color={Hex(c)}>{c.ToString().ToUpper()}</color>";
    }
}