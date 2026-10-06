using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SAE.EditorTools
{
    // Menu SAE → Brancher les modèles des singes : remplit Assets/_Project/Resources/MonkeyVisuals.asset
    // avec le FBX de chaque type de singe, et prépare le matériau des flammes de l'aura.
    // À relancer si on ajoute ou renomme un modèle.
    public static class MonkeyVisualsSetup
    {
        const string AssetPath = "Assets/_Project/Resources/MonkeyVisuals.asset";
        const string MaterialPath = "Assets/_Project/Art/Materials/Aura.mat";
        const string TexturePath = "Assets/_Project/Art/Materials/Aura_Flamme.png";

        // Un FBX par type, dans l'ordre de MonkeyType (Classique, Boomerang, Canon, Sniper, Punaise, Glace, Colle).
        static readonly string[] ModelPaths =
        {
            "Assets/_Project/Art/Singe_Base/FBX/SingeBase_Rigged.fbx",
            "Assets/_Project/Art/Boomerang/FBX/BoomerangMonkey_Rigged.fbx",
            "Assets/_Project/Art/Canon/FBX/Canon.fbx",
            "Assets/_Project/Art/Sniper/FBX/Sniper_Rigged.fbx",
            "Assets/_Project/Art/Tireur/FBX/Tireur.fbx",
            "Assets/_Project/Art/Glace/FBX/Glace_Rigged.fbx",
            "Assets/_Project/Art/Colle/FBX/Colle_Rigged.fbx",
        };

        // Les modèles ont été exportés de dos (ils regardent vers +Z) : on les retourne.
        // Le Tireur est symétrique, inutile de le tourner.
        static readonly float[] Yaw = { 180f, 180f, 180f, 180f, 0f, 180f, 180f };

        [MenuItem("SAE/Brancher les modèles des singes")]
        public static void Setup()
        {
            var visuals = AssetDatabase.LoadAssetAtPath<MonkeyVisuals>(AssetPath);
            if (!visuals)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
                visuals = ScriptableObject.CreateInstance<MonkeyVisuals>();
                AssetDatabase.CreateAsset(visuals, AssetPath);
            }

            visuals.models = new GameObject[MonkeyData.TypeCount];
            visuals.yaw = (float[])Yaw.Clone();
            for (int i = 0; i < ModelPaths.Length; i++)
            {
                visuals.models[i] = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPaths[i]);
                if (!visuals.models[i]) Debug.LogWarning($"Singes : modèle introuvable pour {(MonkeyType)i} ({ModelPaths[i]}), il restera en cube.");
            }
            visuals.auraMaterial = FlameMaterial();

            EditorUtility.SetDirty(visuals);
            AssetDatabase.SaveAssets();
            Debug.Log("Singes : modèles et aura branchés.");
        }

        // Menu SAE → Mettre à jour les singes de la bibliothèque : remplace, dans la scène ouverte, le singe de
        // chaque case par la version à jour (modèle 3D + aura), pour le voir aussi hors Play.
        // (En Play, chaque case le fait déjà toute seule au lancement.) Il faut ensuite enregistrer la scène.
        [MenuItem("SAE/Mettre à jour les singes de la bibliothèque")]
        public static void RefreshLibrary()
        {
            foreach (var slot in Object.FindObjectsByType<LibrarySlot>(FindObjectsSortMode.None))
            {
                float size = 0f;
                for (int i = slot.transform.childCount - 1; i >= 0; i--)
                {
                    var child = slot.transform.GetChild(i);
                    var corps = child.Find("Corps");
                    if (corps) size = corps.localScale.x;
                    Object.DestroyImmediate(child.gameObject);
                }
                Visuals.MonkeyPiece(new Monkey(slot.type, slot.level), slot.transform, Vector3.zero, size, withLabel: false);
                EditorUtility.SetDirty(slot);
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        // Matériau des flammes : particules additives (elles éclaircissent ce qu'il y a derrière, comme une lueur),
        // avec une texture de flamme douce. On réutilise Aura.mat s'il existe déjà, pour garder sa référence.
        static Material FlameMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (!mat)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, MaterialPath);
            }
            mat.shader = shader;
            mat.SetFloat("_Surface", 1f);                          // transparent
            mat.SetFloat("_Blend", 2f);                            // additif
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.One);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_BLENDMODE_ADD");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetTexture("_BaseMap", FlameTexture());
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // Texture d'une flamme : une goutte blanche aux bords doux, plus large en bas, pointue en haut.
        // Elle est blanche : c'est la couleur de la particule (la rareté) qui la colore.
        static Texture2D FlameTexture()
        {
            const int Size = 64;
            if (!File.Exists(TexturePath))
            {
                var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float v = y / (Size - 1f);                        // 0 en bas, 1 en haut
                    float halfWidth = 0.45f * Mathf.Sqrt(1f - v);     // la goutte s'affine vers le haut
                    float dx = Mathf.Abs(x / (Size - 1f) - 0.5f);
                    float side = halfWidth > 0f ? Mathf.Clamp01(1f - dx / halfWidth) : 0f;
                    float ends = Mathf.Clamp01(v * 6f);               // bas arrondi
                    float a = Mathf.SmoothStep(0f, 1f, side) * ends;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                File.WriteAllBytes(TexturePath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(TexturePath);

                var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        }
    }
}
