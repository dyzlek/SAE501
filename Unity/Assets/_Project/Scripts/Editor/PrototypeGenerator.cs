using System.IO;
using System.Linq;
using System.Reflection;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace SAE.EditorTools
{
    // Menu SAE → Générer le prototype : construit la scène Jeu en cubes (greybox).
    // Une seule scène, deux zones : le hub (autour de l'origine) et la carte (plus loin en Z).
    // Ainsi le plateau du hub montre la carte en direct. Relancer le menu écrase la scène.
    public static class PrototypeGenerator
    {
        const string Folder = "Assets/_Project/Scenes";
        const string ScenePath = Folder + "/Jeu.unity";
        const float BoardTile = 0.2f;    // plateau de 1,6 m : l'élément principal du hub
        const float HandHeight = 0.9f;   // table des bananes et socle du panier : à hauteur de main, pas au sol
        const int IgnoreRaycast = 2;     // couche Unity « Ignore Raycast »
        static readonly Vector3 MapCenter = new Vector3(0f, 0f, 40f);

        static readonly Color Floor = new Color(0.35f, 0.35f, 0.38f);
        static readonly Color Wood = new Color(0.45f, 0.30f, 0.18f);
        static readonly Color FloorWood = new Color(0.55f, 0.40f, 0.26f);   // plancher de la cabane
        static readonly Color WallWood = new Color(0.50f, 0.34f, 0.20f);
        static readonly Color Beam = new Color(0.30f, 0.20f, 0.12f);         // poutres et poteaux, plus foncés
        static readonly Color PathColor = new Color(0.62f, 0.45f, 0.25f);
        static readonly Color Grass1 = new Color(0.30f, 0.62f, 0.28f);
        static readonly Color Grass2 = new Color(0.26f, 0.55f, 0.24f);

        [MenuItem("SAE/Générer le prototype")]
        public static void Generate()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Folder);
            TagSetup.EnsureTags();
            VRSetup.Configure();
            CabinArt.Build();   // textures et matériaux de la cabane du hub (bois, tapis, vitres)

            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var defaultCam = GameObject.FindWithTag("MainCamera");
            if (defaultCam) Object.DestroyImmediate(defaultCam); // la caméra est celle du joueur

            var hubSpawn = Spawn("Spawn Hub", Vector3.zero);
            var mapSpawn = Spawn("Spawn Carte", MapCenter + new Vector3(0, 0, -MapLayout.HalfExtent - 3f));

            var mapRoot = BuildMap(hubSpawn);
            var spawner = mapRoot.GetComponent<WaveSpawner>();
            var hub = BuildHub(mapRoot, mapSpawn, spawner);   // avant le joueur : l'installeur des bananes ajoute son TestSouris à Camera.main s'il en trouve une
            var player = Player(hubSpawn.position);
            if (!player) return;
            BuildChest(hub, Around(100f, Ring - 0.2f), player.head);
            BuildPlayerMode(player.gameObject, DesktopPlayerObject(hubSpawn.position));

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerModeMenu.Apply();   // VR ou PC, selon le menu SAE → Mode de jeu
            Debug.Log("Prototype généré : " + ScenePath);
        }

        static Transform Spawn(string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            return go.transform;
        }

        // Le joueur VR : l'XR Origin des Starter Assets (casque + 2 manettes, téléportation, rotation par crans),
        // avec nos réglages de confort, un bout de doigt sur chaque manette pour enfoncer les boutons,
        // et le corps visible en miniature sur le plateau.
        // Les deux joueurs sont dans la scène, désactivés : PlayerMode active le bon au lancement (menu SAE → Mode de jeu).
        static void BuildPlayerMode(GameObject vrPlayer, GameObject pcPlayer)
        {
            var mode = new GameObject("Mode de jeu").AddComponent<PlayerMode>();
            mode.vrPlayer = vrPlayer;
            mode.pcPlayer = pcPlayer;
            vrPlayer.SetActive(false);
            pcPlayer.SetActive(false);
        }

        // Le joueur PC (clavier/souris), pour tester vite sans casque : DesktopPlayer, et la caméra sert de tête et de mains.
        static GameObject DesktopPlayerObject(Vector3 position)
        {
            var player = new GameObject("Joueur PC");
            player.tag = Tags.Joueur;
            player.layer = IgnoreRaycast;
            player.transform.position = position;
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0, 0.9f, 0);

            // Corps : copié en miniature sur le plateau (« Toi »)
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Corps";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = new Vector3(0, 0.9f, 0);
            body.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
            body.GetComponent<Renderer>().enabled = false;
            body.AddComponent<ColorTint>().Set(new Color(1f, 0.55f, 0.1f));
            body.AddComponent<Mirrored>().label = "Toi";

            var cam = new GameObject("Camera");
            cam.tag = "MainCamera";
            cam.transform.SetParent(player.transform, false);
            cam.transform.localPosition = new Vector3(0, 1.6f, 0);
            cam.AddComponent<Camera>().nearClipPlane = 0.05f;
            cam.AddComponent<AudioListener>();

            player.AddComponent<DesktopPlayer>();
            var rig = player.AddComponent<PlayerRig>();
            rig.head = rig.leftHand = rig.rightHand = cam.transform;
            player.AddComponent<MonkeyInfoCard>();
            return player;
        }

        static PlayerRig Player(Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VRSetup.RigPrefab);
            if (!prefab) { Debug.LogError("Joueur VR : Starter Assets de l'XR Interaction Toolkit introuvables : " + VRSetup.RigPrefab); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = "Joueur VR";
            go.tag = Tags.Joueur;
            go.layer = IgnoreRaycast;   // les rayons (pose des singes, fiche) traversent le joueur, comme dans le cours
            go.transform.position = position;

            var origin = go.GetComponent<XROrigin>();
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;   // la vraie taille du joueur

            // Confort : pas de déplacement continu (il donne la nausée). Les deux sticks téléportent,
            // le stick droit tourne par crans (snap turn, réglage par défaut des Starter Assets).
            foreach (var manager in go.GetComponentsInChildren<ControllerInputActionManager>(true))
            {
                var so = new SerializedObject(manager);
                so.FindProperty("m_SmoothMotionEnabled").boolValue = false;
                so.FindProperty("m_SmoothTurnEnabled").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            ShowRays(go);

            var rig = go.AddComponent<PlayerRig>();
            rig.head = origin.Camera.transform;
            rig.leftHand = FindChild(go.transform, "Left Controller");
            rig.rightHand = FindChild(go.transform, "Right Controller");
            AddFingertip(rig.leftHand);
            AddFingertip(rig.rightHand);
            go.AddComponent<MonkeyInfoCard>();   // fiche du singe visé, dans le décor

            // Corps : invisible pour soi, mais c'est lui qu'on voit en miniature sur le plateau (et plus tard un 2e joueur).
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Corps";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0, 0.9f, 0);
            body.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
            body.GetComponent<Renderer>().enabled = false;
            body.AddComponent<ColorTint>().Set(new Color(1f, 0.55f, 0.1f));
            body.AddComponent<Mirrored>().label = "Toi";
            return rig;
        }

        // Le rayon de chaque manette : celui des Starter Assets (courbé, 25 cm, presque transparent) est éteint,
        // et remplacé par notre HandRay, droit et toujours visible, qui part de l'interacteur Near-Far de la manette.
        static void ShowRays(GameObject rig)
        {
            foreach (var visual in rig.GetComponentsInChildren<CurveVisualController>(true))
                visual.gameObject.SetActive(false);
            foreach (var nearFar in rig.GetComponentsInChildren<NearFarInteractor>(true))
            {
                var ray = new GameObject("Rayon");
                ray.transform.SetParent(nearFar.transform, false);
                ray.AddComponent<HandRay>().interactor = nearFar;
            }
        }

        // Petite sphère au bout de la manette : elle enfonce les boutons qu'elle touche (HandPress).
        static void AddFingertip(Transform hand)
        {
            if (!hand) { Debug.LogWarning("Joueur VR : manette introuvable, pas de bout de doigt."); return; }
            var tip = new GameObject("Bout du doigt");
            tip.layer = IgnoreRaycast;
            tip.transform.SetParent(hand, false);
            tip.transform.localPosition = new Vector3(0f, -0.01f, 0.06f);   // devant la manette, là où on pointe
            tip.AddComponent<SphereCollider>().radius = 0.03f;
            tip.AddComponent<Rigidbody>();
            tip.AddComponent<HandPress>();
        }

        static Transform FindChild(Transform root, string name) =>
            root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);

        // Sol sur lequel on peut se téléporter (couche d'interaction « Teleport », celle du rayon de téléportation).
        static void Teleportable(GameObject floor)
        {
            var area = floor.AddComponent<TeleportationArea>();
            area.interactionLayers = 1 << VRSetup.TeleportLayer;
        }

        static ActionCube MakeActionCube(Transform parent, string name, Vector3 pos, float size, Color color,
            string label, ActionCube.Action action, Transform destination)
        {
            var cube = Visuals.Solid(name, parent, pos, Vector3.one * size, color);
            cube.tag = Tags.Bouton;
            var a = cube.AddComponent<ActionCube>();
            cube.AddComponent<RayPress>();   // on peut aussi l'enfoncer de loin, en le visant
            a.action = action;
            a.destination = destination;
            var text = Visuals.Label(cube.transform, label, new Vector3(0, 1.3f, 0), 0.12f);   // bien au-dessus : on voit le cube d'en haut
            text.transform.localScale = Vector3.one / size; // le cube parent est mis à l'échelle : on compense
            return a;
        }

        // Bouton LANCER : la vague ne part que quand le joueur appuie dessus.
        static void MakeLaunchCube(Transform parent, Vector3 pos, float size, WaveSpawner spawner)
        {
            var a = MakeActionCube(parent, "Lancer la vague", pos, size, new Color(0.95f, 0.45f, 0.15f),
                "LANCER", ActionCube.Action.StartWave, null);
            a.spawner = spawner;
        }

        // Tableau de la vague dans le décor (vague, vies, état) : remplace l'affichage à l'écran.
        // Comme la caisse : panneau fixe, +Z local tourné à l'opposé du joueur, texte côté joueur.
        static void BuildWaveBoard(Transform parent, Vector3 pos, Quaternion rotation, WaveSpawner spawner)
        {
            var root = new GameObject("Tableau de la vague").transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(pos, rotation);
            Visuals.Box("Cadre", root, Vector3.zero, new Vector3(1.7f, 0.8f, 0.06f), Wood);
            Visuals.Box("Fond", root, new Vector3(0, 0, -0.035f), new Vector3(1.6f, 0.7f, 0.02f), new Color(0.12f, 0.1f, 0.08f));
            var text = Visuals.Label(root, "", new Vector3(0, 0, -0.06f), 0.1f, new Color(1f, 0.9f, 0.6f));
            Object.DestroyImmediate(text.GetComponent<Billboard>());
            var board = root.gameObject.AddComponent<WaveBoard>();
            board.spawner = spawner;
            board.text = text;
        }

        // ---------------- HUB ----------------
        // Le hub est une cabane en rondins (voir BuildCabin). Tout est posé EN ROND contre les murs, tourné vers le joueur
        // qui se tient au centre, sur le tapis (point d'apparition, regard vers +Z). Cercle de 2,8 m : un ou deux pas suffisent.
        //   devant (0°)        : le plateau incliné (la carte en direct), le tableau de la vague au mur au-dessus,
        //                        LANCER (-41°), JOUER (-30°), Vider (+30°) et le panier (+42°)
        //   gauche (-82°)      : LA bibliothèque, un seul meuble courbe (7 types × 8 raretés)
        //   derrière (180°)    : la grande porte ouverte : le bananier dehors, la table des bananes juste devant
        //   arrière-gauche     : le panneau RÉCOLTEUR (-132°, posé au lancement) et la caisse accrochée au mur au-dessus
        //   arrière-droite     : le panneau d'amélioration du bananier (140°) ; droite : le coffre (100°) et ses chances
        const float Ring = HubLayout.Ring;   // rayon du cercle (2,8 m) : tout est posé dessus, le centre reste libre pour circuler
        const float SlotSize = 0.2f;      // une seule bibliothèque compacte (7 types × 8 raretés), tout à portée de bras
        const float SlotStepX = 0.28f;    // espace entre deux raretés
        const float SlotStepY = 0.26f;    // espace entre deux étagères : la plus haute est à 2,1 m
        const float FirstShelfY = 0.55f;  // rangée la plus basse (sans se baisser)
        const float TreeScale = 1.4f;     // le palmier de Maxens, agrandi, dehors derrière la grande porte
        const float BasketAngle = HubLayout.BasketAngle;     // le panier, à côté du plateau (HarvesterSetup le déplace aussi dans une scène plus ancienne)
        const float BasketRadius = HubLayout.BasketRadius;

        // Position sur le cercle. angle 0 = devant, positif = à droite.
        static Vector3 Around(float angleDeg, float radius, float height = 0f)
        {
            float a = angleDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(a) * radius, height, Mathf.Cos(a) * radius);
        }

        static Transform BuildHub(Transform mapRoot, Transform mapSpawn, WaveSpawner spawner)
        {
            var env = new GameObject("Hub").transform;
            BuildCabin(env);

            // Devant : le plateau, avec JOUER, LANCER, Vider et le panier de part et d'autre
            BuildBoard(env, mapRoot);
            MakeActionCube(env, "Jouer", Around(-30f, Ring - 0.5f, 1.0f), 0.35f, new Color(0.2f, 0.85f, 0.3f),
                "JOUER", ActionCube.Action.Teleport, mapSpawn);
            MakeActionCube(env, "Vider", Around(30f, Ring - 0.5f, 0.9f), 0.25f, new Color(0.6f, 0.6f, 0.6f),
                "Vider", ActionCube.Action.ClearBoard, null);
            MakeLaunchCube(env, Around(-41f, Ring - 0.5f, 1.0f), 0.3f, spawner);
            BuildWaveBoard(env, Around(0f, HubLayout.CabinRadius - 0.25f, 2.25f), Quaternion.identity, spawner);   // accroché au mur

            // À gauche : la bibliothèque (tous les types dans un seul meuble)
            BuildShelf(env, "Bibliotheque", -82f, 0, MonkeyData.TypeCount);

            // Derrière : le bananier dehors, derrière la grande porte ; la table des bananes dedans, juste devant la porte
            var bananier = BuildBananas(env, Around(180f, Ring + 1.3f), 180f);
            BuildMoneyBoard(env, HubLayout.HarvesterPanelAngle);
            if (bananier) BuildUpgradePanel(env, bananier, 140f);

            BuildDecor(env);
            UseWoodTexture(env);
            return env;
        }

        // La cabane en rondins qui ferme le hub : 12 murs de rondins empilés, des poteaux aux angles,
        // un toit conique en planches avec sa charpente, une grande porte ouverte sur le bananier (derrière),
        // trois fenêtres, un lustre et deux lanternes, un plancher et un tapis rond au centre.
        // Les matériaux (bois, tapis, vitres) sont fabriqués par CabinArt.
        // Murs et toit ne font pas d'ombre : le soleil éclaire toujours l'intérieur, les lampes ajoutent la lumière chaude.
        const int CabinSides = 12;
        const float LogSize = 0.22f;      // diamètre d'un rondin, en mètres
        const float DoorHeight = 2.4f;    // la grande porte, derrière (le bananier est dehors)

        static void BuildCabin(Transform env)
        {
            var cabin = new GameObject("Cabane").transform;
            cabin.SetParent(env, false);
            float r = HubLayout.CabinRadius, h = HubLayout.CabinHeight;
            float sideLength = 2f * r * Mathf.Tan(Mathf.PI / CabinSides);

            // Plancher : dedans, et dehors en terrasse sous le bananier (même bois)
            var floor = Visuals.Solid("Plancher", cabin, new Vector3(0, -0.05f, 0), new Vector3(r * 2f + 3f, 0.1f, r * 2f + 3f), Color.white);
            floor.GetComponent<Renderer>().sharedMaterial = CabinArt.Floor;
            Teleportable(floor);

            for (int i = 0; i < CabinSides; i++)
            {
                float angle = i * 360f / CabinSides;
                bool door = Mathf.Approximately(angle, 180f);
                var side = new GameObject(door ? "Mur de la porte" : $"Mur {i}").transform;
                side.SetParent(cabin, false);
                side.SetPositionAndRotation(Around(angle, r), Quaternion.Euler(0, angle, 0));   // +Z local = vers l'extérieur

                // Un seul collider par mur (invisible) : on ne passe pas au travers, ni en marchant ni en se téléportant
                float bottom = door ? DoorHeight : 0f;
                var col = side.gameObject.AddComponent<BoxCollider>();
                col.center = new Vector3(0, (bottom + h) / 2f, 0);
                col.size = new Vector3(sideLength, h - bottom, LogSize);

                // Les rondins, couchés et empilés ; un peu plus longs que le mur pour se croiser aux angles
                for (float y = bottom + LogSize / 2f; y < h; y += LogSize * 0.9f)
                    NoShadow(Part("Rondin", side, PrimitiveType.Cylinder, new Vector3(0, y, 0),
                        new Vector3(LogSize, sideLength / 2f + 0.12f, LogSize), CabinArt.Logs, Quaternion.Euler(0, 0, 90)));

                if (door)
                    for (int s = -1; s <= 1; s += 2)   // les montants de la porte
                        NoShadow(Part("Montant", side, PrimitiveType.Cylinder, new Vector3(s * (sideLength / 2f - 0.12f), DoorHeight / 2f, 0),
                            new Vector3(0.26f, DoorHeight / 2f, 0.26f), CabinArt.Logs));
                else if (angle == 60f || angle == 150f || angle == 210f)
                    BuildWindow(side, 2.15f, 0.9f, 0.7f);

                // Poteau d'angle (entre ce mur et le suivant)
                NoShadow(Part("Poteau", cabin, PrimitiveType.Cylinder, Around(angle + 15f, r / Mathf.Cos(Mathf.PI / CabinSides), h / 2f),
                    new Vector3(0.32f, h / 2f, 0.32f), CabinArt.Logs));
            }

            BuildRoof(cabin, r, h);
            BuildLights(cabin, h);

            // Tapis rond au centre, là où se tient le joueur (sans collider : la téléportation vise le plancher dessous)
            Part("Tapis", cabin, PrimitiveType.Quad, new Vector3(0, 0.005f, 0), Vector3.one * (HubLayout.CarpetRadius * 2f),
                CabinArt.Rug, Quaternion.Euler(90, 0, 0));

            // Rien ici ne bouge : Unity regroupe les rondins en quelques gros maillages (moins d'appels de dessin, plus de fps)
            foreach (var t in cabin.GetComponentsInChildren<Transform>())
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
        }

        // Fenêtre (fausse : une vitre qui brille, couleur ciel), avec son cadre, sa croix et son appui, côté intérieur du mur
        static void BuildWindow(Transform side, float y, float width, float height)
        {
            float z = -LogSize / 2f - 0.02f;
            NoShadow(Part("Vitre", side, PrimitiveType.Cube, new Vector3(0, y, z), new Vector3(width, height, 0.01f), CabinArt.Glass));
            var frame = new Color(0.28f, 0.18f, 0.1f);
            Tinted(Part("Cadre haut", side, PrimitiveType.Cube, new Vector3(0, y + height / 2f, z - 0.02f), new Vector3(width + 0.1f, 0.06f, 0.06f), CabinArt.Furniture), frame);
            Tinted(Part("Appui", side, PrimitiveType.Cube, new Vector3(0, y - height / 2f, z - 0.05f), new Vector3(width + 0.2f, 0.06f, 0.14f), CabinArt.Furniture), frame);
            for (int s = -1; s <= 1; s += 2)
                Tinted(Part("Cadre côté", side, PrimitiveType.Cube, new Vector3(s * width / 2f, y, z - 0.02f), new Vector3(0.06f, height, 0.06f), CabinArt.Furniture), frame);
            Tinted(Part("Croix", side, PrimitiveType.Cube, new Vector3(0, y, z - 0.015f), new Vector3(0.03f, height, 0.03f), CabinArt.Furniture), frame);
            Tinted(Part("Croix", side, PrimitiveType.Cube, new Vector3(0, y, z - 0.015f), new Vector3(width, 0.03f, 0.03f), CabinArt.Furniture), frame);
        }

        // Toit conique en planches (un maillage fait en code : 12 triangles vers la pointe, une face dedans, une dehors),
        // qui dépasse un peu des murs, et ses chevrons (une poutre par angle, de l'avant-toit à la pointe).
        static void BuildRoof(Transform cabin, float r, float h)
        {
            float eave = (r + 0.35f) / Mathf.Cos(Mathf.PI / CabinSides);   // coin de l'avant-toit
            var apex = new Vector3(0, h + 1.8f, 0);
            var verts = new System.Collections.Generic.List<Vector3>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            for (int i = 0; i < CabinSides; i++)
            {
                var p0 = Around(15f + i * 30f, eave, h);
                var p1 = Around(45f + i * 30f, eave, h);
                float edge = Vector3.Distance(p0, p1), slant = Vector3.Distance((p0 + p1) / 2f, apex);
                // Ordre des sommets : la face visible est celle d'où on les voit tourner dans le sens des aiguilles d'une montre.
                bool outward = Vector3.Dot(Vector3.Cross(p1 - p0, apex - p0), (p0 + p1) / 2f) > 0f;
                foreach (bool inside in new[] { true, false })
                {
                    bool keep = inside != outward;
                    verts.Add(keep ? p0 : p1); verts.Add(keep ? p1 : p0); verts.Add(apex);
                    uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(edge * 0.5f, 0)); uvs.Add(new Vector2(edge * 0.25f, slant * 0.5f));
                }

                var rafterStart = p0 + Vector3.down * 0.1f;
                var rafter = Part("Chevron", cabin, PrimitiveType.Cube, (rafterStart + apex) / 2f + Vector3.down * 0.08f,
                    new Vector3(0.1f, 0.14f, Vector3.Distance(rafterStart, apex)), CabinArt.Furniture, Quaternion.LookRotation(apex - rafterStart));
                Tinted(rafter, new Color(0.3f, 0.2f, 0.12f));
                NoShadow(rafter);
            }
            var tris = new int[verts.Count];
            for (int i = 0; i < tris.Length; i++) tris[i] = i;
            var mesh = new Mesh { name = "Toit conique" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var roof = new GameObject("Toit");
            roof.transform.SetParent(cabin, false);
            roof.AddComponent<MeshFilter>().sharedMesh = mesh;
            var rend = roof.AddComponent<MeshRenderer>();
            rend.sharedMaterial = CabinArt.Roof;
            rend.shadowCastingMode = ShadowCastingMode.Off;
        }

        // Un lustre en roue de charrette au centre (6 bougies) et deux lanternes aux murs : lumière chaude, sans ombres (fps)
        static void BuildLights(Transform cabin, float h)
        {
            float wheelY = 2.75f;
            Part("Chaîne", cabin, PrimitiveType.Cube, new Vector3(0, (wheelY + h + 1.6f) / 2f, 0), new Vector3(0.03f, h + 1.6f - wheelY, 0.03f), CabinArt.Iron);
            Tinted(Part("Roue du lustre", cabin, PrimitiveType.Cylinder, new Vector3(0, wheelY, 0), new Vector3(0.9f, 0.03f, 0.9f), CabinArt.Furniture),
                new Color(0.3f, 0.2f, 0.12f));
            for (int i = 0; i < 6; i++)
                Part("Bougie", cabin, PrimitiveType.Cylinder, Around(i * 60f, 0.4f, wheelY + 0.08f), new Vector3(0.05f, 0.06f, 0.05f), CabinArt.Glow);
            AddLight(cabin, new Vector3(0, wheelY - 0.1f, 0), 6.5f, 2.2f);

            foreach (float angle in new[] { 120f, -60f })
            {
                var pos = Around(angle, HubLayout.CabinRadius - LogSize - 0.12f, 2.55f);
                Part("Support", cabin, PrimitiveType.Cube, Around(angle, HubLayout.CabinRadius - LogSize - 0.05f, 2.7f), new Vector3(0.04f, 0.04f, 0.04f), CabinArt.Iron);
                Part("Lanterne", cabin, PrimitiveType.Cube, pos, new Vector3(0.12f, 0.16f, 0.12f), CabinArt.Glow);
                Part("Chapeau", cabin, PrimitiveType.Cube, pos + Vector3.up * 0.1f, new Vector3(0.16f, 0.04f, 0.16f), CabinArt.Iron);
                AddLight(cabin, pos, 3.5f, 1.2f);
            }
        }

        static void AddLight(Transform parent, Vector3 pos, float range, float intensity)
        {
            var light = new GameObject("Lumière").AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.localPosition = pos;
            light.type = LightType.Point;
            light.range = range;
            light.intensity = intensity;
            light.color = new Color(1f, 0.8f, 0.55f);
            light.shadows = LightShadows.None;
        }

        // Les petits objets qui donnent vie à la cabane : caisses, tonneaux, régimes de bananes (dedans et sur la terrasse)
        static void BuildDecor(Transform env)
        {
            var decor = new GameObject("Décor").transform;
            decor.SetParent(env, false);

            Crate(decor, Around(-50f, Ring + 0.3f), 0.5f, 10f);
            Crate(decor, Around(-50f, Ring + 0.3f, 0.5f), 0.34f, -15f);
            Barrel(decor, Around(57f, Ring + 0.25f));
            Bananas(decor, Around(57f, Ring + 0.25f, 0.92f));

            // Dehors, autour du bananier
            Barrel(decor, Around(205f, Ring + 1.6f));
            Bananas(decor, Around(205f, Ring + 1.6f, 0.92f));
            Barrel(decor, Around(155f, Ring + 1.5f));
            Crate(decor, Around(145f, Ring + 1.9f), 0.5f, 30f);
        }

        static void Crate(Transform parent, Vector3 pos, float size, float yaw)
        {
            var crate = Part("Caisse en bois", parent, PrimitiveType.Cube, pos + Vector3.up * (size / 2f), Vector3.one * size, CabinArt.Furniture, Quaternion.Euler(0, yaw, 0));
            Tinted(crate, new Color(0.72f, 0.55f, 0.35f));
            crate.AddComponent<BoxCollider>();
            for (int s = -1; s <= 1; s += 2)   // deux cerclages plus foncés
                Tinted(Part("Planche", crate.transform, PrimitiveType.Cube, new Vector3(0, s * 0.35f, 0), new Vector3(1.03f, 0.14f, 1.03f), CabinArt.Furniture),
                    new Color(0.42f, 0.28f, 0.16f));
        }

        static void Barrel(Transform parent, Vector3 pos)
        {
            var barrel = Part("Tonneau", parent, PrimitiveType.Cylinder, pos + Vector3.up * 0.45f, new Vector3(0.55f, 0.45f, 0.55f), CabinArt.Furniture);
            Tinted(barrel, new Color(0.55f, 0.36f, 0.2f));
            barrel.AddComponent<CapsuleCollider>();
            foreach (float y in new[] { -0.6f, 0.6f })   // cercles de fer
                Part("Cercle", barrel.transform, PrimitiveType.Cylinder, new Vector3(0, y, 0), new Vector3(1.04f, 0.05f, 1.04f), CabinArt.Iron);
        }

        // Un régime de bananes posé : 5 bananes en éventail autour d'une tige
        static void Bananas(Transform parent, Vector3 pos)
        {
            var bunch = new GameObject("Régime de bananes").transform;
            bunch.SetParent(parent, false);
            bunch.position = pos;
            Tinted(Part("Tige", bunch, PrimitiveType.Cylinder, new Vector3(0, 0.06f, 0), new Vector3(0.03f, 0.06f, 0.03f), null), new Color(0.35f, 0.25f, 0.1f));
            for (int i = 0; i < 5; i++)
            {
                var banana = Part("Banane", bunch, PrimitiveType.Capsule, Around(i * 72f, 0.07f, 0.05f), new Vector3(0.05f, 0.09f, 0.05f), null,
                    Quaternion.Euler(0, i * 72f, 0) * Quaternion.Euler(70f, 0, 0));
                Tinted(banana, new Color(1f, 0.85f, 0.2f));
            }
        }

        // Une pièce de décor sans collider (les murs et les meubles portent leur propre collider)
        static GameObject Part(string name, Transform parent, PrimitiveType type, Vector3 localPos, Vector3 scale, Material material, Quaternion? rotation = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = rotation ?? Quaternion.identity;
            go.transform.localScale = scale;
            if (material) go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        static GameObject Tinted(GameObject go, Color color)
        {
            go.AddComponent<ColorTint>().Set(color);
            return go;
        }

        // Les meubles en bois uni (pupitres, bibliothèque, socles, cadres) prennent la texture du bois, gardant leur teinte
        static void UseWoodTexture(Transform env)
        {
            foreach (var tint in env.GetComponentsInChildren<ColorTint>(true))
                if (tint.color == Wood) tint.GetComponent<Renderer>().sharedMaterial = CabinArt.Furniture;
        }

        static void NoShadow(GameObject go) => go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        // Plateau incliné de 25° vers le joueur, posé sur une planche qui suit l'inclinaison + un pied.
        static void BuildBoard(Transform env, Transform mapRoot)
        {
            float scale = BoardTile / MapLayout.Tile;
            float side = MapLayout.Size * BoardTile;
            var center = Around(0f, Ring - 0.9f, 0.95f);   // reculé jusqu'au cercle, devant le joueur

            var boardGo = new GameObject("Plateau");
            boardGo.tag = Tags.Plateau;
            boardGo.transform.SetParent(env, false);
            boardGo.transform.SetPositionAndRotation(center, Quaternion.Euler(-25f, 0, 0));
            var board = boardGo.AddComponent<Board>();
            board.mapRoot = mapRoot;
            board.scale = scale;
            boardGo.AddComponent<PlacementSurface>().scale = scale;
            var col = boardGo.AddComponent<BoxCollider>();
            col.size = new Vector3(side, 0.04f, side);
            col.center = new Vector3(0, -0.02f, 0);
            BuildGrid(boardGo.transform, BoardTile, 0.04f, false);

            // Planche sous le plateau (même inclinaison) : rien ne traverse la surface
            Visuals.Box("Planche", boardGo.transform, new Vector3(0, -0.07f, 0), new Vector3(side + 0.06f, 0.06f, side + 0.06f), Wood);
            // Pied vertical : son sommet s'arrête sous la planche
            float legTop = center.y - 0.18f;
            Visuals.Solid("Pied", env, new Vector3(0, legTop / 2f, center.z), new Vector3(0.12f, legTop, 0.12f), Wood);
            Visuals.Solid("Base du pied", env, new Vector3(0, 0.02f, center.z), new Vector3(0.6f, 0.04f, 0.6f), Wood);
        }

        // Un meuble COURBE qui suit le cercle autour du joueur : chaque colonne (une rareté) est tournée vers lui,
        // donc tout reste lisible depuis le centre (un meuble droit se voyait de biais et les textes se chevauchaient).
        // Une étagère par type (de firstType à firstType+count-1), une colonne par rareté, de gauche à droite.
        static void BuildShelf(Transform env, string name, float centerAngle, int firstType, int count)
        {
            var shelf = new GameObject(name).transform;
            shelf.tag = Tags.Bibliotheque;
            shelf.SetParent(env, false);

            float step = SlotStepX / Ring * Mathf.Rad2Deg;            // angle entre deux colonnes
            float height = FirstShelfY + count * SlotStepY;
            float Angle(float column) => centerAngle + (column - (MonkeyData.LevelCount - 1) / 2f) * step;
            Quaternion Facing(float angle) => Quaternion.Euler(0, angle, 0);   // +Z local = vers l'extérieur

            // Fond et planches : un morceau par colonne (+ un en plus de chaque côté pour les noms des types)
            for (int c = -1; c <= MonkeyData.LevelCount; c++)
            {
                float a = Angle(c);
                var fond = Visuals.Solid($"Fond {c}", shelf, Around(a, Ring + 0.25f, height / 2f), new Vector3(SlotStepX + 0.02f, height, 0.05f), Wood);
                fond.transform.rotation = Facing(a);
                for (int i = 0; i < count; i++)
                {
                    float y = FirstShelfY + i * SlotStepY - SlotSize / 2f - 0.02f;
                    var board = Visuals.Solid($"Etagere {i} {c}", shelf, Around(a, Ring + 0.05f, y), new Vector3(SlotStepX + 0.02f, 0.03f, 0.45f), Wood);
                    board.transform.rotation = Facing(a);
                }
            }
            Visuals.Label(shelf, "BIBLIOTHÈQUE", Around(centerAngle, Ring, height + 0.15f), 0.1f);

            for (int i = 0; i < count; i++)
            {
                var type = (MonkeyType)(firstType + i);
                float y = FirstShelfY + i * SlotStepY;
                Visuals.Label(shelf, type.ToString(), Around(Angle(-1), Ring - 0.05f, y), 0.07f);

                for (int l = 0; l < MonkeyData.LevelCount; l++)
                {
                    float a = Angle(l);
                    var slot = new GameObject($"Slot {type} {(Rarity)l}");
                    slot.tag = Tags.Bibliotheque;
                    slot.transform.SetParent(shelf, false);
                    slot.transform.SetPositionAndRotation(Around(a, Ring, y), Facing(a));
                    slot.AddComponent<BoxCollider>().size = Vector3.one * (SlotSize + 0.04f);
                    var s = slot.AddComponent<LibrarySlot>();
                    s.type = type;
                    s.level = (Rarity)l;
                    Visuals.MonkeyPiece(new Monkey(type, (Rarity)l), slot.transform, Vector3.zero, SlotSize, withLabel: false);   // la couleur dit la rareté, le nom s'affiche en visant
                }
            }
        }

        // Le bananier, son panier et le récolteur, montés par l'installeur de Maxens (BananesInstaller.Construire),
        // appelé tel quel pour ne pas dupliquer son code. Puis on le place sur le cercle, tourné vers le joueur.
        static Bananier BuildBananas(Transform env, Vector3 pos, float yaw)
        {
            var models = AssetDatabase.FindAssets("Bananes_Collectible t:Model");
            if (models.Length == 0) { Debug.LogWarning("Hub : modèles du bananier introuvables, bananier non placé."); return null; }
            var folder = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(models[0])).Replace(Path.DirectorySeparatorChar, '/');
            const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Static;
            typeof(BananesInstaller).GetField("s_fbx", Private).SetValue(null, folder);
            typeof(BananesInstaller).GetMethod("Construire", Private).Invoke(null, new object[] { Vector3.zero });

            var root = GameObject.Find("Systeme_Bananes");
            root.transform.SetParent(env, false);
            root.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));

            // Le palmier en plus grand (seulement l'arbre et son bac, pas le panier)
            var bananier = root.GetComponentInChildren<Bananier>();
            bananier.transform.localScale *= TreeScale;
            // plus de bananes coincées dans le bac ou sous les feuilles : elles sont remises à hauteur de la table
            bananier.gameObject.AddComponent<BananaGuard>().groundY = HandHeight + 0.1f;

            // Table des bananes : devant le bananier, à l'intérieur du cercle. Les bananes tombent DESSUS,
            // à hauteur de main : on ne se baisse pas pour les ramasser (règle de confort VR).
            var tablePos = Around(180f, Ring - 0.1f);   // juste devant la porte, le bananier dehors à 1,4 m
            var table = new GameObject("Table des bananes").transform;
            table.SetParent(env, false);
            table.SetPositionAndRotation(tablePos, Quaternion.LookRotation(new Vector3(tablePos.x, 0, tablePos.z) - new Vector3(pos.x, 0, pos.z)));
            Visuals.Solid("Plateau", table, new Vector3(0, HandHeight - 0.02f, 0), new Vector3(1.6f, 0.04f, 1.0f), new Color(0.95f, 0.85f, 0.35f));
            Visuals.Solid("Rebord avant", table, new Vector3(0, HandHeight + 0.03f, -0.49f), new Vector3(1.6f, 0.06f, 0.02f), Wood);
            Visuals.Solid("Rebord arriere", table, new Vector3(0, HandHeight + 0.03f, 0.49f), new Vector3(1.6f, 0.06f, 0.02f), Wood);
            Visuals.Solid("Rebord gauche", table, new Vector3(-0.79f, HandHeight + 0.03f, 0), new Vector3(0.02f, 0.06f, 1.0f), Wood);
            Visuals.Solid("Rebord droit", table, new Vector3(0.79f, HandHeight + 0.03f, 0), new Vector3(0.02f, 0.06f, 1.0f), Wood);
            Visuals.Solid("Pied", table, new Vector3(0, (HandHeight - 0.04f) / 2f, 0), new Vector3(0.12f, HandHeight - 0.04f, 0.12f), Wood);
            bananier.versCible = table;
            bananier.hauteurAuSol = HandHeight + 0.1f;   // la banane se pose sur la table, pas par terre
            bananier.margePanier = 0f;
            bananier.largeurZone = 0.8f;
            bananier.angleDispersion = 18f;

            // Le panier : juste à côté de la table, sur un socle à hauteur de main
            var basket = root.transform.Find("Panier");
            if (basket)
            {
                var basketPos = Around(BasketAngle, BasketRadius);   // avancé vers le centre : de la place autour, loin de la table et de la caisse
                Visuals.Solid("Socle du panier", env, basketPos + Vector3.up * (HandHeight / 2f), new Vector3(0.5f, HandHeight, 0.5f), Wood);
                basket.position = basketPos + Vector3.up * HandHeight;
            }
            return bananier;
        }

        // La caisse : un panneau en bois avec l'argent total en gros chiffres dorés, à côté du panier.
        static void BuildMoneyBoard(Transform env, float angle)
        {
            // Accrochée au mur, en hauteur : elle ne prend pas de place au sol et se voit de partout
            var pos = Around(angle, HubLayout.CabinRadius - 0.3f);
            var root = new GameObject("Caisse").transform;
            root.SetParent(env, false);
            root.SetPositionAndRotation(pos, Quaternion.Euler(0, angle, 0));     // +Z local = vers l'extérieur

            var panel = new GameObject("Panneau").transform;
            panel.SetParent(root, false);
            panel.localPosition = new Vector3(0, 2.3f, 0);
            var frame = Visuals.Box("Cadre", panel, Vector3.zero, new Vector3(0.9f, 0.5f, 0.06f), Wood);
            Visuals.Box("Fond", panel, new Vector3(0, 0, -0.035f), new Vector3(0.8f, 0.4f, 0.02f), new Color(0.12f, 0.1f, 0.08f));
            var title = Visuals.Label(panel, "CAISSE", new Vector3(0, 0.12f, -0.06f), 0.07f, new Color(0.9f, 0.85f, 0.7f));
            var amount = Visuals.Label(panel, "0", new Vector3(0, -0.05f, -0.06f), 0.2f, new Color(1f, 0.82f, 0.2f));
            // Panneau fixe (pas de billboard) : c'est le panneau entier qui fait face au joueur (texte lisible vers +Z local)
            Object.DestroyImmediate(title.GetComponent<Billboard>());
            Object.DestroyImmediate(amount.GetComponent<Billboard>());

            var board = root.gameObject.AddComponent<MoneyBoard>();
            board.amount = amount;
            board.panel = panel;
            board.frame = frame.GetComponent<ColorTint>();
        }

        // Panneau d'amélioration du bananier : 3 gros boutons ronds à enfoncer (production, fraîcheur, valeur).
        static void BuildUpgradePanel(Transform env, Bananier bananier, float angle)
        {
            var pos = Around(angle, Ring - 0.45f);
            var root = new GameObject("Ameliorations bananier").transform;
            root.SetParent(env, false);
            root.SetPositionAndRotation(pos, Quaternion.Euler(0, angle, 0));     // +Z local = vers l'extérieur

            Visuals.Solid("Pupitre", root, new Vector3(0, 0.45f, 0.05f), new Vector3(1.6f, 0.9f, 0.3f), Wood);
            Visuals.Solid("Fronton", root, new Vector3(0, 1.5f, 0.18f), new Vector3(1.6f, 0.7f, 0.04f), Wood);
            var title = Visuals.Label(root, "BANANIER", new Vector3(0, 1.75f, 0.14f), 0.09f, new Color(1f, 0.9f, 0.4f));
            Object.DestroyImmediate(title.GetComponent<Billboard>());

            var stats = new[] { BananaStat.Frequence, BananaStat.Pourriture, BananaStat.Valeur };
            for (int i = 0; i < stats.Length; i++)
            {
                float x = (i - 1) * 0.5f;
                var button = new GameObject($"Bouton {stats[i]}").transform;
                button.SetParent(root, false);
                button.localPosition = new Vector3(x, 0.9f, 0.05f);
                button.gameObject.tag = Tags.Bouton;
                var col = button.gameObject.AddComponent<BoxCollider>();
                col.size = new Vector3(0.24f, 0.14f, 0.24f);
                col.center = new Vector3(0, 0.05f, 0);

                var socle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                socle.name = "Socle";
                Object.DestroyImmediate(socle.GetComponent<Collider>());
                socle.transform.SetParent(button, false);
                socle.transform.localPosition = new Vector3(0, 0.02f, 0);
                socle.transform.localScale = new Vector3(0.22f, 0.02f, 0.22f);
                socle.AddComponent<ColorTint>().Set(new Color(0.15f, 0.15f, 0.17f));

                var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cap.name = "Bouton";
                Object.DestroyImmediate(cap.GetComponent<Collider>());
                cap.transform.SetParent(button, false);
                cap.transform.localPosition = new Vector3(0, 0.07f, 0);
                cap.transform.localScale = new Vector3(0.16f, 0.035f, 0.16f);
                cap.AddComponent<ColorTint>().Set(new Color(0.25f, 0.85f, 0.35f));

                var label = Visuals.Label(root, "", new Vector3(x, 1.42f, 0.14f), 0.065f, Color.white);
                Object.DestroyImmediate(label.GetComponent<Billboard>());

                var up = button.gameObject.AddComponent<UpgradeButton>();
                button.gameObject.AddComponent<RayPress>();
                up.bananier = bananier;
                up.stat = stats[i];
                up.cap = cap.transform;
                up.label = label;
            }
        }

        // Le coffre de Nicolas (modèle .glb animé + roulette + texte [E]), monté comme dans son menu SAE501 → 2,
        // branché sur notre joueur (ChestClickable) et sur l'argent commun (EconomyBridge).
        static void BuildChest(Transform env, Vector3 pos, Transform player)
        {
            const string ChestModelPath = "Assets/_Project/Art/Chest/chest_cartoon_animations.glb";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ChestModelPath);
            if (!model) { Debug.LogWarning("Hub : modèle du coffre introuvable (glTFast installé ?), coffre non placé."); return; }

            var chest = (GameObject)PrefabUtility.InstantiatePrefab(model);
            chest.name = "Coffre";
            chest.transform.SetParent(env, false);
            var b = Bounds(chest);
            chest.transform.localScale *= 0.8f / Mathf.Max(b.size.x, b.size.z);    // ~0,8 m de large
            // tourné vers le joueur (au centre), posé au sol
            chest.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(new Vector3(-pos.x, 0, -pos.z)));
            b = Bounds(chest);
            chest.transform.position += new Vector3(pos.x - b.center.x, -b.min.y, pos.z - b.center.z);
            float top = Bounds(chest).max.y;


            var rouletteGo = new GameObject("Roulette");
            rouletteGo.transform.SetParent(env, false);
            rouletteGo.transform.position = new Vector3(pos.x, top + 1.0f, pos.z);
            rouletteGo.AddComponent<Sae501.Coffres.Billboard>();
            var roulette = rouletteGo.AddComponent<Sae501.Coffres.RouletteView>();

            var promptGo = new GameObject("PromptCoffre");
            promptGo.transform.SetParent(env, false);
            promptGo.transform.position = new Vector3(pos.x, top + 0.35f, pos.z);
            promptGo.AddComponent<Sae501.Coffres.Billboard>();
            var prompt = promptGo.AddComponent<Sae501.Coffres.ChestPrompt>();

            var controller = chest.AddComponent<Sae501.Coffres.ChestController>();

            controller.roulette = roulette;
            controller.prompt = prompt;
            prompt.chest = controller;
            prompt.player = player;
            prompt.keyLabel = "Touche";   // « [Touche]  Ouvrir le coffre » : on l'ouvre avec la main
            chest.AddComponent<ChestClickable>().chest = controller;
            // Le modèle .glb n'a pas de collider : on en met un autour, pour le toucher ou le viser avec le rayon
            var chestBounds = Bounds(chest);
            var box = chest.AddComponent<BoxCollider>();
            box.center = chest.transform.InverseTransformPoint(chestBounds.center);
            var s = chest.transform.lossyScale;
            box.size = new Vector3(chestBounds.size.x / s.x, chestBounds.size.y / s.y, chestBounds.size.z / s.z);
            chest.AddComponent<RayPress>();
            // Les singes gagnés sortent du coffre avec leur aura et vont se ranger dans la bibliothèque
            var reward = chest.AddComponent<ChestReward>();
            reward.chest = controller;

            // Le coffre se tourne toujours vers le joueur. L'origine du modèle n'est pas au centre du coffre :
            // on le met dans un pivot placé au centre, et c'est le pivot qui tourne.
            var pivot = new GameObject("Coffre (pivot)").transform;
            pivot.SetParent(env, false);
            pivot.SetPositionAndRotation(new Vector3(pos.x, 0, pos.z), chest.transform.rotation);
            chest.transform.SetParent(pivot, true);
            pivot.gameObject.AddComponent<FacePlayer>();

            // Panneau des chances, à côté du coffre (il tourne avec lui, donc reste face au joueur)
            var oddsRoot = new GameObject("Chances du coffre").transform;
            oddsRoot.SetParent(pivot, false);
            oddsRoot.localPosition = new Vector3(1.0f, 1.35f, 0f);
            oddsRoot.localRotation = Quaternion.Euler(0, 180, 0);   // le pivot regarde le joueur : on retourne le texte pour qu'il soit lisible
            Visuals.Box("Fond", oddsRoot, new Vector3(0, 0, 0.02f), new Vector3(1.15f, 1.5f, 0.02f), new Color(0.1f, 0.09f, 0.08f));
            var oddsText = Visuals.Label(oddsRoot, "", Vector3.zero, 0.055f);
            Object.DestroyImmediate(oddsText.GetComponent<Billboard>());
            var oddsPanel = oddsRoot.gameObject.AddComponent<ChestOddsPanel>();
            oddsPanel.chest = controller;
            oddsPanel.text = oddsText;

            // Prix du coffre, toujours visible au-dessus (doré si on peut payer)
            var tag = new GameObject("Prix du coffre");
            tag.transform.SetParent(env, false);
            tag.transform.position = new Vector3(pos.x, top + 0.75f, pos.z);
            var priceTag = tag.AddComponent<ChestPriceTag>();
            priceTag.chest = controller;
            priceTag.label = Visuals.Label(tag.transform, "", Vector3.zero, 0.09f);
        }

        static Bounds Bounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        // Grille 8x8 en dalles (sans collider sur le plateau, avec collider sur la carte pour marcher).
        static void BuildGrid(Transform parent, float tile, float thickness, bool walkable)
        {
            float k = tile / MapLayout.Tile;
            for (int r = 0; r < MapLayout.Size; r++)
                for (int c = 0; c < MapLayout.Size; c++)
                {
                    char ch = MapLayout.At(r, c);
                    Color color = ch == 'S' ? new Color(0.3f, 0.9f, 0.4f)
                        : ch == 'E' ? new Color(0.9f, 0.25f, 0.25f)
                        : ch == '#' ? PathColor
                        : (r + c) % 2 == 0 ? Grass1 : Grass2;

                    var size = new Vector3(tile, thickness, tile);
                    var pos = MapLayout.CellLocal(r, c) * k + new Vector3(0, -thickness / 2f, 0);
                    var cell = walkable ? Visuals.Solid($"Case {r},{c}", parent, pos, size, color)
                                        : Visuals.Box($"Case {r},{c}", parent, pos, size, color);
                    cell.tag = ch == '.' ? Tags.Terrain : Tags.Piste;
                }
        }

        // ---------------- CARTE ----------------
        static Transform BuildMap(Transform hubSpawn)
        {
            var map = new GameObject("Carte");
            map.transform.position = MapCenter;
            BuildGrid(map.transform, MapLayout.Tile, 0.5f, true);
            map.AddComponent<TowerManager>();
            map.AddComponent<PlacementSurface>().scale = 1f; // prendre / poser / fusionner directement sur la carte
            var spawner = map.AddComponent<WaveSpawner>();

            float edge = MapLayout.HalfExtent;
            Teleportable(Visuals.Solid("Estrade", map.transform, new Vector3(0, -0.25f, -edge - 2.5f), new Vector3(8, 0.5f, 5), Floor));
            Teleportable(Visuals.Solid("Sol autour", map.transform, new Vector3(0, -0.6f, 0), new Vector3(edge * 2 + 20, 0.1f, edge * 2 + 20), Floor));

            MakeActionCube(map.transform, "Retour hub", new Vector3(3f, 0.9f, -edge - 2f), 0.6f, new Color(0.3f, 0.5f, 1f),
                "HUB", ActionCube.Action.Teleport, hubSpawn);
            MakeLaunchCube(map.transform, new Vector3(-3f, 0.9f, -edge - 2f), 0.6f, spawner);
            BuildWaveBoard(map.transform, map.transform.position + new Vector3(0, 2.4f, -edge - 0.3f), Quaternion.identity, spawner);
            return map.transform;
        }
    }
}
