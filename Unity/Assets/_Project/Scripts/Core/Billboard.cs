using UnityEngine;

namespace SAE
{
    // Le texte se tourne vers le joueur, en restant droit (il ne penche pas quand on incline la tête : plus lisible
    // et plus confortable en VR).
    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (!cam) return;
            var away = transform.position - cam.transform.position;   // un TextMesh se lit de dos : on regarde dans le même sens que le joueur
            away.y = 0f;
            if (away.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(away);
        }
    }
}
