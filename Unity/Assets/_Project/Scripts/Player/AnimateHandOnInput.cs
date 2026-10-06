using UnityEngine;
using UnityEngine.InputSystem;

namespace SAE
{
    // Une main de Quincy (prefab Main_Gauche / Main_Droite) posée sur une manette, qui se ferme selon les boutons,
    // comme le script AnimateHandOnInput du cours (support 3) : le grip plie les doigts, la gâchette plie le pouce.
    // La main qui tient l'arc reste fermée tant que l'arc est sorti.
    // C'est la main simple de Quincy (un bloc pour les doigts, un pour le pouce). Les doigts tournent autour de
    // l'axe des articulations (celui qui va vers le pouce, perpendiculaire aux doigts), relevé au lancement dans
    // le repère de la main : ça marche aussi pour la main droite, qui est la main gauche en miroir.
    public class AnimateHandOnInput : MonoBehaviour
    {
        public InputActionProperty gripValue;      // XRI Left|Right Interaction/Select Value
        public InputActionProperty triggerValue;   // XRI Left|Right Interaction/Activate Value
        public Transform fingers, fingersTip;      // os des doigts et leur bout (direction des doigts)
        public Transform thumb, thumbTip;
        public Bow heldBow;                        // l'arc tenu par cette main (main gauche), sinon vide

        public float fingersCurl = 60f;            // pliage maximum, en degrés
        public float thumbCurl = 30f;

        Quaternion fingersRest, thumbRest;
        Vector3 curlAxis;                          // axe de pliage, dans le repère de la main

        void Awake()
        {
            fingersRest = fingers.localRotation;
            thumbRest = thumb.localRotation;
            // L'axe de pliage : la direction du pouce, sans sa part dans le sens des doigts (axe des articulations)
            var fingerDir = (fingersTip.position - fingers.position).normalized;
            var towardThumb = thumb.position - fingers.position;
            curlAxis = transform.InverseTransformDirection(Vector3.ProjectOnPlane(towardThumb, fingerDir).normalized);
        }

        void OnEnable()
        {
            gripValue.action?.Enable();
            triggerValue.action?.Enable();
        }

        void Update()
        {
            bool holding = heldBow && heldBow.isActiveAndEnabled;
            float grip = holding ? 1f : Read(gripValue);
            float trigger = holding ? 1f : Read(triggerValue);
            Curl(fingers, fingersRest, grip * fingersCurl);
            Curl(thumb, thumbRest, trigger * thumbCurl);
        }

        void Curl(Transform bone, Quaternion rest, float angle)
        {
            bone.localRotation = rest;
            bone.Rotate(transform.TransformDirection(curlAxis), angle, Space.World);
        }

        static float Read(InputActionProperty input) => input.action != null ? input.action.ReadValue<float>() : 0f;
    }
}
