using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SAE.EditorTools
{
    // Menu SAE → Préparer les mains : fabrique Prefabs/Main_Gauche.prefab et Prefabs/Main_Droite.prefab
    // à partir des gants de Quincy (Art/Quincy/FBX/Quincy_Main_Gauche|Droite.fbx, créés par creer_mains.py).
    // Dans chaque prefab : le poignet à l'origine, les doigts vers +Z (vers l'avant de la manette),
    // le dos de la main vers +Y. Le générateur pose les mains sur les manettes.
    public static class HandSetup
    {
        const string ModelFolder = "Assets/_Project/Art/Quincy/FBX/";
        const string LeftPath = "Assets/_Project/Prefabs/Main_Gauche.prefab";
        const string RightPath = "Assets/_Project/Prefabs/Main_Droite.prefab";
        static readonly string[] Fingers = { "Index", "Majeur", "Annulaire", "Auriculaire" };

        [MenuItem("SAE/Préparer les mains")]
        public static void Setup()
        {
            Build("Gauche", LeftPath);
            Build("Droite", RightPath);
        }

        public static AnimateHandOnInput Left => AssetDatabase.LoadAssetAtPath<AnimateHandOnInput>(LeftPath);
        public static AnimateHandOnInput Right => AssetDatabase.LoadAssetAtPath<AnimateHandOnInput>(RightPath);

        static void Build(string side, string path)
        {
            var root = new GameObject("Main " + side.ToLower());
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "Quincy_Main_" + side + ".fbx");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            model.name = "Modele";
            model.transform.SetParent(root.transform, false);

            // On tourne le modèle : les doigts (Paume -> Majeur_1) vers +Z, le dos de la main vers +Y, puis le poignet à l'origine
            var wrist = Find(model, "Poignet");
            var palm = Find(model, "Paume");
            var back = Find(model, "Dos");
            var forward = Find(model, "Majeur_1").position - palm.position;
            var up = back.position - palm.position;
            model.transform.rotation = Quaternion.Inverse(Quaternion.LookRotation(forward, up)) * model.transform.rotation;
            model.transform.position -= palm.position;

            var hand = root.AddComponent<AnimateHandOnInput>();
            hand.palm = palm;
            hand.back = back;
            hand.fingerBones = Fingers.SelectMany(f => new[] { Find(model, f + "_1"), Find(model, f + "_2") }).ToArray();
            hand.thumbBones = new[] { Find(model, "Pouce_1"), Find(model, "Pouce_2") };
            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>()) r.updateWhenOffscreen = true;

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        static Transform Find(GameObject root, string name) =>
            root.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
    }
}
