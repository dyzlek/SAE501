using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // La piste en labyrinthe. La même description sert à la carte (en grand) et au plateau (en petit).
    // S = départ des ballons, E = sortie, # = piste, . = terrain où l'on peut poser des singes.
    public static class MapLayout
    {
        public const int Size = 8;
        public const float Tile = 3f;                 // taille d'une case de la carte, en mètres
        public const float HalfExtent = Size * Tile / 2f;

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

        static List<Vector3> pathPoints;

        public static char At(int row, int col) => rows[row][col];

        // Centre d'une case dans le repère de la carte. La ligne 0 est au fond, loin du joueur.
        public static Vector3 CellLocal(int row, int col) =>
            new Vector3((col - (Size - 1) / 2f) * Tile, 0f, ((Size - 1) / 2f - row) * Tile);

        // Centres des cases de la piste dans l'ordre, du départ à la sortie (repère de la carte).
        public static List<Vector3> PathPoints()
        {
            if (pathPoints != null) return pathPoints;
            pathPoints = new List<Vector3>();
            var current = Find('S');
            var visited = new HashSet<Vector2Int>();
            Vector2Int[] dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

            while (true)
            {
                pathPoints.Add(CellLocal(current.y, current.x));
                visited.Add(current);
                if (At(current.y, current.x) == 'E') break;

                bool moved = false;
                foreach (var d in dirs)
                {
                    var next = current + d;
                    if (next.x < 0 || next.y < 0 || next.x >= Size || next.y >= Size) continue;
                    if (At(next.y, next.x) == '.' || visited.Contains(next)) continue;
                    current = next;
                    moved = true;
                    break;
                }
                if (!moved) { Debug.LogError("MapLayout : la piste ne mène pas à E."); break; }
            }
            return pathPoints;
        }

        // Peut-on poser un singe de rayon 'radius' en pos (x, z) ? Dans la carte et pas sur la piste.
        public static bool CanPlace(Vector2 pos, float radius)
        {
            if (Mathf.Abs(pos.x) > HalfExtent - radius || Mathf.Abs(pos.y) > HalfExtent - radius) return false;
            var pts = PathPoints();
            float minDist = Tile / 2f + radius;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                var a = new Vector2(pts[i].x, pts[i].z);
                var b = new Vector2(pts[i + 1].x, pts[i + 1].z);
                if (DistanceToSegment(pos, a, b) < minDist) return false;
            }
            return true;
        }

        static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + t * ab);
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
