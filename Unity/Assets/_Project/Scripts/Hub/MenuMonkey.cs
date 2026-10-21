using UnityEngine;

namespace SAE
{
    // Un singe de l'estrade du menu : il sautille, se dandine et fait un tour sur lui-même de temps en temps
    public class MenuMonkey : MonoBehaviour
    {
        public float phase;
        Vector3 basePos;
        Quaternion baseRot;

        void Start() { basePos = transform.localPosition; baseRot = transform.localRotation; }

        void Update()
        {
            float t = Time.unscaledTime * 2.2f + phase;
            float hop = Mathf.Abs(Mathf.Sin(t)) * 0.18f;                       // petits sauts
            float sway = Mathf.Sin(t * 0.5f) * 15f;                            // il se dandine
            float spin = Mathf.Repeat(t / 6f, 1f) > 0.9f ? Mathf.Repeat(t / 6f, 1f) * 10f - 9f : 0f;   // un tour toutes les ~17 s
            transform.localPosition = basePos + Vector3.up * hop;
            transform.localRotation = baseRot * Quaternion.Euler(0f, sway + spin * 360f, 0f);
        }
    }
}
