using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace SAE
{
    // Le joueur VR = l'XR Origin des Starter Assets (téléportation et rotation par crans déjà réglées dedans).
    // Ce script donne aux autres la tête et les deux mains, et téléporte le joueur (boutons JOUER / HUB).
    [RequireComponent(typeof(XROrigin))]
    public class PlayerRig : MonoBehaviour
    {
        public Transform head;        // la caméra du casque
        public Transform leftHand;    // les manettes
        public Transform rightHand;

        public static PlayerRig Local { get; private set; }

        XROrigin origin;
        CharacterController body;

        void Awake()
        {
            Local = this;
            origin = GetComponent<XROrigin>();
            body = GetComponent<CharacterController>();
        }

        // Pose le joueur sur 'spot', tourné dans sa direction. La hauteur du casque (vraie taille du joueur) est gardée.
        // Le CharacterController est coupé le temps du déplacement, sinon il remet le joueur à son ancienne place.
        public void TeleportTo(Transform spot)
        {
            if (body) body.enabled = false;
            origin.MatchOriginUpCameraForward(spot.up, spot.forward);
            origin.MoveCameraToWorldLocation(spot.position + Vector3.up * origin.CameraInOriginSpaceHeight);
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
