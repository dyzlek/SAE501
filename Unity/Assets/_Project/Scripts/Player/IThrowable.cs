using UnityEngine;

namespace SAE
{
    // Ce qu'on peut prendre puis LANCER (singe récolteur, fléchette), en VR comme en mode PC :
    // en VR, c'est le XR Grab qui appelle Grab() et Throw(vitesse de la main) ;
    // en mode PC, c'est DesktopPlayer (clic maintenu : prendre ; relâcher : lancer droit devant).
    public interface IThrowable
    {
        void Grab();
        void Throw(Vector3 velocity);
    }
}
