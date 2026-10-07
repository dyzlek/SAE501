using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace SAE
{
    // Appuyer sur un bouton (ou ouvrir le coffre) DE LOIN : on le vise avec le rayon de la manette et on appuie
    // sur la gâchette (index) ou on serre le grip (majeur).
    // XR Simple Interactable = un objet qu'on peut viser et « sélectionner » (grip) sans le prendre.
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

        void OnSelected(SelectEnterEventArgs args) => Press(args.interactorObject.transform);

        // La gâchette : on regarde, pour chaque main qui vise ce bouton, si elle vient d'appuyer.
        void Update()
        {
            foreach (var interactor in interactable.interactorsHovering)
                if (interactor is XRBaseInputInteractor hand && hand.activateInput.ReadWasPerformedThisFrame())
                {
                    Press(hand.transform);
                    return;
                }
        }

        void Press(Transform hand)
        {
            target?.Press();
            PlayerRig.Buzz(hand, 0.6f);
            Sfx.Play(Sfx.Sound.Click, transform.position);
        }
    }
}
