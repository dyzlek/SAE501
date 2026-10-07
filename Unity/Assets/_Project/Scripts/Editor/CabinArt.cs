using System.IO;
using UnityEditor;
using UnityEngine;

namespace SAE
{
    // La texture de bois des meubles du hub (pupitres, bibliothèque, socles, cadres), fabriquée par le code :
    // des veines dessinées pixel par pixel avec du bruit de Perlin, enregistrées en PNG dans Art/Cabane,
    // puis un matériau URP Lit qui l'utilise. Elle est claire : la teinte vient de ColorTint, meuble par meuble.
    // La cabane elle-même (rondins, toit, tapis…) est un modèle Blender : voir Blender/cabane.py.
    // Appelé par PrototypeGenerator ; refait à chaque génération.
    public static class CabinArt
    {
        const string Folder = "Assets/_Project/Art/Cabane";

        public static Material Furniture;

        public static void Build()
        {
            Directory.CreateDirectory(Folder);
            var tex = SaveTexture("Meubles", GrainTexture(512, 512, 51));
            string path = $"{Folder}/Meubles.mat";
            Furniture = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!Furniture)
            {
                Furniture = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(Furniture, path);
            }
            Furniture.SetTexture("_BaseMap", tex);
            Furniture.SetColor("_BaseColor", Color.white);
            Furniture.SetFloat("_Smoothness", 0.3f);
            Furniture.enableInstancing = true;
            EditorUtility.SetDirty(Furniture);
            AssetDatabase.SaveAssets();
        }

        // Bois clair : seulement les veines (la couleur du meuble vient de ColorTint)
        static Texture2D GrainTexture(int w, int h, float offset)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGB24, true);
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    float wave = Mathf.PerlinNoise(x * 0.02f + offset, y * 0.004f) * 12f;
                    float lines = 0.5f + 0.5f * Mathf.Sin((x * 0.25f + wave) * 1.3f);
                    float blotch = Mathf.PerlinNoise(x * 0.01f + offset, y * 0.01f + offset);
                    float v = 0.8f + 0.12f * lines + 0.15f * blotch;
                    tex.SetPixel(x, y, new Color(v, v, v));
                }
            tex.Apply();
            return tex;
        }

        static Texture2D SaveTexture(string name, Texture2D tex)
        {
            string path = $"{Folder}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.maxTextureSize = 512;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
