using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sae501.Coffres.EditorTools
{
    // Menu SAE501 : installe les packages VR et construit la scène de test du coffre en un clic.
    public static class Sae501SetupMenu
    {
        const string ChestModelPath = "Assets/_Project/Art/Chest/chest_cartoon_animations.glb";
        const string ScenePath = "Assets/_Project/Scenes/Sandbox/Nicolas/ChestSandbox.unity";
        const string FloorMaterialPath = "Assets/_Project/Art/Chest/Mat_Floor.mat";
        const string PlayerMaterialPath = "Assets/_Project/Art/Chest/Mat_Player.mat";

        [MenuItem("SAE501/1 - Installer les packages (glTFast, XR Toolkit, OpenXR)")]
        static void InstallPackages()
        {
            // Sans numéro de version : Unity prend la version recommandée pour cet éditeur.
            Client.AddAndRemove(new[]
            {
                "com.unity.cloud.gltfast",              // import du .glb
                "com.unity.xr.interaction.toolkit",     // interactions VR
                "com.unity.xr.openxr",                  // runtime VR
            });
            Debug.Log("SAE501 : installation des packages lancée (voir la barre de progression en bas à droite).");
        }

        [MenuItem("SAE501/2 - Créer la scène test du coffre")]
        static void BuildScene()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ChestModelPath);
            if (model == null)
            {
                EditorUtility.DisplayDialog("Modèle introuvable",
                    "Impossible de charger " + ChestModelPath + ".\nLe package glTFast est-il installé (menu 1) et l'import terminé ?", "OK");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Lumière
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Sol 30 m x 30 m (1 unité = 1 m ; un Plane fait 10 m de côté à l'échelle 1)
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Sol";
            floor.transform.localScale = new Vector3(3f, 1f, 3f);
            floor.GetComponent<Renderer>().sharedMaterial = GetMaterial(FloorMaterialPath, new Color(0.35f, 0.37f, 0.4f));

            // Coffre : on le ramène à ~0,8 m de large, posé sur le sol, centré sur l'origine.
            var chest = (GameObject)PrefabUtility.InstantiatePrefab(model);
            chest.name = "Coffre";
            var b = GetBounds(chest);
            chest.transform.localScale *= 0.8f / Mathf.Max(b.size.x, b.size.z);
            b = GetBounds(chest);
            chest.transform.position += new Vector3(-b.center.x, -b.min.y, -b.center.z);
            float chestTop = GetBounds(chest).max.y;

            // Systèmes
            var systems = new GameObject("Systemes");
            var wallet = systems.AddComponent<Wallet>();

            // Joueur : corps basique + caméra à hauteur des yeux
            var player = BuildPlayer();
            var playerController = player.GetComponent<PlayerController>();
            var playerCam = playerController.cameraTransform.GetComponent<Camera>();

            // Roulette (au-dessus du coffre, tournée vers le joueur)
            var rouletteGo = new GameObject("Roulette");
            rouletteGo.transform.position = new Vector3(0f, chestTop + 1.0f, 0f);
            rouletteGo.AddComponent<Billboard>();
            var roulette = rouletteGo.AddComponent<RouletteView>();

            // Indication "[E] Ouvrir le coffre" (au-dessus du coffre, tournée vers le joueur)
            var promptGo = new GameObject("PromptCoffre");
            promptGo.transform.position = new Vector3(0f, chestTop + 0.35f, 0f);
            promptGo.AddComponent<Billboard>();
            var prompt = promptGo.AddComponent<ChestPrompt>();

            var controller = chest.AddComponent<ChestController>();
            controller.wallet = wallet;
            controller.roulette = roulette;
            controller.prompt = prompt;
            prompt.chest = controller;
            prompt.player = player.transform;

            var interactor = player.AddComponent<DesktopInteractor>();
            interactor.chest = controller;
            interactor.player = playerController;
            interactor.playerCamera = playerCam;

            var panel = systems.AddComponent<ChestDebugPanel>();
            panel.chest = controller;
            panel.player = playerController;
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("SAE501 : scène créée → " + ScenePath + ". Lance Play : ZQSD pour bouger, souris pour regarder, E près du coffre, F1 pour le menu bêta.");
        }

        static Bounds GetBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        // Bonhomme basique (capsule + tête) avec CharacterController, et la caméra FPS à hauteur des yeux.
        static GameObject BuildPlayer()
        {
            var player = new GameObject("Joueur");
            player.transform.position = new Vector3(0f, 0f, -3.2f); // regarde vers +Z, donc vers le coffre

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            var bodyMat = GetMaterial(PlayerMaterialPath, new Color(0.2f, 0.55f, 0.95f));
            AddPart(player.transform, PrimitiveType.Capsule, "Corps", new Vector3(0f, 0.8f, 0f), new Vector3(0.6f, 0.8f, 0.6f), bodyMat);
            AddPart(player.transform, PrimitiveType.Sphere, "Tete", new Vector3(0f, 1.65f, 0f), Vector3.one * 0.3f, bodyMat);
            AddPart(player.transform, PrimitiveType.Cube, "Nez", new Vector3(0f, 1.65f, 0.16f), Vector3.one * 0.08f, bodyMat);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();

            var controller = player.AddComponent<PlayerController>();
            controller.cameraTransform = camGo.transform;
            return player;
        }

        static void AddPart(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>()); // c'est le CharacterController qui gère les collisions
        }

        static Material GetMaterial(string path, Color color)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            mat.SetColor("_BaseColor", color);
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}