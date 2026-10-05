using UnityEngine;
using UnityEngine.InputSystem;

namespace Sae501.Coffres
{
    // Interaction pour tester à l'écran (sans casque) : touche E près du coffre,
    // ou clic gauche en visant le coffre. À retirer quand on passera au rig XR.
    public class DesktopInteractor : MonoBehaviour
    {
        public ChestController chest;
        public PlayerController player;
        public Camera playerCamera;

        void Update()
        {
            if (!player.ControlEnabled) return; // menu ouvert ou souris libérée

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame && chest.IsInRange(transform.position))
                chest.Interact();

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && chest.IsInRange(transform.position))
            {
                // Rayon depuis le centre de l'écran (là où est le point de visée).
                var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
                if (Physics.Raycast(ray, out var hit, chest.interactDistance + 1f)
                    && hit.collider.GetComponentInParent<ChestController>() == chest)
                    chest.Interact();
            }
        }
    }
}
