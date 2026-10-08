using UnityEngine;

namespace SAE
{
    // Ce qui bouge dans un portail (entrée ou sortie du chemin) : le voile lumineux « respire » doucement
    // et le tourbillon tourne, pour attirer le regard sur l'endroit d'où viennent (ou où vont) les ballons.
    public class PortalGlow : MonoBehaviour
    {
        public Transform swirl;       // le tourbillon, qui tourne sur lui-même
        public float spin = 90f;      // en degrés par seconde (le signe donne le sens)
        public float pulse = 0.05f;   // amplitude de la respiration, en part de la taille
        public float speed = 1.5f;    // respirations par seconde (environ)

        Vector3 baseScale;

        void Awake() => baseScale = transform.localScale;

        void Update()
        {
            float k = 1f + Mathf.Sin(Time.time * speed * Mathf.PI) * pulse;
            transform.localScale = new Vector3(baseScale.x * k, baseScale.y * k, baseScale.z);
            if (swirl) swirl.Rotate(0f, 0f, spin * Time.deltaTime, Space.Self);
        }
    }
}
