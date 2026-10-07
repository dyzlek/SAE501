using UnityEngine;
using UnityEngine.InputSystem;

namespace SAE
{
    // Tirer à l'arc en mode PC, pour tester sans casque : clic droit maintenu = une flèche s'encoche
    // et l'arc se tend petit à petit ; relâcher = la flèche part vers le viseur (centre de l'écran).
    public class DesktopArcher : MonoBehaviour
    {
        public Bow bow;
        public Transform aim;            // la caméra : la flèche part vers le centre de l'écran
        public float fullDrawTime = 1f;  // secondes pour tendre l'arc à fond

        bool drawing;

        void Start() => bow.aim = aim;   // la courbe de visée de l'arc part elle aussi vers le centre de l'écran

        void Update()
        {
            var mouse = Mouse.current;
            if (!bow || !bow.isActiveAndEnabled || mouse == null) return;   // l'arc n'est sorti que sur la carte

            if (mouse.rightButton.wasPressedThisFrame)
            {
                drawing = true;
                bow.Nock();
            }
            if (drawing && mouse.rightButton.isPressed)
                bow.SetDraw(bow.Draw + Time.deltaTime / fullDrawTime);
            if (drawing && mouse.rightButton.wasReleasedThisFrame)
            {
                drawing = false;
                bow.Release(aim.forward);
            }
        }
    }
}
