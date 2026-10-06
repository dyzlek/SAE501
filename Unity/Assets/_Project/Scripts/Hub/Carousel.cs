using UnityEngine;

namespace SAE
{
    // Étagère tournante de la bibliothèque : un meuble carré, une face par type de singe, posé à portée de main.
    // On le fait tourner d'un quart de tour pour amener une autre face devant soi (à la souris : clic sur la
    // manivelle du dessus ; au casque, on la fera tourner à la main). Les cases (LibrarySlot) tournent avec lui.
    public class Carousel : MonoBehaviour, IClickable
    {
        public int faces = 4;
        public float turnSpeed = 360f;   // degrés par seconde

        float targetYaw;

        public string GetHint(Vector3 point) => "Tourner l'étagère";

        public void OnClick(PlayerController player, Vector3 point) => Turn();

        public void Turn() => targetYaw += 360f / faces;

        void Start() => targetYaw = transform.eulerAngles.y;

        void Update()
        {
            var target = Quaternion.Euler(0f, targetYaw, 0f);
            if (transform.rotation != target)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
        }
    }
}
