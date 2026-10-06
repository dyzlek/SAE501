using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SAE
{
    // Appuyer sur un bouton (ou ouvrir le coffre) DE LOIN : on le vise avec le rayon de la manette et on serre le grip,
    // comme pour attraper un objet. XR Simple Interactable = un objet qu'on peut viser et « sélectionner » sans le prendre.
    // Quand le rayon le survole, il grossit un peu (on sait ce qu'on vise) ; à l'appui, la manette vibre.
    // On peut toujours aussi l'enfoncer en le touchant avec le bout de la manette (HandPress).
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class RayPress : MonoBehaviour
    {
        public float hoverScale = 1.1f;   // grossissement quand on le vise

        XRSimpleInteractable interactable;
        IPressable target;
        Vector3 baseScale;

        void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            target = GetComponent<IPressable>();
            baseScale = transform.localScale;
        }

        void OnEnable()
        {
            interactable.hoverEntered.AddListener(OnHoverEntered);
            interactable.hoverExited.AddListener(OnHoverExited);
            interactable.selectEntered.AddListener(OnSelected);
        }

        void OnDisable()
        {
            interactable.hoverEntered.RemoveListener(OnHoverEntered);
            interactable.hoverExited.RemoveListener(OnHoverExited);
            interactable.selectEntered.RemoveListener(OnSelected);
        }

        void OnHoverEntered(HoverEnterEventArgs args)
        {
            transform.localScale = baseScale * hoverScale;
            PlayerRig.Buzz(args.interactorObject.transform, 0.1f, 0.03f);   // petit « tic » : c'est visé
        }

        void OnHoverExited(HoverExitEventArgs args)
        {
            if (!interactable.isHovered) transform.localScale = baseScale;   // plus aucune main ne le vise
        }

        void OnSelected(SelectEnterEventArgs args)
        {
            target?.Press();
            PlayerRig.Buzz(args.interactorObject.transform, 0.6f);
        }
    }
}
