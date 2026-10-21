using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Aura « super saiyan » autour d'un singe (de la couleur de sa rareté) ou d'une banane dorée.
    // OPTIMISATION (21 oct., voir docs/opti.md) : avant, chaque aura était le prefab Aura.prefab avec DEUX systèmes de
    // particules (jusqu'à 86 particules, 74 nouvelles par seconde, simulées à chaque image). Avec ~60 singes visibles
    // (bibliothèque, plateau, carte), c'était des milliers de particules et ~120 systèmes pour le Quest.
    // Maintenant, une aura est UN seul maillage fixe (un seul appel de dessin), sans particule :
    //   - des flammes : un tube ouvert autour du singe, plus étroit en haut, découpé en langues de hauteurs différentes,
    //     coloré en bas et transparent en haut (dégradé dans les couleurs des sommets) ;
    //   - un cœur : un tube plus serré et plus court, presque blanc, qui donne l'éclat « super saiyan » ;
    //   - un disque lumineux au sol, sous ses pieds.
    // Elle tourne lentement sur elle-même (les langues de flamme semblent bouger) et « respire » (petit battement) :
    // deux lignes par image, pas de simulation.
    // Le maillage est partagé entre toutes les auras de même couleur (fait une fois, gardé dans un dictionnaire).
    public class Aura : MonoBehaviour
    {
        public const float MinRadius = 0.38f;   // rayon minimum, en part de la hauteur (un singe fin garde une aura serrée)

        const int Sides = 24;                   // côtés du tube : assez pour paraître rond
        const float HeightFactor = 1.2f;        // les flammes dépassent un peu le haut du singe
        const float PulseSpeed = 5f, PulseSize = 0.07f;
        const float SpinSpeed = 35f;            // degrés par seconde : les langues de flamme tournent doucement

        // Hauteur de chaque langue de flamme (en part de la hauteur), répétée tout autour : irrégulier, comme un feu
        static readonly float[] Tongues = { 1f, 0.62f, 0.85f, 0.55f, 0.95f, 0.7f, 0.5f, 0.8f };

        static readonly Dictionary<Color, Mesh> meshes = new Dictionary<Color, Mesh>();
        static Mesh rainbowMesh;

        // Sans rechargement du domaine (Enter Play Mode rapide), les maillages de la partie d'avant sont détruits
        // mais le dictionnaire les gardait : c'est ce qui cachait toutes les auras sauf l'arc-en-ciel. On repart de zéro.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() { meshes.Clear(); rainbowMesh = null; }

        Vector3 baseScale;
        float phase;

        // Pose une aura sur target. bodySize = taille du singe en mètres (largeur, hauteur, profondeur).
        // Le singe est centré sur target, donc ses pieds sont à -hauteur/2.
        public static Aura Add(GameObject target, Rarity level, Vector3 bodySize) =>
            Add(target, MonkeyData.RarityColor(level), MonkeyData.IsRainbow(level), bodySize);

        // Même aura, d'une couleur choisie (ex. la banane dorée)
        public static Aura Add(GameObject target, ParticleSystem.MinMaxGradient color, Vector3 bodySize) =>
            Add(target, color.color, false, bodySize);

        static Aura Add(GameObject target, Color color, bool rainbow, Vector3 bodySize)
        {
            var go = new GameObject("Aura");
            go.transform.SetParent(target.transform, false);
            float height = bodySize.y;
            float radius = Mathf.Max(MinRadius * height, Mathf.Max(bodySize.x, bodySize.z) / 2f);
            go.transform.localPosition = Vector3.down * height / 2f;   // aux pieds
            go.transform.localRotation = Quaternion.Euler(0f, Random.value * 360f, 0f);   // toutes ne commencent pas pareil
            go.transform.localScale = new Vector3(radius, height * HeightFactor, radius);

            go.AddComponent<MeshFilter>().sharedMesh = rainbow ? RainbowMesh() : MeshOf(color);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = Visuals.LineMaterial;   // transparent, couleurs des sommets, vu des deux côtés (shader du jeu)
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;

            var aura = go.AddComponent<Aura>();
            aura.baseScale = go.transform.localScale;
            aura.phase = Random.value * 6.28f;   // les auras ne battent pas toutes en même temps
            return aura;
        }

        void Update()
        {
            float s = 1f + PulseSize * Mathf.Sin(Time.time * PulseSpeed + phase);
            transform.localScale = new Vector3(baseScale.x, baseScale.y * s, baseScale.z);
            transform.Rotate(0f, SpinSpeed * Time.deltaTime, 0f, Space.Self);
        }

        static Mesh MeshOf(Color color)
        {
            color.a = 1f;
            // « mesh == null » de Unity est vrai aussi pour un maillage détruit
            if (!meshes.TryGetValue(color, out var mesh) || mesh == null) meshes[color] = mesh = Build(i => color);
            return mesh;
        }

        // Arc-en-ciel : chaque côté a sa teinte
        static Mesh RainbowMesh() => rainbowMesh ? rainbowMesh : rainbowMesh = Build(i => Color.HSVToRGB(i / (float)Sides, 0.8f, 1f));

        // Le maillage d'une aura de 1 m de rayon et 1 m de haut (le transform le met à la taille du singe)
        static Mesh Build(System.Func<int, Color> colorOfSide)
        {
            var verts = new List<Vector3>();
            var colors = new List<Color>();
            var tris = new List<int>();

            // Les flammes : larges, colorées, en langues irrégulières
            Tube(verts, colors, tris, colorOfSide, 1f, 0.55f, 1f, 0.42f, 0f, 0);
            // Le cœur : serré, plus court, presque blanc (l'éclat au centre), langues décalées
            Tube(verts, colors, tris, colorOfSide, 0.7f, 0.45f, 0.6f, 0.35f, 0.65f, 3);

            // Le disque au sol : un centre lumineux, un bord qui s'efface (1 cm au-dessus du sol pour ne pas clignoter)
            int center = verts.Count;
            var mid = Color.Lerp(colorOfSide(0), Color.white, 0.4f);
            verts.Add(Vector3.up * 0.01f);
            colors.Add(new Color(mid.r, mid.g, mid.b, 0.6f));
            for (int i = 0; i <= Sides; i++)
            {
                float a = i * Mathf.PI * 2f / Sides;
                var c = colorOfSide(i % Sides);
                verts.Add(new Vector3(Mathf.Cos(a) * 1.4f, 0.01f, Mathf.Sin(a) * 1.4f));
                colors.Add(new Color(c.r, c.g, c.b, 0f));
                if (i > 0) tris.AddRange(new[] { center, center + i + 1, center + i });
            }

            var mesh = new Mesh { name = "Aura" };
            mesh.SetVertices(verts);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        // Un tube ouvert : rayon 'bottom' en bas, 'top' en haut, et chaque côté monte à height × sa langue de flamme.
        // alpha = opacité en bas (le haut est toujours transparent) ; white = de combien la couleur tire vers le blanc ;
        // shift = décalage dans la liste des langues (le cœur et les flammes n'ont pas leurs pointes au même endroit).
        static void Tube(List<Vector3> verts, List<Color> colors, List<int> tris, System.Func<int, Color> colorOfSide,
            float bottom, float top, float height, float alpha, float white, int shift)
        {
            int first = verts.Count;
            for (int i = 0; i <= Sides; i++)
            {
                float a = i * Mathf.PI * 2f / Sides;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                var c = Color.Lerp(colorOfSide(i % Sides), Color.white, white);
                float tongue = Tongues[(i % Sides + shift) % Tongues.Length];
                verts.Add(dir * bottom);
                colors.Add(new Color(c.r, c.g, c.b, alpha));
                verts.Add(dir * top + Vector3.up * height * tongue);
                colors.Add(new Color(c.r, c.g, c.b, 0f));
                if (i == Sides) continue;
                int b = first + i * 2;
                tris.AddRange(new[] { b, b + 1, b + 2, b + 1, b + 3, b + 2 });
            }
        }
    }
}
