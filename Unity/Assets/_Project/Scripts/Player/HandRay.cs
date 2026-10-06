using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace SAE
{
    // Le rayon lumineux devant chaque manette : il part de l'interacteur Near-Far (celui qui vise et attrape de loin)
    // et s'arrête sur ce qu'il touche. Bleu quand il vise quelque chose d'utilisable (bouton, banane, singe), blanc sinon.
    // On le dessine nous-mêmes : celui des Starter Assets était courbé, très court et presque invisible.
    // Caché quand la main tient déjà quelque chose (le singe tenu dessine son propre rayon de pose).
    [RequireComponent(typeof(LineRenderer))]
    public class HandRay : MonoBehaviour
    {
        public NearFarInteractor interactor;
        public float length = 8f;   // en mètres, quand il ne touche rien

        static readonly Color Free = new Color(1f, 1f, 1f, 0.6f);
        static readonly Color OnTarget = new Color(0.3f, 0.75f, 1f, 1f);

        LineRenderer line;

        void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = 0.006f;
            line.endWidth = 0.003f;
            line.sharedMaterial = Visuals.LineMaterial;
        }

        void LateUpdate()
        {
            // Pas de rayon si l'interacteur est coupé (pendant la visée de téléportation) ou s'il tient déjà quelque chose
            bool show = interactor && interactor.isActiveAndEnabled && !interactor.hasSelection;
            line.enabled = show;
            if (!show) return;

            var origin = interactor.transform;
            var end = origin.position + origin.forward * length;
            if (Physics.Raycast(origin.position, origin.forward, out var hit, length, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                end = hit.point;

            line.SetPosition(0, origin.position);
            line.SetPosition(1, end);
            line.startColor = line.endColor = interactor.hasHover ? OnTarget : Free;
        }
    }
}
