using System.IO;
using UnityEditor;
using UnityEngine;

namespace SAE.EditorTools
{
    // Menu SAE → Brancher les modèles des ballons : remplit Assets/_Project/Resources/BalloonVisuals.asset
    // avec le FBX de chaque sorte de ballon. À relancer si on ajoute ou renomme un modèle.
    public static class BalloonVisualsSetup
    {
        const string AssetPath = "Assets/_Project/Resources/BalloonVisuals.asset";

        // Un FBX par sorte, dans l'ordre de BalloonKind (Normal, Rapide, Blindé, Boss, Dirigeable).
        // Le boss est le MOAB (bleu), le dirigeable rouge de la vague 10 est le BFB.
        static readonly string[] ModelPaths =
        {
            "Assets/_Project/Art/Ballons/FBX/Ballon_Normal.fbx",
            "Assets/_Project/Art/Ballons/FBX/Ballon_Normal.fbx",
            "Assets/_Project/Art/Ballons/FBX/Ballon_Blindage.fbx",
            "Assets/_Project/Art/MOAB/FBX/MOAB.fbx",
            "Assets/_Project/Art/BFB/FBX/BFB.fbx",
        };

        [MenuItem("SAE/Brancher les modèles des ballons")]
        public static void Setup()
        {
            var visuals = AssetDatabase.LoadAssetAtPath<BalloonVisuals>(AssetPath);
            if (!visuals)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
                visuals = ScriptableObject.CreateInstance<BalloonVisuals>();
                AssetDatabase.CreateAsset(visuals, AssetPath);
            }

            visuals.models = new GameObject[ModelPaths.Length];
            for (int i = 0; i < ModelPaths.Length; i++)
            {
                visuals.models[i] = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPaths[i]);
                if (!visuals.models[i]) Debug.LogWarning($"Ballons : modèle introuvable pour {(BalloonKind)i} ({ModelPaths[i]}), il restera en sphère.");
            }

            EditorUtility.SetDirty(visuals);
            AssetDatabase.SaveAssets();
            Debug.Log("Ballons : modèles branchés.");
        }
    }
}
