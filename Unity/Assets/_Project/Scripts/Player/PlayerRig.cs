using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace SAE
{
    // Le joueur, en VR ou en mode PC (voir PlayerMode) : ce script donne aux autres la tête et les deux mains,
    // et le remet sur un point (FallGuard, après une chute). Changer de niveau, c'est changer de scène (Levels).
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
            origin = GetComponent<XROrigin>();       // null en mode PC
            body = GetComponent<CharacterController>();
        }

        // Chaque scène a son joueur : celui qui s'allume (on arrive dans son niveau) devient le joueur actif
        void OnEnable()
        {
            Local = this;
            UseMyTeleporter();
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

        // Le joueur ne se cogne pas à cet objet (banane, singe à saisir) : sinon, en le tenant sous ses pieds,
        // son CharacterController monte dessus et il s'envole (le bug du « prop fly »). Le joueur actif seulement :
        // c'est le seul qui marche (l'autre mode est désactivé).
        public static void IgnoreCollisions(GameObject held)
        {
            foreach (var player in GameObject.FindGameObjectsWithTag(Tags.Joueur))
                foreach (var body in player.GetComponentsInChildren<CharacterController>())
                    foreach (var c in held.GetComponentsInChildren<Collider>())
                        Physics.IgnoreCollision(c, body);
        }

        // Les zones de téléportation (sol du hub, prairie et cases de la carte) envoient la téléportation à un
        // « téléporteur » (TeleportationProvider). XRI garde en mémoire le premier trouvé, celui du joueur du hub :
        // sur la carte, la cible s'affichait mais rien ne se passait au relâchement (ce joueur-là est éteint).
        // Le joueur qui s'allume branche donc toutes les zones des deux scènes sur son propre téléporteur.
        void UseMyTeleporter()
        {
            var mine = GetComponentInChildren<TeleportationProvider>();
            if (!mine) return;   // mode PC : on marche, pas de téléportation
            foreach (var area in FindObjectsByType<BaseTeleportationInteractable>(FindObjectsInactive.Include))
                area.teleportationProvider = mine;
        }

        // Petite vibration dans la manette qui contient 'hand' : le retour « c'est fait » de chaque action.
        public static void Buzz(Transform hand, float strength = 0.5f, float duration = 0.08f)
        {
            var haptics = hand ? hand.GetComponentInParent<HapticImpulsePlayer>() : null;
            if (haptics) haptics.SendHapticImpulse(strength, duration);
        }
    }
}
