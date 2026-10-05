#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

/// Menu  Bananes > Installer dans la scène ouverte
///       Bananes > Créer une scène de test
/// Fait tout le montage automatiquement :
///  - matériaux (Built-in ou URP détecté tout seul) et import des FBX,
///  - prefab « Banane » (modèle + collider + Rigidbody + Banane.cs + XR Grab Interactable si le XR Interaction
///    Toolkit est installé, avec Select Entered -> Prise() et Select Exited -> Lachee() déjà branchés),
///  - Bananier (Bananier.cs réglé : point d'apparition, arbre, prefab),
///  - Panier (zone de dépôt en trigger + Panier.cs),
///  - Récolteur (désactivé par défaut : cocher « Achete » pour tester).
/// Fichiers : _Project/Art/Bananier (FBX, textures, matériaux), _Project/Prefabs, _Project/Scripts,
/// scène de test dans _Project/Scenes/Sandbox/Maxens/BananierSandbox.unity.
// Intégration : les menus « Bananes » ont été retirés, le bananier est monté par le menu SAE → Générer le prototype
// (PrototypeGenerator appelle Construire). Le code de Maxens est inchangé par ailleurs.
public static class BananesInstaller
{
    static string s_root, s_fbx;   // s_root = _Project/Art/Bananier (FBX, Textures, Materiaux)
    const string DossierPrefabs = "Assets/_Project/Prefabs";
    const string PrefabBanane = DossierPrefabs + "/Banane.prefab";
    const string DossierSandbox = "Assets/_Project/Scenes/Sandbox/Maxens";
    const string SceneTest = DossierSandbox + "/BananierSandbox.unity";

    public static void InstallerDansScene()
    {
        if (!Preparer()) return;
        Construire(Vector3.zero);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Bananes : installé dans la scène ouverte. Déplace l'objet « Systeme_Bananes » où tu veux.");
    }

    public static void SceneDeTest()
    {
        if (!Preparer()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var sol = GameObject.CreatePrimitive(PrimitiveType.Plane); sol.name = "Sol"; sol.transform.localScale = Vector3.one * 2f;
        Construire(Vector3.zero);
        var cam = Camera.main;
        if (cam != null) { cam.transform.position = new Vector3(0, 2.2f, -3.2f); cam.transform.LookAt(new Vector3(0.4f, 0.4f, 0)); }
        CreerDossiers(DossierSandbox);
        EditorSceneManager.SaveScene(scene, SceneTest);
        Debug.Log("Bananes : scène de test créée. Play, puis clic sur les bananes pour les envoyer dans le panier (test sans casque).");
    }

    // ------------------------------------------------------------------------------------------
    static bool Preparer()
    {
        var g = AssetDatabase.FindAssets("Bananes_Collectible t:Model");
        if (g.Length == 0) { Debug.LogError("Bananes : Bananes_Collectible.fbx introuvable — copie le dossier « Bananes » dans Assets/."); return false; }
        s_fbx = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(g[0])).Replace('\\', '/');
        s_root = Path.GetDirectoryName(s_fbx).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(s_root + "/Materiaux")) AssetDatabase.CreateFolder(s_root, "Materiaux");
        CreerDossiers(DossierPrefabs);

