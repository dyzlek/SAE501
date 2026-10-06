using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Management;

namespace SAE
{
    // Tirer à l'arc au casque, comme un vrai arc : l'arc est dans une main,
    // on approche l'autre main (drawHand) de la corde et on serre le grip (ou la gâchette) :
    // une flèche s'encoche, on recule la main pour tendre, on relâche le grip pour tirer.
    // C'est le geste VR du jeu : impossible à faire aussi bien à la souris (« test de l'écran »).
    // Sans casque (simulateur XR dans l'éditeur), on ne peut pas reculer la main en tenant le bouton :
    // la tension monte alors toute seule tant qu'on garde le bouton enfoncé (comme le clic droit du mode PC).
    public class VRArcher : MonoBehaviour
    {
        public Bow bow;
        public Transform drawHand;
        public InputActionProperty drawGrip;      // XRI Right Interaction/Select Value : le grip de la main qui tire
        public InputActionProperty drawTrigger;   // XRI Right Interaction/Activate Value : la gâchette marche aussi
        public float grabRadius = 0.4f;           // distance main-corde pour attraper la corde, en mètres (large : pas besoin de viser)
        public float simulatorDrawTime = 1f;      // sans casque : secondes pour tendre l'arc à fond

        bool drawing;
        bool simulated;                           // pas de vrai casque : c'est le simulateur XR qui bouge les manettes

        // Aucun casque n'a démarré (OpenXR sans « loader » actif) : on est dans le simulateur
        void Start() => simulated = !XRGeneralSettings.Instance || !XRGeneralSettings.Instance.Manager.activeLoader;

        void OnEnable()
        {
            drawGrip.action?.Enable();
            drawTrigger.action?.Enable();
        }

        void Update()
        {
            if (!bow || !bow.isActiveAndEnabled) return;   // l'arc n'est sorti que sur la carte
            bool pressed = Mathf.Max(Read(drawGrip), Read(drawTrigger)) > 0.5f;

            if (!drawing && pressed && Vector3.Distance(drawHand.position, bow.NockPoint) < grabRadius)
            {
                drawing = true;
                bow.Nock();
                PlayerRig.Buzz(drawHand, 0.3f);
            }
            else if (drawing && pressed)
            {
                float tension = simulated ? bow.Draw + Time.deltaTime / simulatorDrawTime : bow.TensionFor(drawHand.position);
                bow.SetDraw(tension);
                PlayerRig.Buzz(drawHand, 0.05f * bow.Draw, 0.02f);   // la corde « tire » de plus en plus dans la main
            }
            else if (drawing)
            {
                drawing = false;
                bow.Release(bow.ShootDirection);
                PlayerRig.Buzz(drawHand, 0.8f, 0.1f);
            }
        }

        static float Read(InputActionProperty input) => input.action != null ? input.action.ReadValue<float>() : 0f;
    }
}
