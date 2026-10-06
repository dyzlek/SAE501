using System.IO;
using UnityEditor;
using UnityEngine;

namespace SAE.EditorTools
{
    // Menu SAE → Brancher les modèles des singes : remplit Assets/_Project/Resources/MonkeyVisuals.asset
    // avec le FBX de chaque type de singe et le prefab de l'aura.
    // À relancer si on ajoute ou renomme un modèle.
    public static class MonkeyVisualsSetup
    {
        const string AssetPath = "Assets/_Project/Resources/MonkeyVisuals.asset";
        const string AuraPrefabPath = "Assets/_Project/Prefabs/Aura.prefab";

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
            visuals.auraPrefab = AssetDatabase.LoadAssetAtPath<Aura>(AuraPrefabPath);
            if (!visuals.auraPrefab) Debug.LogWarning($"Singes : prefab de l'aura introuvable ({AuraPrefabPath}).");

            EditorUtility.SetDirty(visuals);
            AssetDatabase.SaveAssets();
            Debug.Log("Singes : modèles et aura branchés.");
        }

        // Menu SAE → Mettre à jour les singes de la bibliothèque : remplace, dans la scène ouverte, le singe de
        // chaque case par la version à jour (modèle 3D), pour le voir aussi hors Play.
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
                Visuals.MonkeyPiece(new Monkey(slot.type, slot.level), slot.transform, Vector3.zero, size);
                EditorUtility.SetDirty(slot);
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
    }
}
