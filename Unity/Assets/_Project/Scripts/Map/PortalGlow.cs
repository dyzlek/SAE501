using UnityEngine;

namespace SAE
{
    // Le voile coloré d'un portail (entrée ou sortie du chemin) : il « respire » doucement, pour attirer le regard.
    public class PortalGlow : MonoBehaviour
    {
        public float pulse = 0.06f;   // amplitude, en part de la taille
        public float speed = 2f;      // battements par seconde (environ)

        Vector3 baseScale;

        void Awake() => baseScale = transform.localScale;

        void Update()
        {
            float k = 1f + Mathf.Sin(Time.time * speed * Mathf.PI) * pulse;
            transform.localScale = new Vector3(baseScale.x * k, baseScale.y * k, baseScale.z);
        }
    }
}
