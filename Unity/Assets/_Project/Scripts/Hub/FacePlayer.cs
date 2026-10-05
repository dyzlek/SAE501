using UnityEngine;

namespace SAE
{
    // Tourne doucement l'objet vers le joueur (seulement autour de l'axe vertical).
    // Sert au coffre : il fait toujours face au joueur, où qu'il soit dans le hub.
    // yawOffset : à régler si l'avant du modèle n'est pas son axe +Z.
    public class FacePlayer : MonoBehaviour
    {
        public float yawOffset;
        public float turnSpeed = 6f;

        void LateUpdate()
        {
            var player = PlayerController.Local;
            if (!player) return;
            var dir = player.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            var target = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, yawOffset, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
        }
    }
}
