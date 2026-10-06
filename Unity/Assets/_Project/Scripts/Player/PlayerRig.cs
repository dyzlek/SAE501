using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace SAE
{
    // Le joueur, en VR ou en mode PC (voir PlayerMode) : ce script donne aux autres la tête et les deux mains,
    // et téléporte le joueur (boutons JOUER / HUB).
    //   - VR : l'XR Origin des Starter Assets (téléportation et rotation par crans déjà réglées dedans) ;
    //   - PC : DesktopPlayer, la caméra fait office de tête et de mains.
    public class PlayerRig : MonoBehaviour
    {
        public Transform head;        // la caméra du casque (ou de l'écran)
        public Transform leftHand;    // les manettes (en mode PC : la caméra)
        public Transform rightHand;

        public static PlayerRig Local { get; private set; }

        XROrigin origin;
        CharacterController body;

        void Awake()
        {
            Local = this;
            origin = GetComponent<XROrigin>();       // null en mode PC
            body = GetComponent<CharacterController>();
            // Le texte du coffre (« [Touche] Ouvrir… ») suit le joueur actif
            foreach (var prompt in FindObjectsByType<Sae501.Coffres.ChestPrompt>()) prompt.player = head;
        }

        // Pose le joueur sur 'spot', tourné dans sa direction. La hauteur du casque (vraie taille du joueur) est gardée.
        // Le CharacterController est coupé le temps du déplacement, sinon il remet le joueur à son ancienne place.
        public void TeleportTo(Transform spot)
        {
            if (body) body.enabled = false;
            if (origin)
            {
                origin.MatchOriginUpCameraForward(spot.up, spot.forward);
                origin.MoveCameraToWorldLocation(spot.position + Vector3.up * origin.CameraInOriginSpaceHeight);
            }
            else transform.SetPositionAndRotation(spot.position, Quaternion.Euler(0f, spot.eulerAngles.y, 0f));
            if (body) body.enabled = true;
        }

        // Petite vibration dans la manette qui contient 'hand' : le retour « c'est fait » de chaque action.
        public static void Buzz(Transform hand, float strength = 0.5f, float duration = 0.08f)
        {
            var haptics = hand ? hand.GetComponentInParent<HapticImpulsePlayer>() : null;
            if (haptics) haptics.SendHapticImpulse(strength, duration);
        }
    }
}
