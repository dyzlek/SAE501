using UnityEngine;

namespace SAE
{
    // Garde une taille fixe dans le monde quel que soit l'échelle du parent.
    public class KeepWorldScale : MonoBehaviour
    {
        void LateUpdate()
        {
            var s = transform.parent ? transform.parent.lossyScale : Vector3.one;
            transform.localScale = new Vector3(1f / Mathf.Max(s.x, 1e-4f), 1f / Mathf.Max(s.y, 1e-4f), 1f / Mathf.Max(s.z, 1e-4f));
            transform.localPosition = new Vector3(0f, 0.04f / Mathf.Max(s.y, 1e-4f) + 0.5f, 0f);
        }
    }
}
