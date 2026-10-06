using System.IO;
using System.Linq;
using System.Reflection;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
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
        const float DesktopBowScale = 0.5f;   // en mode PC, l'arc est collé à la caméra : plus petit
        static readonly Vector3 MapCenter = new Vector3(0f, 0f, 40f);

        static readonly Color Floor = new Color(0.35f, 0.35f, 0.38f);
        static readonly Color Wood = new Color(0.45f, 0.30f, 0.18f);
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

            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var defaultCam = GameObject.FindWithTag("MainCamera");
            if (defaultCam) Object.DestroyImmediate(defaultCam); // la caméra est celle du joueur

            var hubSpawn = Spawn("Spawn Hub", Vector3.zero);
            var mapSpawn = Spawn("Spawn Carte", MapCenter + new Vector3(0, 0, -MapLayout.HalfExtent - 3f));

            var mapRoot = BuildMap(hubSpawn);
            var spawner = mapRoot.GetComponent<WaveSpawner>();
            var hub = BuildHub(mapRoot, mapSpawn, spawner);   // avant le joueur : l'installeur des bananes ajoute son TestSouris à Camera.main s'il en trouve une
            var player = Player(hubSpawn.position, mapSpawn);
            if (!player) return;
            BuildChest(hub, Around(132f, Ring - 0.3f), player.head);
            BuildPlayerMode(player.gameObject, DesktopPlayerObject(hubSpawn.position, mapSpawn));

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
        static GameObject DesktopPlayerObject(Vector3 position, Transform mapSpawn)
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

            // L'arc, en bas à droite de la vue, plus petit qu'en VR pour ne pas cacher l'écran (clic droit : tirer)
            var bow = ((GameObject)PrefabUtility.InstantiatePrefab(BowSetup.Setup().gameObject)).GetComponent<Bow>();
            bow.transform.localScale *= DesktopBowScale;
            bow.maxDraw *= DesktopBowScale;
            bow.HoldIn(cam.transform, new Vector3(0.3f, -0.3f, 0.7f));
            var archer = player.AddComponent<DesktopArcher>();
            archer.bow = bow;
            archer.aim = cam.transform;
            GiveHolster(player, bow, mapSpawn);
            player.AddComponent<MonkeyInfoCard>();
            return player;
        }

        static PlayerRig Player(Vector3 position, Transform mapSpawn)
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
            GiveBow(go, rig, mapSpawn);
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

        // L'arc de Quincy dans la main gauche ; on tire la corde avec la main droite (grip). Rangé au hub (BowHolster).
        static void GiveBow(GameObject player, PlayerRig rig, Transform mapSpawn)
        {
            if (!rig.leftHand || !rig.rightHand) { Debug.LogWarning("Joueur VR : manette introuvable, pas d'arc."); return; }
            var bow = ((GameObject)PrefabUtility.InstantiatePrefab(BowSetup.Setup().gameObject)).GetComponent<Bow>();
            bow.HoldIn(rig.leftHand, Vector3.zero);
            var archer = player.AddComponent<VRArcher>();
            archer.bow = bow;
            archer.drawHand = rig.rightHand;
            archer.drawGrip = new InputActionProperty(InputReference("XRI Right Interaction/Select Value"));
            GiveHolster(player, bow, mapSpawn);
        }

        static void GiveHolster(GameObject player, Bow bow, Transform mapSpawn)
        {
            var holster = player.AddComponent<BowHolster>();
            holster.bow = bow;
            holster.mapSpawn = mapSpawn;
        }

        // Une action des contrôles XRI des Starter Assets (même chose que « Use Reference » dans l'Inspector).
        static InputActionReference InputReference(string name)
        {
            var path = VRSetup.SamplesFolder + "/Starter Assets/XRI Default Input Actions.inputactions";
            var reference = AssetDatabase.LoadAllAssetsAtPath(path).OfType<InputActionReference>().FirstOrDefault(r => r.name == name);
            if (!reference) Debug.LogWarning("Action introuvable : " + name);
            return reference;
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
        // Tout ce qui compte est disposé EN ROND autour du joueur (point d'apparition au centre, regard vers +Z),
        // chaque élément tourné vers lui :
        //   devant (0°)           : le plateau incliné (la carte en direct), reculé sur le cercle,
        //                           avec LANCER (-32°), JOUER (-20°) et Vider (+20°) de part et d'autre,
        //                           et le tableau de la vague au-dessus
        //   côtés (±72°)          : les deux meubles de la bibliothèque, courbés le long du cercle
        //   derrière (180°)       : le bananier de Maxens (agrandi), la table des bananes devant lui (-172°),
        //                           le panier sur son socle (-157°) et la caisse (-140°), le panneau d'amélioration (160°)
        //   derrière (132°)       : le coffre de Nicolas, avec son prix au-dessus et le panneau des chances
        const float Ring = 5.0f;          // rayon du cercle : tout est posé dessus, le centre reste libre pour circuler
        const float SlotSize = 0.24f;     // ancienne taille ×1,1
        const float SlotStepX = 0.36f;    // espace entre deux raretés
        const float SlotStepY = 0.40f;    // espace entre deux étagères
        const float FirstShelfY = 0.45f;  // rangée la plus basse (accessible à un petit joueur)
        const float TreeScale = 1.6f;     // le palmier de Maxens, agrandi

        // Position sur le cercle. angle 0 = devant, positif = à droite.
        static Vector3 Around(float angleDeg, float radius, float height = 0f)
        {
            float a = angleDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(a) * radius, height, Mathf.Cos(a) * radius);
        }

        static Transform BuildHub(Transform mapRoot, Transform mapSpawn, WaveSpawner spawner)
        {
            var env = new GameObject("Hub").transform;
            Teleportable(Visuals.Solid("Sol", env, new Vector3(0, -0.05f, 0), new Vector3(15, 0.1f, 15), Floor));

            // Devant, sur le cercle : le plateau, avec JOUER et Vider de part et d'autre
            BuildBoard(env, mapRoot);
            MakeActionCube(env, "Jouer", Around(-20f, Ring - 0.5f, 1.0f), 0.35f, new Color(0.2f, 0.85f, 0.3f),
                "JOUER", ActionCube.Action.Teleport, mapSpawn);
            MakeActionCube(env, "Vider", Around(20f, Ring - 0.5f, 0.9f), 0.25f, new Color(0.6f, 0.6f, 0.6f),
                "Vider", ActionCube.Action.ClearBoard, null);
            // LANCER à côté de JOUER (on peut lancer depuis le hub et regarder la vague sur le plateau),
            // et le tableau de la vague au-dessus du plateau
            MakeLaunchCube(env, Around(-32f, Ring - 0.5f, 1.0f), 0.3f, spawner);
            BuildWaveBoard(env, Around(0f, Ring + 0.1f, 2.1f), Quaternion.identity, spawner);

            // Sur les côtés : les deux meubles de la bibliothèque, qui suivent le cercle (4 types + 3 types)
            BuildShelf(env, "Bibliotheque gauche", -72f, 0, 4);
            BuildShelf(env, "Bibliotheque droite", 72f, 4, 3);

            // Derrière : le bananier sur le cercle, la table des bananes devant lui, le panier et la caisse d'un côté,
            // le panneau d'amélioration de l'autre (le coffre est placé après le joueur, derrière-droite)
            var bananier = BuildBananas(env, Around(180f, Ring + 0.7f), 180f);
            BuildMoneyBoard(env, -140f);
            if (bananier) BuildUpgradePanel(env, bananier, 160f);
            return env;
        }

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
            var tablePos = Around(-172f, Ring - 0.5f);
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
                var basketPos = Around(-157f, Ring - 0.5f);
                Visuals.Solid("Socle du panier", env, basketPos + Vector3.up * (HandHeight / 2f), new Vector3(0.5f, HandHeight, 0.5f), Wood);
                basket.position = basketPos + Vector3.up * HandHeight;
            }
            return bananier;
        }

        // La caisse : un panneau en bois avec l'argent total en gros chiffres dorés, à côté du panier.
        static void BuildMoneyBoard(Transform env, float angle)
        {
            var pos = Around(angle, Ring - 0.4f);
            var root = new GameObject("Caisse").transform;
            root.SetParent(env, false);
            root.SetPositionAndRotation(pos, Quaternion.Euler(0, angle, 0));     // +Z local = vers l'extérieur

            Visuals.Solid("Poteau", root, new Vector3(0, 0.75f, 0.05f), new Vector3(0.08f, 1.5f, 0.08f), Wood);
            var panel = new GameObject("Panneau").transform;
            panel.SetParent(root, false);
            panel.localPosition = new Vector3(0, 1.6f, 0);
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
