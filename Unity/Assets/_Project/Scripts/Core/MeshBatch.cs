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

        // --- Formes de base, low-poly (faces plates, comme le reste du décor) ---

        static Mesh cube, gem;

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

        // Une « boule » à 20 faces (icosaèdre) : cailloux, buissons, fleurs. Rayon 0,5 (comme la sphère de Unity).
        public static Mesh Gem
        {
            get
            {
                if (gem) return gem;
                float t = (1f + Mathf.Sqrt(5f)) / 2f;
                var corners = new[]
                {
                    new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                    new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                    new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
                };
                int[] faces =
                {
                    0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                    3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
                };
                // Trois sommets à part par face : chaque face a sa normale, d'où l'aspect taillé
                var vertices = new Vector3[faces.Length];
                var triangles = new int[faces.Length];
                for (int i = 0; i < faces.Length; i++)
                {
                    // la liste des faces tourne dans le sens inverse de Unity : on lit chaque triangle à l'envers
                    vertices[i] = corners[faces[i / 3 * 3 + 2 - i % 3]].normalized * 0.5f;
                    triangles[i] = i;
                }
                gem = new Mesh { name = "Gemme", vertices = vertices, triangles = triangles };
                gem.RecalculateNormals();
                gem.RecalculateBounds();
                return gem;
            }
        }
    }
}
