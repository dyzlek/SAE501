using UnityEngine;

namespace SAE
{
    // Mesure la vitesse d'un objet pendant qu'on le tient en main, pour pouvoir le LANCER.
    // XRI sait lancer un objet tout seul (« throw on detach »), mais pas un objet qui était cinématique quand on l'a pris
    // (nos bananes et nos singes attendent sans tomber) : il lui remet son état d'avant au lâcher, et l'élan est perdu.
    // On fait donc le calcul nous-mêmes : la position de l'objet aux dernières images, et la vitesse moyenne entre
    // la plus ancienne et la plus récente (une moyenne lisse les petits tremblements de la main).
    public class ThrowVelocity : MonoBehaviour
    {
        const int Samples = 6;            // environ 0,08 s à 72 images/s : assez court pour suivre le geste
        public float boost = 1.3f;        // un lancer en VR paraît toujours un peu mou sans ce petit coup de pouce

        readonly Vector3[] positions = new Vector3[Samples];
        readonly float[] times = new float[Samples];
        int count, next;

        // À appeler quand on prend l'objet : on oublie les anciennes positions
        public void Clear() => count = next = 0;

        void Update()
        {
            positions[next] = transform.position;
            times[next] = Time.time;
            next = (next + 1) % Samples;
            if (count < Samples) count++;
        }

        // La vitesse de lancer, en m/s (zéro tant qu'on n'a pas au moins deux mesures)
        public Vector3 Velocity
        {
            get
            {
                if (count < 2) return Vector3.zero;
                int newest = (next - 1 + Samples) % Samples;
                int oldest = count < Samples ? 0 : next;
                float dt = times[newest] - times[oldest];
                return dt > 0f ? (positions[newest] - positions[oldest]) / dt * boost : Vector3.zero;
            }
        }
    }
}
