using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Regroupe beaucoup de petites formes (dalles, fleurs, cailloux…) en UN maillage par couleur.
    // Pour le casque : 300 fleurs = 300 objets à dessiner, alors qu'ici elles ne coûtent que quelques objets.
    public class MeshBatch
    {
        readonly Dictionary<Color, List<CombineInstance>> parts = new Dictionary<Color, List<CombineInstance>>();

        // Ajoute une forme : mesh posé en pos (repère du parent), tourné de rot, à la taille size (en mètres).
        public void Add(Mesh mesh, Vector3 pos, Quaternion rot, Vector3 size, Color color)
        {
            if (!parts.TryGetValue(color, out var list)) parts[color] = list = new List<CombineInstance>();
            list.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(pos, rot, size) });
        }

        // Crée les objets sous parent : un par couleur, avec le matériau du jeu (Visuals.Box) et le maillage fusionné.
        public void Build(Transform parent, string name)
        {
            foreach (var pair in parts)
            {
                var go = Visuals.Box(name, parent, Vector3.zero, Vector3.one, pair.Key);
                var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(pair.Value.ToArray(), true, true);
                mesh.RecalculateBounds();
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                go.isStatic = true;
            }
        }

        // --- Formes de base ---

        static Mesh cube, ball;

        public static Mesh Cube
        {
            get
            {
                if (cube) return cube;
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube = go.GetComponent<MeshFilter>().sharedMesh;
                Visuals.Kill(go);
                return cube;
            }
        }

        // Une boule lisse (icosaèdre découpé deux fois, 320 faces) : cailloux, buissons, fleurs. Rayon 0,5 (comme la sphère de Unity),
        // mais 4 fois moins de sommets que celle de Unity : ça compte quand on en pose des centaines.
        public static Mesh Ball
        {
            get
            {
                if (ball) return ball;
                float t = (1f + Mathf.Sqrt(5f)) / 2f;
                var points = new List<Vector3>
                {
                    new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                    new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                    new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
                };
                var faces = new List<int>
                {
                    0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                    3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
                };
                for (int i = 0; i < points.Count; i++) points[i] = points[i].normalized * 0.5f;

                // Chaque triangle est coupé en 4 ; les nouveaux sommets (milieux des côtés) sont repoussés sur la boule
                var middles = new Dictionary<long, int>();
                int Middle(int a, int b)
                {
                    long key = a < b ? (long)a << 32 | (uint)b : (long)b << 32 | (uint)a;   // un côté partagé n'a qu'un milieu
                    if (middles.TryGetValue(key, out int index)) return index;
                    points.Add(((points[a] + points[b]) / 2f).normalized * 0.5f);
                    return middles[key] = points.Count - 1;
                }
                for (int level = 0; level < 2; level++)
                {
                    var split = new List<int>();
                    for (int f = 0; f < faces.Count; f += 3)
                    {
                        int a = faces[f], b = faces[f + 1], c = faces[f + 2];
                        int ab = Middle(a, b), bc = Middle(b, c), ca = Middle(c, a);
                        split.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                    }
                    faces = split;
                }

                // Toutes les faces tournées vers l'extérieur (sinon on voit l'intérieur de la boule : des trous noirs)
                for (int f = 0; f < faces.Count; f += 3)
                {
                    Vector3 a = points[faces[f]], b = points[faces[f + 1]], c = points[faces[f + 2]];
                    if (Vector3.Dot(Vector3.Cross(b - a, c - a), a + b + c) < 0f)
                        (faces[f + 1], faces[f + 2]) = (faces[f + 2], faces[f + 1]);
                }

                ball = new Mesh { name = "Boule" };
                ball.SetVertices(points);
                ball.SetTriangles(faces, 0);
                ball.SetNormals(points.ConvertAll(p => p.normalized));   // normales lisses : pas de facettes
                ball.RecalculateBounds();
                return ball;
            }
        }
    }
}
