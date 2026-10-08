using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Le dessin du chemin des ballons (MapLayout) : un ruban plat qui suit la courbe, comme le chemin de terre de Bloons TD.
    // Le générateur en pose deux l'un sur l'autre : une bordure plus large et plus sombre, puis la terre.
    // Le maillage est refait au lancement : il suit toujours MapLayout, même si on change les points de passage.
    [RequireComponent(typeof(MeshFilter))]
    public class TrackView : MonoBehaviour
    {
        public float scale = 1f;     // 1 sur la carte, plus petit sur le plateau du hub
        public float width = MapLayout.PathWidth;   // largeur du ruban, en mètres de la carte

        void Awake() => Build();

        public void Build()
        {
            var points = MapLayout.PathPoints();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            float half = width * scale / 2f;

            for (int i = 0; i < points.Count; i++)
            {
                // Direction du chemin à ce point (moyenne avant/après), puis un pas à gauche et à droite
                var forward = points[Mathf.Min(i + 1, points.Count - 1)] - points[Mathf.Max(i - 1, 0)];
                var side = Vector3.Cross(Vector3.up, forward.normalized) * half;
                var center = points[i] * scale;
                vertices.Add(center - side);
                vertices.Add(center + side);
                if (i == 0) continue;
                int a = vertices.Count - 4;   // les deux points précédents, puis les deux nouveaux
                triangles.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
            }

            var mesh = new Mesh { name = "Chemin" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }
    }
}
