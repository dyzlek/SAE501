using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SAE
{
    // Posé sur le volume de lumière de chaque scène (réglages de l'image : tons, couleurs, halo).
    // Sur le casque (Android), on coupe le halo lumineux (Bloom) : c'est l'effet le plus coûteux,
    // et il faut tenir 72 images par seconde. Les tons et les couleurs, eux, ne coûtent presque rien.
    [RequireComponent(typeof(Volume))]
    public class MobileLighting : MonoBehaviour
    {
        void Awake()
        {
            if (Application.platform != RuntimePlatform.Android) return;
            var profile = GetComponent<Volume>().profile;   // une copie pour cette scène : le fichier n'est pas modifié
            if (profile.TryGet<Bloom>(out var bloom)) bloom.active = false;
        }
    }
}
