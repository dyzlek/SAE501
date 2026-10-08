using System.Collections;
using UnityEngine;

namespace SAE
{
    // Le voile noir des portes (PortalDoor) : il monte du bas pour cacher la vue, le joueur est téléporté à l'abri,
    // puis il redescend pour la découvrir (le haut réapparaît en premier).
    // C'est un carré noir collé devant la caméra du joueur, juste après sa distance minimale d'affichage (0,03 m),
    // dessiné par-dessus tout avec le shader des textes du jeu (_ZTest = Always) : il marche en VR comme au PC.
    public static class ScreenCurtain
    {
        const float Distance = 0.06f;   // en mètres devant l'œil
        const float Height = 0.2f;      // assez pour couvrir tout le champ de vision du casque (≈ 120°) à cette distance
        const float Width = 0.5f;       // plus large : les deux yeux, et le champ est plus large que haut

        static Transform curtain;

        // Le voile monte (cover = true) ou redescend (cover = false), en duration secondes
        public static IEnumerator Slide(bool cover, float duration)
        {
            var c = Get();
            if (!c) yield break;
            c.gameObject.SetActive(true);
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                Place(c, cover ? t : 1f - t);
                yield return null;
            }
            Place(c, cover ? 1f : 0f);
            if (!cover) c.gameObject.SetActive(false);
        }

        // 0 = tout en bas, hors de la vue ; 1 = la vue entièrement cachée
        static void Place(Transform c, float amount)
        {
            c.localPosition = new Vector3(0f, (Mathf.SmoothStep(0f, 1f, amount) - 1f) * Height, Distance);
        }

        // Le voile, créé au premier passage devant la tête du joueur actif (VR ou PC)
        static Transform Get()
        {
            var head = PlayerRig.Local ? PlayerRig.Local.head : null;
            if (!head) return null;
            if (curtain && curtain.parent == head) return curtain;
            if (curtain) Object.Destroy(curtain.gameObject);

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Voile noir";
            Object.Destroy(go.GetComponent<Collider>());
            var material = new Material(Shader.Find("SAE/Texte 3D")) { name = "Voile noir", renderQueue = 4000 };   // après tout le reste
            material.SetColor("_Color", Color.black);
            material.SetFloat("_VertexColor", 0f);
            material.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            curtain = go.transform;
            curtain.SetParent(head, false);
            curtain.localRotation = Quaternion.identity;
            curtain.localScale = new Vector3(Width, Height, 1f);
            go.SetActive(false);
            return curtain;
        }
    }
}
