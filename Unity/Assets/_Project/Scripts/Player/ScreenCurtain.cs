using System.Collections;
using UnityEngine;

namespace SAE
{
    // Le voile noir des portes (PortalDoor) : il descend du haut pour cacher la vue, le joueur est téléporté à l'abri,
    // puis il remonte pour la découvrir (le bas de l'image réapparaît en premier). Son bord est fondu, et il démarre
    // et s'arrête en douceur : rien de brusque dans le casque.
    // C'est un carré noir collé devant la caméra du joueur, juste après sa distance minimale d'affichage (0,03 m),
    // dessiné par-dessus tout avec le shader des textes du jeu (_ZTest = Always) : il marche en VR comme au PC.
    // Sa taille suit le champ de vision de la caméra, pour que le bord traverse toute l'image.
    public static class ScreenCurtain
    {
        const float Distance = 0.06f;   // en mètres devant l'œil
        const float Feather = 0.6f;     // le bord fondu, en part de la demi-hauteur de l'image

        static Transform curtain;
        static float hiddenY, coveredY;   // le voile au-dessus de l'image (caché), ou devant toute l'image

        // Le voile descend (cover = true) ou remonte (cover = false), en duration secondes
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

        // 0 = au-dessus de l'image, caché ; 1 = l'image entièrement noire. Départ et arrivée tout en douceur.
        static void Place(Transform c, float amount)
        {
            float k = amount * amount * amount * (amount * (amount * 6f - 15f) + 10f);
            c.localPosition = new Vector3(0f, Mathf.Lerp(hiddenY, coveredY, k), Distance);
        }

        // Le voile, créé au premier passage devant la tête du joueur actif (VR ou PC)
        static Transform Get()
        {
            var head = PlayerRig.Local ? PlayerRig.Local.head : null;
            if (!head) return null;
            if (curtain && curtain.parent == head) return curtain;
            if (curtain) Object.Destroy(curtain.gameObject);

            // La demi-hauteur de l'image à cette distance (un peu plus, pour ne rien laisser passer)
            var cam = head.GetComponent<Camera>();
            float fov = cam ? cam.fieldOfView : 100f;
            float half = Distance * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * 1.15f;
            float feather = half * Feather;
            float height = 2f * half + feather;                    // la partie noire, plus le bord fondu (en bas)
            hiddenY = half + height / 2f;                           // tout le voile au-dessus de l'image
            coveredY = height / 2f - half - feather;                // la partie noire devant toute l'image

            var go = new GameObject("Voile noir");
            go.AddComponent<MeshFilter>().sharedMesh = FadedQuad(feather / height);
            var material = new Material(Shader.Find("SAE/Texte 3D")) { name = "Voile noir", renderQueue = 4000 };   // après tout le reste
            material.SetColor("_Color", Color.black);
            material.SetFloat("_VertexColor", 1f);   // la transparence vient des sommets (le bord fondu)
            material.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            curtain = go.transform;
            curtain.SetParent(head, false);
            curtain.localRotation = Quaternion.identity;
            curtain.localScale = new Vector3(half * 6f, height, 1f);   // large : les deux yeux du casque, l'écran large du PC
            go.SetActive(false);
            return curtain;
        }

        // Un carré de 1 × 1 en bandes horizontales : transparent tout en bas, de plus en plus noir jusqu'à « edge »
        // (en part de la hauteur), puis noir jusqu'en haut. La couleur des sommets donne la transparence.
        static Mesh FadedQuad(float edge)
        {
            const int Steps = 8;   // les bandes du bord fondu
            var vertices = new System.Collections.Generic.List<Vector3>();
            var colors = new System.Collections.Generic.List<Color>();
            var triangles = new System.Collections.Generic.List<int>();
            for (int i = 0; i <= Steps + 1; i++)
            {
                float v = i <= Steps ? edge * i / Steps : 1f;
                float a = i <= Steps ? Mathf.SmoothStep(0f, 1f, (float)i / Steps) : 1f;
                vertices.Add(new Vector3(-0.5f, v - 0.5f, 0f));
                vertices.Add(new Vector3(0.5f, v - 0.5f, 0f));
                colors.Add(new Color(1f, 1f, 1f, a));
                colors.Add(new Color(1f, 1f, 1f, a));
                if (i > 0)
                {
                    int b = 2 * i - 2;
                    triangles.AddRange(new[] { b, b + 2, b + 1, b + 1, b + 2, b + 3 });
                }
            }
            var mesh = new Mesh { name = "Voile noir" };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
