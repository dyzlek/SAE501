using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SAE
{
    // Réglages de rendu du joueur, posés par le code (PlayerRig l'ajoute) : rien à régler à la main dans les scènes.
    //   - Partout : la caméra ne voit pas plus loin que FarClip (#184). La carte et le hub font quelques dizaines de mètres ;
    //     250 m couvre encore les nuages de l'orage (Weather, qui arrivent de 150 m) et la fin du brouillard (230 m).
    //   - Au casque (Android) seulement :
    //       * pas de post-traitement (#187) : tons, couleurs et balance des blancs obligent le Quest à dessiner l'image
    //         dans une texture à part, puis à la recopier en plein écran pour les deux yeux. C'est cher pour son GPU,
    //         et ça empêche la fovéation de marcher ;
    //       * fovéation (#175) : les bords de l'image, que l'œil voit flou, sont calculés en plus basse résolution.
    //         On utilise la fonction « Meta XR Foveation », déjà cochée dans OpenXR (Android) ; il manquait le niveau.
    //         Dynamique = le casque ne l'active que quand une image risque d'être en retard.
    public class HeadsetRendering : MonoBehaviour
    {
        public const float FarClip = 250f;   // en mètres (avant : 400 m dans les scènes, 1 000 m dans le prefab XRI)
        const OVRManager.FoveatedRenderingLevel Foveation = OVRManager.FoveatedRenderingLevel.High;

        IEnumerator Start()
        {
            var eye = GetComponentInChildren<Camera>();
            if (eye) eye.farClipPlane = FarClip;
            if (Application.platform != RuntimePlatform.Android) yield break;

            if (eye) eye.GetUniversalAdditionalCameraData().renderPostProcessing = false;

            // Une image d'attente : la session OpenXR doit être ouverte pour recevoir le niveau de fovéation
            yield return null;
            OVRManager.foveatedRenderingLevel = Foveation;
            OVRManager.useDynamicFoveatedRendering = true;
        }
    }
}
