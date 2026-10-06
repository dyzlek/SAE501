using UnityEngine;
using UnityEngine.InputSystem;

namespace SAE
{
    // Tirer à l'arc au casque, comme un vrai arc : l'arc est dans une main,
    // on approche l'autre main (drawHand) de la corde et on serre la gâchette de prise (grip) :
    // une flèche s'encoche, on recule la main pour tendre, on relâche le grip pour tirer.
    // C'est le geste VR du jeu : impossible à faire aussi bien à la souris (« test de l'écran »).
    public class VRArcher : MonoBehaviour
    {
        public Bow bow;
        public Transform drawHand;
        public InputActionProperty drawGrip;   // XRI Right Interaction/Select Value : le grip de la main qui tire
        public float grabRadius = 0.15f;       // distance main-corde pour attraper la corde, en mètres

        bool drawing;

        void OnEnable() => drawGrip.action?.Enable();

        void Update()
        {
            if (!bow || !bow.isActiveAndEnabled) return;   // l'arc n'est sorti que sur la carte
            bool pressed = drawGrip.action != null && drawGrip.action.ReadValue<float>() > 0.5f;

            if (!drawing && pressed && Vector3.Distance(drawHand.position, bow.NockPoint) < grabRadius)
            {
                drawing = true;
                bow.Nock();
                PlayerRig.Buzz(drawHand, 0.3f);
            }
            else if (drawing && pressed)
            {
                bow.SetDraw(bow.TensionFor(drawHand.position));
                PlayerRig.Buzz(drawHand, 0.05f * bow.Draw, 0.02f);   // la corde « tire » de plus en plus dans la main
            }
            else if (drawing)
            {
                drawing = false;
                bow.Release(bow.ShootDirection);
                PlayerRig.Buzz(drawHand, 0.8f, 0.1f);
            }
        }
    }
}
