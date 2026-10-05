using UnityEngine;

namespace SAE
{
    // Le texte se tourne toujours vers la caméra.
    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam) transform.rotation = cam.transform.rotation;
        }
    }
}
