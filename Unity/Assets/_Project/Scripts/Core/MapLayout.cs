using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Le chemin des ballons. La même description sert à la carte (en grand) et au plateau (en petit).
    // Inspiré des cartes de Bloons TD (Monkey Lane) : un chemin de dalles qui fait des boucles et SE CROISE deux fois.
    // Les ballons passent donc deux fois aux croisements : c'est là qu'un singe est le plus utile.
    // Le chemin est une suite de lignes droites (points de passage) dont les coins sont arrondis.
    public static class MapLayout
    {
        public const int Size = 8;
        public const float Tile = 3f;                 // une case de 3 m (sert à la taille du plateau du hub)
        public const float HalfExtent = Size * Tile / 2f;
        public const float PathWidth = 2.2f;          // largeur du chemin de dalles, en mètres
        const float CornerRadius = 1.1f;              // rayon des coins arrondis, en mètres
        const int CornerSteps = 6;                    // points calculés dans chaque coin

        // Points de passage (x, z) en mètres, centre de la carte = (0, 0), le joueur est au sud (z négatif).
        static readonly Vector2[] waypoints =
        {
            new Vector2(-12f, 5f),     // entrée, bord ouest
            new Vector2(2.5f, 5f),
            new Vector2(2.5f, 10.5f),   // petite boucle au nord
            new Vector2(-3.5f, 10.5f),
            new Vector2(-3.5f, -6f),   // longue descente : croise deux fois le chemin
            new Vector2(-8.5f, -6f),
            new Vector2(-8.5f, 0.5f),   // boucle à l'ouest
            new Vector2(6f, 0.5f),
            new Vector2(6f, 6.5f),
            new Vector2(10.8f, 6.5f),   // boucle à l'est
            new Vector2(10.8f, -4f),
            new Vector2(1.5f, -4f),
            new Vector2(1.5f, -12f),    // sortie, bord sud, juste devant l'estrade du joueur
        };

        static List<Vector3> pathPoints;

        // Le chemin du départ à la sortie (repère de la carte, y = 0), coins arrondis.
        public static List<Vector3> PathPoints()
        {
            if (pathPoints != null) return pathPoints;
            pathPoints = new List<Vector3> { ToMap(waypoints[0]) };
            for (int i = 1; i < waypoints.Length - 1; i++)
            {
                // Le coin est remplacé par un arc (courbe de Bézier) qui commence avant lui et finit après
                var corner = waypoints[i];
                var from = corner - (corner - waypoints[i - 1]).normalized * CornerRadius;
                var to = corner + (waypoints[i + 1] - corner).normalized * CornerRadius;
                for (int s = 0; s <= CornerSteps; s++)
                {
                    float t = s / (float)CornerSteps;
                    var p = (1 - t) * (1 - t) * from + 2 * (1 - t) * t * corner + t * t * to;
                    pathPoints.Add(ToMap(p));
                }
            }
            pathPoints.Add(ToMap(waypoints[waypoints.Length - 1]));
            return pathPoints;
        }

        static Vector3 ToMap(Vector2 p) => new Vector3(p.x, 0f, p.y);

        // Peut-on poser un singe en pos (x, z) ? Dans la carte, et son centre à au moins margin mètres du bord du chemin.
        public static bool CanPlace(Vector2 pos, float margin)
        {
            if (Mathf.Abs(pos.x) > HalfExtent - margin || Mathf.Abs(pos.y) > HalfExtent - margin) return false;
            return DistanceToPath(pos) >= PathWidth / 2f + margin;
        }

        // Distance (en mètres) entre un point (x, z) et le chemin le plus proche.
        public static float DistanceToPath(Vector2 pos)
        {
            var pts = PathPoints();
            float best = float.MaxValue;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                var a = new Vector2(pts[i].x, pts[i].z);
                var b = new Vector2(pts[i + 1].x, pts[i + 1].z);
                best = Mathf.Min(best, DistanceToSegment(pos, a, b));
            }
            return best;
        }

        static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + t * ab);
        }
    }
}
