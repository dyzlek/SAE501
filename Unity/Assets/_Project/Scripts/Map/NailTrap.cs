using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Le piège à clous de la bombe Clous (#134) : un tapis de clous posé sur le chemin des ballons.
    // Chaque ballon qui passe dessus perd une couche (une seule fois par ballon), jusqu'à MaxPops ballons ou Lifetime secondes.
    // Pas de physique : on compare les distances avec Balloon.All, comme l'explosion des flèches.
    public class NailTrap : MonoBehaviour
    {
        const float Reach = 0.7f;          // en mètres autour du piège
        const float MaxSnap = 3f;          // la bombe doit tomber à moins de 3 m du chemin
        const int MaxPops = 20;
        const float Lifetime = 20f;

        readonly HashSet<Balloon> popped = new HashSet<Balloon>();
        int popsLeft = MaxPops;
        float endAt;
        float groundOffset;                // le piège est à la hauteur des ballons ; son visuel, au sol

        // Pose le piège au point du chemin le plus proche de 'at' (rien si la bombe est tombée trop loin)
        public static void PlaceNear(Vector3 at)
        {
            var spawner = WaveSpawner.Instance;
            if (!spawner || !ClosestOnPath(spawner.Path, at, out var point)) return;
            var go = new GameObject("Piège à clous");
            go.transform.position = point;
            go.AddComponent<NailTrap>().groundOffset = -spawner.balloonHeight;
        }

        static bool ClosestOnPath(IReadOnlyList<Vector3> path, Vector3 at, out Vector3 best)
        {
            best = at;
            float bestSqr = MaxSnap * MaxSnap;
            bool found = false;
            for (int i = 0; i + 1 < path.Count; i++)
            {
                // Le point le plus proche sur le segment i (le chemin est une suite de segments droits)
                var a = path[i];
                var ab = path[i + 1] - a;
                float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector3.Dot(at - a, ab) / ab.sqrMagnitude) : 0f;
                var p = a + ab * t;
                float d = (p - at).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = p; found = true; }
            }
            return found;
        }

        void Start()
        {
            endAt = Time.time + Lifetime;
            // Le visuel, posé au sol sous le chemin : une plaque sombre hérissée de 9 pointes grises
            Visuals.Box("Plaque", transform, new Vector3(0f, groundOffset + 0.02f, 0f), new Vector3(0.9f, 0.04f, 0.9f), new Color(0.2f, 0.18f, 0.16f));
            for (int x = -1; x <= 1; x++)
                for (int z = -1; z <= 1; z++)
                    Visuals.Box("Clou", transform, new Vector3(x * 0.28f, groundOffset + 0.1f, z * 0.28f), new Vector3(0.04f, 0.14f, 0.04f), new Color(0.6f, 0.6f, 0.65f));
        }

        void Update()
        {
            foreach (var b in Balloon.All)
            {
                if (popsLeft <= 0) break;
                if ((b.transform.position - transform.position).sqrMagnitude > Reach * Reach || !popped.Add(b)) continue;
                b.Pop(1);
                popsLeft--;
            }
            if (popsLeft <= 0 || Time.time > endAt) Destroy(gameObject);
        }
    }
}
