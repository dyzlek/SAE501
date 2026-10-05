using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // La piste en labyrinthe, partagée par le plateau du hub (en petit) et la carte (en grand).
    // S = départ des ballons, E = sortie, # = piste, . = case libre pour un singe.
    public static class MapLayout
    {
        public const int Size = 8;

        static readonly string[] rows =
        {
            "S#######",
            ".......#",
            ".#######",
            ".#......",
            ".######.",
            "......#.",
            "E######.",
            "........",
        };

        public static char At(int row, int col) => rows[row][col];
        public static bool IsPath(int row, int col) => rows[row][col] != '.';

        // Position locale du centre d'une case. La ligne 0 est au fond, loin du joueur.
        public static Vector3 CellLocal(int row, int col, float tile) =>
            new Vector3((col - (Size - 1) / 2f) * tile, 0f, ((Size - 1) / 2f - row) * tile);

        // Cases de la piste dans l'ordre, du départ à la sortie (x = colonne, y = ligne).
        public static List<Vector2Int> OrderedPath()
        {
            var path = new List<Vector2Int>();
            var current = Find('S');
            var visited = new HashSet<Vector2Int>();
            Vector2Int[] dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

            while (true)
            {
                path.Add(current);
                visited.Add(current);
                if (At(current.y, current.x) == 'E') break;

                bool moved = false;
                foreach (var d in dirs)
                {
                    var next = current + d;
                    if (next.x < 0 || next.y < 0 || next.x >= Size || next.y >= Size) continue;
                    if (!IsPath(next.y, next.x) || visited.Contains(next)) continue;
                    current = next;
                    moved = true;
                    break;
                }
                if (!moved) { Debug.LogError("MapLayout : la piste ne mène pas à E."); break; }
            }
            return path;
        }

        static Vector2Int Find(char c)
        {
            for (int r = 0; r < Size; r++)
                for (int col = 0; col < Size; col++)
                    if (rows[r][col] == c) return new Vector2Int(col, r);
            return Vector2Int.zero;
        }
    }
}
