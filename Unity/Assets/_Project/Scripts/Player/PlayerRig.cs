using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

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

        // Le joueur actif (VR ou PC, selon PlayerMode)
        void OnEnable()
        {
            Local = this;
            // Le texte du coffre (« [Touche] Ouvrir… ») suit le joueur actif
            foreach (var prompt in FindObjectsByType<Sae501.Coffres.ChestPrompt>()) prompt.player = head;
        }

        // Bug du point d'apparition : au lancement, le casque n'est pas encore suivi (la caméra est à 0, 0, 0).
        // Le joueur était posé au bon endroit... puis le suivi démarrait et le décalait d'autant que l'endroit où il se
        // tenait dans sa pièce : il pouvait apparaître dans un meuble. On attend donc que le casque soit suivi
        // (la caméra a bougé, ou 2 s au plus), puis on le remet au point d'arrivée du niveau, regard vers l'avant.
        const float TrackingTimeout = 2f;

        IEnumerator Start()
        {
            if (!origin) yield break;   // mode PC : pas de suivi à attendre
            for (float t = 0f; t < TrackingTimeout && head.localPosition == Vector3.zero; t += Time.deltaTime)
                yield return null;
            yield return null;          // une image de plus : la position du casque est à jour
            var spawn = LevelSpawn.Of(Levels.Current);
            if (spawn) TeleportTo(spawn);
        }

        // Joueur assis (bouton ASSIS du hub) : le casque suit la vraie hauteur des yeux, assis on serait trop bas pour
        // atteindre les étagères et voir le plateau. On remonte donc tout le joueur de SeatedLift, comme s'il était debout.
        public const float SeatedLift = 0.5f;   // en mètres : la différence entre les yeux debout (~1,6 m) et assis (~1,1 m)
        public static bool Seated { get; private set; }

        public void ToggleSeated()
        {
            Seated = !Seated;
            if (origin && origin.CameraFloorOffsetObject)
            {
                var offset = origin.CameraFloorOffsetObject.transform;
                offset.localPosition = new Vector3(offset.localPosition.x, Seated ? SeatedLift : 0f, offset.localPosition.z);
            }
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

        // Petite vibration dans la manette qui contient 'hand' : le retour « c'est fait » de chaque action.
        public static void Buzz(Transform hand, float strength = 0.5f, float duration = 0.08f)
        {
            var haptics = hand ? hand.GetComponentInParent<HapticImpulsePlayer>() : null;
            if (haptics) haptics.SendHapticImpulse(strength, duration);
        }
    }
}
