using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Aura « super saiyan » autour d'un singe (de la couleur de sa rareté) ou d'une banane dorée.
    // OPTIMISATION (21 oct., voir docs/opti.md) : avant, chaque aura était le prefab Aura.prefab avec DEUX systèmes de
    // particules (jusqu'à 86 particules, 74 nouvelles par seconde, simulées à chaque image). Avec ~60 singes visibles
    // (bibliothèque, plateau, carte), c'était des milliers de particules et ~120 systèmes pour le Quest.
    // Maintenant, une aura est UN seul maillage fixe, sans particule :
    //   - une flamme : un tube ouvert qui entoure le singe, plus étroit en haut, au bord dentelé comme des flammes,
    //     opaque en bas et transparent en haut (dégradé dans les couleurs des sommets) ;
    //   - un disque lumineux au sol, sous ses pieds.
    // Elle « respire » (petit battement de hauteur) : une seule ligne par image, pas de simulation.
    // Le maillage est partagé entre toutes les auras de même couleur (fait une fois, gardé dans un dictionnaire).
    public class Aura : MonoBehaviour
    {
        public const float MinRadius = 0.38f;   // rayon minimum, en part de la hauteur (un singe fin garde une aura serrée)

        const int Sides = 20;                   // côtés du tube : assez pour paraître rond
        const float HeightFactor = 1.15f;       // la flamme dépasse un peu le haut du singe
        const float TopShrink = 0.6f;           // le haut du tube fait 60 % du rayon du bas
        const float BottomAlpha = 0.45f;
        const float DiscAlpha = 0.5f;
        const float PulseSpeed = 5f, PulseSize = 0.06f;

        static readonly Dictionary<Color, Mesh> meshes = new Dictionary<Color, Mesh>();
        static Mesh rainbowMesh;

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
        }

        static Mesh MeshOf(Color color)
        {
            color.a = 1f;
            if (!meshes.TryGetValue(color, out var mesh)) meshes[color] = mesh = Build(i => color);
            return mesh;
        }

        // Arc-en-ciel : chaque côté du tube a sa teinte
        static Mesh RainbowMesh() => rainbowMesh ? rainbowMesh : rainbowMesh = Build(i => Color.HSVToRGB(i / (float)Sides, 0.8f, 1f));

        // Le maillage d'une aura de 1 m de rayon et 1 m de haut (le transform le met à la taille du singe)
        static Mesh Build(System.Func<int, Color> colorOfSide)
        {
            var verts = new List<Vector3>();
            var colors = new List<Color>();
            var tris = new List<int>();

            // Le tube : un anneau en bas, un anneau en haut, dentelé (une pointe sur deux monte plus haut)
            for (int i = 0; i <= Sides; i++)
            {
                float a = i * Mathf.PI * 2f / Sides;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                var c = colorOfSide(i % Sides);
                float top = i % 2 == 0 ? 1f : 0.75f;
                verts.Add(dir);                                     colors.Add(new Color(c.r, c.g, c.b, BottomAlpha));
                verts.Add(dir * TopShrink + Vector3.up * top);      colors.Add(new Color(c.r, c.g, c.b, 0f));
                if (i == Sides) continue;
                int b = i * 2;
                tris.AddRange(new[] { b, b + 1, b + 2, b + 1, b + 3, b + 2 });
            }

            // Le disque au sol : un centre plein, un bord qui s'efface (légèrement au-dessus du sol pour ne pas clignoter)
            int center = verts.Count;
            var mid = colorOfSide(0);
            verts.Add(Vector3.up * 0.01f); colors.Add(new Color(mid.r, mid.g, mid.b, DiscAlpha));
            for (int i = 0; i <= Sides; i++)
            {
                float a = i * Mathf.PI * 2f / Sides;
                var c = colorOfSide(i % Sides);
                verts.Add(new Vector3(Mathf.Cos(a) * 1.3f, 0.01f, Mathf.Sin(a) * 1.3f)); colors.Add(new Color(c.r, c.g, c.b, 0f));
                if (i > 0) tris.AddRange(new[] { center, center + i + 1, center + i });
            }

            var mesh = new Mesh { name = "Aura" };
            mesh.SetVertices(verts);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
