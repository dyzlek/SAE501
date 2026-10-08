using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Le chemin des ballons. La même description sert à la carte (en grand) et au plateau (en petit).
    // Comme dans les cartes de Bloons TD (Monkey Meadow, In The Loop…), c'est un chemin de terre qui serpente
    // avec des virages arrondis : il entre par un bord de la carte et ressort par un autre, près du joueur.
    // On le décrit par quelques points de passage ; la courbe lisse qui passe par eux est calculée (Catmull-Rom).
    public static class MapLayout
    {
        public const int Size = 8;
        public const float Tile = 3f;                 // une case de 3 m (sert à la taille du plateau du hub)
        public const float HalfExtent = Size * Tile / 2f;
        public const float PathWidth = 3f;            // largeur du chemin de terre, en mètres
        const int StepsPerSegment = 10;               // points calculés entre deux points de passage

        // Points de passage (x, z) en mètres, centre de la carte = (0, 0), le joueur est au sud (z négatif).
        // Les allers-retours sont espacés d'au moins 5 m : il reste de la place pour poser des singes entre deux.
        static readonly Vector2[] waypoints =
        {
            new Vector2(-12f, 7f),    // entrée, bord ouest
            new Vector2(-3f, 7f),
            new Vector2(3f, 9f),      // petite bosse vers le nord
            new Vector2(8.5f, 8f),
            new Vector2(9f, 3f),      // virage en épingle à l'est
            new Vector2(5f, 0f),
            new Vector2(-2f, 1f),     // retour par le milieu
            new Vector2(-8f, 0f),
            new Vector2(-8.5f, -5f),  // descente à l'ouest
            new Vector2(-3f, -8f),
            new Vector2(4f, -6f),
            new Vector2(8f, -8.5f),
            new Vector2(8f, -12f),    // sortie, bord sud, à droite de l'estrade du joueur
        };

        static List<Vector3> pathPoints;

        // La courbe du chemin, du départ à la sortie (repère de la carte, y = 0).
        public static List<Vector3> PathPoints()
        {
            if (pathPoints != null) return pathPoints;
            pathPoints = new List<Vector3>();
            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                // Catmull-Rom : la courbe passe par chaque point, en tenant compte du point d'avant et d'après
                var p0 = waypoints[Mathf.Max(i - 1, 0)];
                var p1 = waypoints[i];
                var p2 = waypoints[i + 1];
                var p3 = waypoints[Mathf.Min(i + 2, waypoints.Length - 1)];
                for (int s = 0; s < StepsPerSegment; s++)
                {
                    var p = CatmullRom(p0, p1, p2, p3, s / (float)StepsPerSegment);
                    pathPoints.Add(new Vector3(p.x, 0f, p.y));
                }
            }
            var last = waypoints[waypoints.Length - 1];
            pathPoints.Add(new Vector3(last.x, 0f, last.y));
            return pathPoints;
        }

        static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
        }

        // Peut-on poser un singe de rayon 'radius' en pos (x, z) ? Dans la carte et pas sur le chemin.
        public static bool CanPlace(Vector2 pos, float radius)
        {
            if (Mathf.Abs(pos.x) > HalfExtent - radius || Mathf.Abs(pos.y) > HalfExtent - radius) return false;
            var pts = PathPoints();
            float minDist = PathWidth / 2f + radius;
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
    }
}
