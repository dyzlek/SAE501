using System.IO;
using UnityEditor;
using UnityEngine;

namespace SAE
{
    // Les matériaux de la cabane du hub, fabriqués par le code (pas de fichier à télécharger) :
    // des textures dessinées pixel par pixel (veines du bois avec du bruit de Perlin, planches, tapis à motifs),
    // enregistrées en PNG dans Art/Cabane, puis des matériaux URP Lit qui les utilisent.
    // Appelé par PrototypeGenerator ; refait à chaque génération (on peut donc changer une couleur ici et régénérer).
    public static class CabinArt
    {
        const string Folder = "Assets/_Project/Art/Cabane";

        public static Material Floor, Logs, Roof, Furniture, Rug, Glass, Glow, Iron;

        public static void Build()
        {
            Directory.CreateDirectory(Folder);
            var planks = SaveTexture("Plancher", PlanksTexture(512, 4, new Color(0.62f, 0.43f, 0.26f), 11));
            var logs = SaveTexture("Rondins", GrainTexture(256, 512, new Color(0.55f, 0.36f, 0.2f), 23));
            var roof = SaveTexture("Toit", PlanksTexture(512, 6, new Color(0.42f, 0.28f, 0.17f), 37));
            var furniture = SaveTexture("Meubles", GrainTexture(512, 512, Color.white, 51));   // clair : la couleur vient de ColorTint (teinte du meuble)
            var rug = SaveTexture("Tapis", RugTexture(1024));

            Floor = Lit("Plancher", planks, new Vector2(4f, 4f), 0.35f);
            Logs = Lit("Rondins", logs, new Vector2(1f, 3f), 0.15f);
            Roof = Lit("Toit", roof, new Vector2(3f, 3f), 0.2f);
            Furniture = Lit("Meubles", furniture, Vector2.one, 0.3f);
            Rug = Lit("Tapis", rug, Vector2.one, 0.05f);
            Rug.SetFloat("_AlphaClip", 1f);           // le tapis est rond : on découpe le carré de la texture
            Rug.SetFloat("_Cutoff", 0.5f);
            Rug.EnableKeyword("_ALPHATEST_ON");
            Glass = Emissive("Vitre", new Color(0.7f, 0.85f, 1f), 1.2f);     // fenêtre : un ciel clair qui brille
            Glow = Emissive("Flamme", new Color(1f, 0.75f, 0.4f), 2.5f);     // l'intérieur des lanternes
            Iron = Lit("Fer", null, Vector2.one, 0.5f);
            Iron.SetColor("_BaseColor", new Color(0.12f, 0.11f, 0.1f));
            Iron.SetFloat("_Metallic", 0.7f);
            AssetDatabase.SaveAssets();
        }

        // ---------- textures ----------

        // Planches côte à côte (dans le sens de la hauteur de l'image), chacune d'une teinte un peu différente,
        // avec des veines, des joints sombres entre elles et une coupure à une hauteur au hasard.
        static Texture2D PlanksTexture(int size, int count, Color baseColor, int seed)
        {
            var rnd = new System.Random(seed);
            var tex = new Texture2D(size, size, TextureFormat.RGB24, true);
            int width = size / count;
            for (int p = 0; p < count; p++)
            {
                float shade = 0.85f + (float)rnd.NextDouble() * 0.3f;
                int cut = rnd.Next(size);
                float offset = (float)rnd.NextDouble() * 100f;
                for (int x = p * width; x < (p + 1) * width; x++)
                    for (int y = 0; y < size; y++)
                    {
                        var c = baseColor * shade * Grain(x, y, offset);
                        int inX = x - p * width;
                        if (inX < 2 || inX >= width - 1 || Mathf.Abs(y - cut) < 2) c *= 0.45f;   // joints
                        tex.SetPixel(x, y, c);
                    }
            }
            tex.Apply();
            return tex;
        }

        // Bois sans planches (rondins, meubles) : seulement les veines
        static Texture2D GrainTexture(int w, int h, Color baseColor, int seed)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGB24, true);
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    tex.SetPixel(x, y, baseColor * Grain(x, y, seed));
            tex.Apply();
            return tex;
        }

        // Veines du bois : des lignes ondulées le long de la planche (axe y), plus quelques nœuds sombres
        static float Grain(float x, float y, float offset)
        {
            float wave = Mathf.PerlinNoise(x * 0.02f + offset, y * 0.004f) * 12f;
            float lines = 0.5f + 0.5f * Mathf.Sin((x * 0.25f + wave) * 1.3f);
            float blotch = Mathf.PerlinNoise(x * 0.01f + offset, y * 0.01f + offset);
            float knot = Mathf.PerlinNoise(x * 0.08f + offset * 2f, y * 0.03f) > 0.82f ? 0.75f : 1f;
            return (0.8f + 0.12f * lines + 0.15f * blotch) * knot;
        }

        // Tapis rond oriental : bordure dorée, frise de losanges, champ rouge, médaillon au centre.
        // Transparent hors du cercle (découpé par le matériau).
        static Texture2D RugTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            var gold = new Color(0.85f, 0.65f, 0.25f);
            var red = new Color(0.55f, 0.1f, 0.08f);
            var navy = new Color(0.12f, 0.15f, 0.3f);
            var cream = new Color(0.92f, 0.85f, 0.68f);
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    float u = x / (size - 1f) * 2f - 1f, v = y / (size - 1f) * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);
                    float a = Mathf.Atan2(v, u);
                    Color c;
                    if (r > 1f) c = Color.clear;
                    else if (r > 0.93f) c = gold;
                    else if (r > 0.9f) c = navy;
                    else if (r > 0.72f)
                    {
                        // frise : 24 losanges crème sur fond bleu nuit
                        float t = Mathf.Repeat(a / (Mathf.PI * 2f) * 24f, 1f) - 0.5f;
                        float k = (r - 0.81f) / 0.09f;
                        c = Mathf.Abs(t) * 2f + Mathf.Abs(k) < 0.9f ? cream : navy;
                    }
                    else if (r > 0.69f) c = gold;
                    else if (r > 0.3f)
                    {
                        // champ rouge avec une fleur à 8 pétales en filigrane
                        float petal = Mathf.Abs(Mathf.Cos(a * 4f)) * 0.25f + 0.35f;
                        c = Mathf.Abs(r - petal - 0.15f) < 0.015f ? gold : red;
                    }
                    else if (r > 0.27f) c = gold;
                    else c = Mathf.Abs(Mathf.Cos(a * 4f)) * 0.22f > r ? cream : navy;   // médaillon en étoile

                    // un peu de laine : petit bruit, et les bords usés
                    float wool = 0.9f + 0.1f * Mathf.PerlinNoise(x * 0.3f, y * 0.3f);
                    c = new Color(c.r * wool, c.g * wool, c.b * wool, c.a);
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            return tex;
        }

        // ---------- fichiers ----------

        static Texture2D SaveTexture(string name, Texture2D tex)
        {
            string path = $"{Folder}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material Lit(string name, Texture2D tex, Vector2 tiling, float smoothness)
        {
            string path = $"{Folder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_BaseMap", tex);
            mat.SetTextureScale("_BaseMap", tiling);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", smoothness);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material Emissive(string name, Color color, float intensity)
        {
            var mat = Lit(name, null, Vector2.one, 0.9f);
            mat.SetColor("_BaseColor", color);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * intensity);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            return mat;
        }
    }
}