        var matBananier = Mat("Bananier_Mat", "Bananier_Texture");
        var matPanier = Mat("Panier_Mat", "Panier_Texture");
        foreach (var n in new[] { "Bananier", "Bananes_Collectible", "Panier" })
        {
            var imp = AssetImporter.GetAtPath(s_fbx + "/" + n + ".fbx") as ModelImporter;
            if (imp == null) { Debug.LogError("Bananes : " + n + ".fbx introuvable dans " + s_fbx); return false; }
            imp.animationType = ModelImporterAnimationType.None;
            imp.importAnimation = false;
            imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Bananier_Mat"), matBananier);
            imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Panier_Mat"), matPanier);
            imp.SaveAndReimport();
        }
        CreerPrefabBanane();
        return true;
    }

    /// Crée un chemin de dossiers (ex. Assets/_Project/Scenes/Sandbox/Maxens) s'il n'existe pas.
    static void CreerDossiers(string chemin)
    {
        string courant = "Assets";
        foreach (var part in chemin.Split('/').Skip(1))
        {
            if (!AssetDatabase.IsValidFolder(courant + "/" + part)) AssetDatabase.CreateFolder(courant, part);
            courant += "/" + part;
        }
    }

    static Material Mat(string name, string tex)
    {
        bool urp = GraphicsSettings.currentRenderPipeline != null;
        var sh = Shader.Find(urp ? "Universal Render Pipeline/Lit" : "Standard");
        string path = s_root + "/Materiaux/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
        m.shader = sh;
        Texture2D t = null;
        foreach (var id in AssetDatabase.FindAssets(tex + " t:Texture2D", new[] { s_root }))
        {
            var p = AssetDatabase.GUIDToAssetPath(id);
            if (Path.GetFileNameWithoutExtension(p) == tex) t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        }
        if (t != null)
        {
            var ti = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(t)) as TextureImporter;
            if (ti != null && ti.filterMode != FilterMode.Point) { ti.filterMode = FilterMode.Point; ti.SaveAndReimport(); }  // texture palette : couleurs nettes
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", t);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", t);
        }
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.15f);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.15f);
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    static GameObject Model(string n) { return AssetDatabase.LoadAssetAtPath<GameObject>(s_fbx + "/" + n + ".fbx"); }

    static Transform Find(GameObject root, string n)
    {
        return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == n);
    }

    // ------------------------------------------------------------------------------------------
    static void CreerPrefabBanane()
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(Model("Bananes_Collectible"));
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        go.name = "Banane";

        var bounds = new Bounds(go.transform.position, Vector3.zero);
        foreach (var r in go.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
        var col = go.AddComponent<BoxCollider>();
        col.center = go.transform.InverseTransformPoint(bounds.center);
        col.size = bounds.size * 1.3f;                 // un peu plus grand : plus facile à viser de loin

        var rb = go.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        var banane = go.AddComponent<Banane>();

        // XR Interaction Toolkit (2.x ou 3.x) si installé : on l'ajoute par réflexion pour ne pas casser
        // la compilation dans un projet sans XR.
        var grabType = TrouverType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable")
                    ?? TrouverType("UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable");
        if (grabType != null)
        {
            var grab = go.AddComponent(grabType);
            Brancher(grab, "selectEntered", banane.Prise);
            Brancher(grab, "selectExited", banane.Lachee);
            var p = grabType.GetProperty("throwOnDetach"); if (p != null) p.SetValue(grab, false);
            // ramassage de loin : visée au rayon de la manette, la banane vient dans la main
            var far = grabType.GetProperty("farAttachMode");
            if (far != null && far.PropertyType.IsEnum) far.SetValue(grab, Enum.Parse(far.PropertyType, "Near"));
            var ease = grabType.GetProperty("attachEaseInTime"); if (ease != null) ease.SetValue(grab, 0.15f);
            Debug.Log("Bananes : XR Grab Interactable ajouté à la banane et branché (Prise / Lachee).");
        }
        else Debug.LogWarning("Bananes : XR Interaction Toolkit non trouvé — la banane n'est pas attrapable en VR (clic souris seulement).");

        PrefabUtility.SaveAsPrefabAsset(go, PrefabBanane);
        UnityEngine.Object.DestroyImmediate(go);
    }

    static Type TrouverType(string nom)
    {
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = a.GetType(nom);
            if (t != null) return t;
        }
        return null;
    }

    static void Brancher(Component grab, string membre, UnityAction action)
    {
        var t = grab.GetType();
        object evt = t.GetProperty(membre)?.GetValue(grab) ?? t.GetField(membre)?.GetValue(grab);
        var ub = evt as UnityEventBase;
        if (ub == null) { Debug.LogWarning("Bananes : impossible de brancher " + membre + " — à faire à la main."); return; }
        UnityEventTools.AddVoidPersistentListener(ub, action);
    }

    // ------------------------------------------------------------------------------------------
    static void Construire(Vector3 origine)
    {
        var cam = Camera.main;                                   // test sans casque : clic souris
        if (cam != null && cam.GetComponent<TestSouris>() == null) cam.gameObject.AddComponent<TestSouris>();

        var racine = new GameObject("Systeme_Bananes");
        racine.transform.position = origine;

        // Bananier
        var arbreGo = (GameObject)PrefabUtility.InstantiatePrefab(Model("Bananier"));
        arbreGo.transform.SetParent(racine.transform, false);
        var bananier = arbreGo.AddComponent<Bananier>();
        bananier.prefabBanane = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabBanane).GetComponent<Banane>();
        bananier.pointApparition = Find(arbreGo, "BananaSpawn");
        bananier.arbre = Find(arbreGo, "Bananier_Arbre");
        var bac = Find(arbreGo, "Bananier_Bac");
        if (bac != null)
        {
            bac.gameObject.AddComponent<MeshCollider>();   // les bananes lâchées ne traversent pas le bac
            bananier.socle = bac.GetComponent<Renderer>(); // les bananes tombent en dehors du bac
        }

        // Panier, posé à côté du bananier
        var panierGo = (GameObject)PrefabUtility.InstantiatePrefab(Model("Panier"));
        panierGo.transform.SetParent(racine.transform, false);
        panierGo.transform.localPosition = new Vector3(1.3f, 0f, -0.4f);
        foreach (var mf in panierGo.GetComponentsInChildren<MeshFilter>())
            mf.gameObject.AddComponent<MeshCollider>();                // parois du panier
        var zone = Find(panierGo, "Zone_Depot");
        if (zone == null) { zone = new GameObject("Zone_Depot").transform; zone.SetParent(panierGo.transform, false); zone.localPosition = new Vector3(0, 0.13f, 0); zone.localScale = new Vector3(0.23f, 0.12f, 0.23f); }
        var box = zone.gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true; box.size = Vector3.one * 2f;               // l'empty est déjà à l'échelle de l'intérieur
        var panier = zone.gameObject.AddComponent<Panier>();
        bananier.versCible = panierGo.transform;           // les bananes tombent du côté du panier

        // Récolteur (désactivé : cocher « Achete » pour tester)
        var rec = new GameObject("Recolteur").AddComponent<Recolteur>();
        rec.transform.SetParent(racine.transform, false);
        rec.transform.localPosition = new Vector3(-1.2f, 0f, 0f);
        rec.bananier = bananier; rec.panier = panier; rec.achete = false;

        Selection.activeGameObject = racine;
    }
}
#endif
