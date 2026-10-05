using UnityEngine;

namespace Sae501.Coffres
{
    // Tourne l'objet (texte, panneau) face à la caméra du joueur, en restant droit.
    // Utile pour que l'interface dans le décor suive le regard, à l'écran comme en VR.
    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;

            // Direction opposée au joueur, sans l'inclinaison verticale (le panneau reste vertical).
            var away = transform.position - cam.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(away);
        }
    }
}
