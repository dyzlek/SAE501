using System.IO;
using UnityEditor;
using UnityEngine;

namespace SAE
{
    // Menu SAE → Optimiser les textures (Quest), #176.
    // Les modèles de Blender gardent leur texture DANS le FBX (c'est pour ça qu'un singe de 700 triangles pèse 1,9 Mo).
    // Unity ne laisse pas régler la taille d'une texture intégrée : on la sort du FBX (comme le bouton « Extract Textures »
    // de l'Inspector), dans <Sujet>/Textures/Extraites, et le FBX s'en sert à la place.
    // Ensuite, sur Android seulement (le casque), chaque texture du dossier Art est limitée à 1024 px (512 pour les
    // projectiles, tout petits à l'écran). Dans l'éditeur et sur PC, rien ne change.
    public static class TextureMenu
    {
        const string ArtFolder = "Assets/_Project/Art";
        const int MaxSize = 1024, ProjectileMaxSize = 512;
        static readonly string[] ProjectileWords = { "Dart", "Projectile", "Bullet", "Tack", "Goutte", "BouleNeige", "Bomb", "Arrow" };

        [MenuItem("SAE/Optimiser les textures (Quest)")]
        static void Optimize()
        {
            int extracted = 0, limited = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ArtFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is ModelImporter model)) continue;
                var folder = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(path)), "Textures", "Extraites").Replace('\\', '/');
                Directory.CreateDirectory(folder);
                if (model.ExtractTextures(folder)) extracted++;
                if (Directory.GetFileSystemEntries(folder).Length == 0) Directory.Delete(folder);   // rien d'intégré dans ce FBX
            }
            AssetDatabase.Refresh();

            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter texture)) continue;
                int size = IsProjectile(path) ? ProjectileMaxSize : MaxSize;
                var android = texture.GetPlatformTextureSettings("Android");
                if (android.overridden && android.maxTextureSize <= size) continue;
                android.overridden = true;
                android.maxTextureSize = size;
                android.format = TextureImporterFormat.Automatic;   // ASTC sur le Quest
                texture.SetPlatformTextureSettings(android);
                texture.mipmapEnabled = true;                       // de loin, une plus petite version de la texture
                texture.SaveAndReimport();
                limited++;
            }
            Debug.Log($"Textures : {extracted} FBX vidés de leurs textures intégrées, {limited} textures limitées sur Android.");
        }

        static bool IsProjectile(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            foreach (var word in ProjectileWords)
                if (name.Contains(word)) return true;
            return false;
        }
    }
}
