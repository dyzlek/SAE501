using System;

namespace SAE
{
    // État partagé entre le hub et la carte. Statique : il survit au changement de scène.
    public static class GameState
    {
        public const int BoardSize = 8;

        // Singes posés sur le plateau. [ligne, colonne], même grille que la carte.
        public static readonly Monkey?[,] Board = new Monkey?[BoardSize, BoardSize];

        // Singe tenu en main (null = main vide).
        public static Monkey? Held;

        public static int Money;

        public static event Action Changed;
        public static void NotifyChanged() => Changed?.Invoke();

        public static void ClearBoard()
        {
            Array.Clear(Board, 0, Board.Length);
            NotifyChanged();
        }
    }
}
