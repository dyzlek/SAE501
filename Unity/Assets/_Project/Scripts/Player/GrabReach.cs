using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace SAE
{
    // Portée de saisie d'un objet qu'on prend (singe, banane) : on peut l'attraper de loin, mais pas plus loin que Reach.
    // En VR : c'est un filtre XRI posé sur le XR Grab Interactable. Au-delà de Reach, le rayon ne le survole pas
    // (il reste blanc) et le grip ne le prend pas. Les boutons, eux, gardent leur grande portée.
    // En mode PC : DesktopPlayer utilise la même constante Reach pour le clic.
    public class GrabReach : MonoBehaviour, IXRHoverFilter, IXRSelectFilter
    {
        public const float Reach = 6f;   // en mètres, de la main au bord de l'objet : du centre du hub (cercle de 5 m), on atteint les bibliothèques

        XRBaseInteractable interactable;

        void Awake()
        {
            interactable = GetComponent<XRBaseInteractable>();
            if (!interactable) return;   // pas de XR Grab (projet sans XR) : rien à filtrer
            interactable.hoverFilters.Add(this);
            interactable.selectFilters.Add(this);
        }

        void OnDestroy()
        {
            if (!interactable) return;
            interactable.hoverFilters.Remove(this);
            interactable.selectFilters.Remove(this);
        }

        public bool canProcess => isActiveAndEnabled;

        public bool Process(IXRHoverInteractor interactor, IXRHoverInteractable target) => InReach(interactor.transform);

        // Déjà en main : on ne le lâche pas parce qu'il est loin (pris de loin, il vient dans la main).
        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable target) =>
            interactor.IsSelecting(target) || InReach(interactor.transform);

        // Distance entre la main et le point le plus proche des colliders de l'objet
        bool InReach(Transform hand) =>
            interactable.GetDistance(hand.position).distanceSqr <= Reach * Reach;
    }
}
