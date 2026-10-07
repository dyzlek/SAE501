using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SAE.EditorTools
{
    // Menu SAE → Préparer l'arc : fabrique les deux prefabs de l'arme, à partir des modèles de Quincy.
    //   Prefabs/Fleche.prefab : la flèche (Arrow + Rigidbody + collider en trigger), l'origine à l'encoche, la pointe vers +Z ;
    //   Prefabs/Arc.prefab    : l'arc (Bow) avec ses os (corde, branches, poignée) et la flèche à tirer.
    // Le générateur de scène l'appelle pour mettre l'arc dans les mains du joueur. On peut le relancer sans risque.
    public static class BowSetup
    {
        const string BowModel = "Assets/_Project/Art/Quincy/FBX/Quincy_Bow.fbx";
        const string ArrowModel = "Assets/_Project/Art/Quincy/FBX/Quincy_Arrow.fbx";
        const string ArrowPrefabPath = "Assets/_Project/Prefabs/Fleche.prefab";
        const string BowPrefabPath = "Assets/_Project/Prefabs/Arc.prefab";
        const float BowScale = 0.55f;    // l'arc de Quincy fait 1,2 m : réduit à environ 65 cm, pour qu'il ne cache pas la vue au casque
        const float ArrowScale = 0.75f;  // la flèche (0,81 m) réduite à environ 60 cm, à la taille de l'arc

        [MenuItem("SAE/Préparer l'arc")]
        public static Bow Setup()
        {
            var arrow = BuildArrow();
            return BuildBow(arrow);
        }

        static Arrow BuildArrow()
        {
            var root = new GameObject("Fleche");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ArrowModel));
            model.name = "Modele";
            model.transform.SetParent(root.transform, false);
            model.transform.localScale *= ArrowScale;
            // Dans le modèle, la flèche est déjà le long de +Z, pointe devant (les plumes, plus larges, sont derrière)
            var b = Bounds(model);
            model.transform.localPosition = new Vector3(-b.center.x, -b.center.y, -b.min.z);   // encoche à l'origine
            float length = b.size.z;

            var col = root.AddComponent<CapsuleCollider>();
            col.isTrigger = true;
            col.direction = 2;   // axe Z
            col.radius = 0.03f;
            col.height = length;
            col.center = new Vector3(0, 0, length / 2f);
            root.AddComponent<Rigidbody>().mass = 0.05f;
            root.AddComponent<Arrow>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ArrowPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<Arrow>();
        }

        static Bow BuildBow(Arrow arrowPrefab)
        {
            var root = new GameObject("Arc");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BowModel));
            model.name = "Modele";
            model.transform.SetParent(root.transform, false);
            root.transform.localScale = Vector3.one * BowScale;

            var bow = root.AddComponent<Bow>();
            bow.stringBone = Find(model.transform, "String");
            bow.limbTop = Find(model.transform, "LimbTop");
            bow.limbBottom = Find(model.transform, "LimbBottom");
            bow.grip = Find(model.transform, "Grip");
            bow.arrowPrefab = arrowPrefab;
            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>()) r.updateWhenOffscreen = true;   // la corde bouge : ses limites aussi

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, BowPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<Bow>();
        }

        static Transform Find(Transform root, string name) =>
            root.GetComponentsInChildren<Transform>(true).First(t => t.name == name);

        static Bounds Bounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
