using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SAE
{
    // Attraper le plateau de la roulette pour le faire tourner (voir RouletteWheel), posé sur le plateau lui-même.
    //   VR : on serre le grip sur le plateau (en le touchant ou en le visant avec le rayon), on déplace la main, on lâche.
    //        Le déplacement de la main le long du bord fait tourner la roue (de près comme de loin, avec le rayon).
    //   PC : clic maintenu sur la roue, on glisse la souris de côté, on relâche (DesktopPlayer appelle BeginMouse / MouseDrag / EndMouse).
    // Pendant le tutoriel, la roue suit les mêmes règles que les boutons : refusée si l'étape ne la demande pas.
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class WheelGrab : MonoBehaviour
    {
        public RouletteWheel wheel;
        public float lever = 0.25f;           // m : bouger la main de 'lever' le long du bord fait tourner la roue d'un radian
        public float mouseDegrees = 0.5f;     // degrés de roue par pixel de souris

        XRSimpleInteractable interactable;
        Transform hand;                       // la main qui tient la roue (null : personne)
        Vector3 lastHand;
        Vector3 grabDir;                      // d'où on tient la roue (depuis son centre, dans son plan) : tourne avec elle

        Vector3 Axis => wheel.rotor.up;
        Vector3 Center => wheel.rotor.position;

        void Awake() => interactable = GetComponent<XRSimpleInteractable>();

        void OnEnable()
        {
            interactable.selectEntered.AddListener(OnGrab);
            interactable.selectExited.AddListener(OnRelease);
        }

        void OnDisable()
        {
            interactable.selectEntered.RemoveListener(OnGrab);
            interactable.selectExited.RemoveListener(OnRelease);
        }

        // --- VR ---

        void OnGrab(SelectEnterEventArgs args)
        {
            if (hand || !Begin()) return;
            hand = args.interactorObject.transform;
            lastHand = hand.position;
            grabDir = Vector3.ProjectOnPlane(lastHand - Center, Axis);
            if (grabDir.sqrMagnitude < 0.0025f) grabDir = -wheel.transform.forward;   // la main juste au-dessus du centre : on la prend côté joueur
            grabDir.Normalize();
            PlayerRig.Buzz(hand, 0.3f);
        }

        void Update()
        {
            if (!hand) return;
            var now = hand.position;
            // Le déplacement de la main le long du bord (la tangente au point tenu), converti en degrés de roue
            var tangent = Vector3.Cross(Axis, grabDir);
            float degrees = Vector3.Dot(now - lastHand, tangent) / lever * Mathf.Rad2Deg;
            wheel.Drag(degrees);
            grabDir = Quaternion.AngleAxis(degrees, Axis) * grabDir;
            lastHand = now;
        }

        void OnRelease(SelectExitEventArgs args)
        {
            if (args.interactorObject.transform != hand) return;
            bool launched = wheel.EndDrag();
            PlayerRig.Buzz(hand, launched ? 0.7f : 0.2f, launched ? 0.12f : 0.05f);
            hand = null;
        }

        // --- PC ---

        public bool BeginMouse() => Begin();
        public void MouseDrag(float pixels) => wheel.Drag(-pixels * mouseDegrees);   // glisser vers la droite = le bord côté joueur part à droite
        public void EndMouse() => wheel.EndDrag();

        // --- Commun ---

        bool Begin()
        {
            if (!Tutorial.Allows(TutorialAction.Other)) { Tutorial.Refuse(); return false; }   // comme les boutons pendant le tutoriel
            return wheel.BeginDrag();
        }
    }
}
