using UnityEngine;
using UnityEngine.InputSystem;

namespace SAE
{
    // Une main de Quincy (prefab Main_Gauche / Main_Droite) posée sur une manette, qui se ferme selon les boutons,
    // comme le script AnimateHandOnInput du cours (support 3) : le grip plie les 4 doigts, la gâchette plie le pouce.
    // La main qui tient l'arc reste fermée tant que l'arc est sorti.
    // Chaque phalange tourne autour de l'axe « côté de la main » (perpendiculaire aux doigts et au dos de la main),
    // relevé au lancement : ça marche pour les deux mains, la droite étant la gauche en miroir.
    public class AnimateHandOnInput : MonoBehaviour
    {
        public InputActionProperty gripValue;      // XRI Left|Right Interaction/Select Value
        public InputActionProperty triggerValue;   // XRI Left|Right Interaction/Activate Value
        public Transform palm;                     // la paume (os « Paume ») et son bout (os « Dos ») : donnent l'axe de pliage
        public Transform back;
        public Transform[] fingerBones;            // les 2 phalanges des 4 doigts
        public Transform[] thumbBones;             // les 2 phalanges du pouce
        public Bow heldBow;                        // l'arc tenu par cette main (main gauche), sinon vide

        public float fingersCurl = 75f;            // pliage maximum de chaque phalange, en degrés
        public float thumbCurl = 40f;

        Quaternion[] fingerRest, thumbRest;

        void Awake()
        {
            fingerRest = System.Array.ConvertAll(fingerBones, b => b.localRotation);
            thumbRest = System.Array.ConvertAll(thumbBones, b => b.localRotation);
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
            // Les doigts se replient vers la paume, c'est-à-dire à l'opposé du dos de la main
            var towardPalm = palm.position - back.position;
            for (int i = 0; i < fingerBones.Length; i++) Curl(fingerBones[i], fingerRest[i], towardPalm, grip * fingersCurl);
            for (int i = 0; i < thumbBones.Length; i++) Curl(thumbBones[i], thumbRest[i], towardPalm, trigger * thumbCurl);
        }

        // Remet la phalange au repos puis la tourne de 'angle' degrés pour que son bout aille vers la paume.
        static void Curl(Transform bone, Quaternion rest, Vector3 towardPalm, float angle)
        {
            bone.localRotation = rest;
            var along = bone.GetChild(0).position - bone.position;   // le sens de la phalange (vers l'os suivant)
            var axis = Vector3.Cross(along, towardPalm);
            if (axis.sqrMagnitude > 1e-8f) bone.Rotate(axis.normalized, angle, Space.World);
        }

        static float Read(InputActionProperty input) => input.action != null ? input.action.ReadValue<float>() : 0f;
    }
}
