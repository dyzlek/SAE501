using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SAE.EditorTools
{
    // Menu SAE → Préparer les mains : fabrique Prefabs/Main_Gauche.prefab et Prefabs/Main_Droite.prefab
    // à partir de la main de Quincy (Art/Quincy/FBX/Quincy_Main.fbx, découpée dans le personnage par decouper_main.py).
    // Dans chaque prefab : le poignet à l'origine, les doigts vers +Z (vers l'avant de la manette), le dos de la
    // main vers +Y (on voit le dos de la main, comme sa propre main) ; la main droite est la gauche en miroir
    // (échelle X = -1). Réduite à HandScale : le gant de Quincy est fait pour un singe vu de loin, pas pour le casque.
    public static class HandSetup
    {
        const string HandModel = "Assets/_Project/Art/Quincy/FBX/Quincy_Main.fbx";
        const string LeftPath = "Assets/_Project/Prefabs/Main_Gauche.prefab";
        const string RightPath = "Assets/_Project/Prefabs/Main_Droite.prefab";
        const float HandScale = 0.6f;

        [MenuItem("SAE/Préparer les mains")]
        public static void Setup()
        {
            // La main utilise la texture de Quincy (comme le reste de ses modèles)
            var importer = (ModelImporter)AssetImporter.GetAtPath(HandModel);
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Quincy/Materiaux/Quincy_Mat.mat");
            if (importer.GetExternalObjectMap().Count == 0)
            {
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Quincy"), mat);
                importer.SaveAndReimport();
            }
            Build(LeftPath, mirror: false);
            Build(RightPath, mirror: true);
        }

        public static AnimateHandOnInput Left => AssetDatabase.LoadAssetAtPath<AnimateHandOnInput>(LeftPath);
        public static AnimateHandOnInput Right => AssetDatabase.LoadAssetAtPath<AnimateHandOnInput>(RightPath);

        static void Build(string path, bool mirror)
        {
            var root = new GameObject(mirror ? "Main droite" : "Main gauche");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HandModel));
            model.name = "Modele";
            model.transform.SetParent(root.transform, false);

            var wrist = Find(model, "Poignet");
            var fingers = Find(model, "Doigts");
            var thumb = Find(model, "Pouce");
            // On tourne le modèle pour que les doigts aillent vers +Z et le dos de la main vers +Y
            // (dans le modèle, le bras est tendu à l'horizontale, dos de la main vers le haut), puis le poignet à l'origine
            var fingerDir = Find(model, "Doigts_Bout").position - wrist.position;
            model.transform.rotation = Quaternion.Inverse(Quaternion.LookRotation(fingerDir, Vector3.up)) * model.transform.rotation;
            model.transform.position -= wrist.position;
            root.transform.localScale = new Vector3(mirror ? -HandScale : HandScale, HandScale, HandScale);

            var hand = root.AddComponent<AnimateHandOnInput>();
            hand.fingers = fingers;
            hand.fingersTip = Find(model, "Doigts_Bout");
            hand.thumb = thumb;
            hand.thumbTip = Find(model, "Pouce_Bout");
            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>()) r.updateWhenOffscreen = true;

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        static Transform Find(GameObject root, string name) =>
            root.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
    }
}
