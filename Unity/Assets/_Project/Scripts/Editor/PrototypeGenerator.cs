using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace SAE.EditorTools
{
    // Menu SAE → Générer le prototype : construit les DEUX scènes du jeu (relancer le menu les écrase).
    //   Hub.unity        : la cabane (bananier, coffre, bibliothèque, plateau) ; c'est la scène de départ.
    //   Labyrinthe.unity : la carte et ses vagues, où l'on défend avec l'arc (à 1 km du hub).
    // Le hub charge aussi le labyrinthe (LevelLoader) : les deux tournent ensemble, une vague continue quand on est au hub.
    // Un seul joueur (VR et PC), dans le hub : XRI ne gère bien qu'un joueur VR. SE TP / HUB le déplacent d'une scène
    // à l'autre ; le soleil et les réglages d'image de chaque scène ne s'allument que quand on y est (voir Levels).
    public static class PrototypeGenerator
    {
        const string Folder = "Assets/_Project/Scenes";
        const string HubScenePath = Folder + "/Hub.unity";
        const string MapScenePath = Folder + "/Labyrinthe.unity";   // même nom que Levels.MapScene
        const string OldScenePath = Folder + "/Jeu.unity";          // l'ancienne scène unique, supprimée à la génération
        const float BoardTile = 0.2f;    // plateau de 1,6 m : l'élément principal du hub
        const float HandHeight = 0.9f;   // table des bananes et socle du panier : à hauteur de main, pas au sol
        const int IgnoreRaycast = 2;     // couche Unity « Ignore Raycast »
        const float ChestFloor = 0.3f;         // hauteur du double fond du coffre (FLOOR_Y dans Blender/coffre.py)
        const float ChestBananaScale = 0.8f;   // les bananes du bananier, un peu plus petites dans le coffre
        const float DesktopBowScale = 0.5f;   // en mode PC, l'arc est collé à la caméra : plus petit
        static readonly Vector3 HandOffset = new Vector3(0f, -0.01f, -0.06f);   // la paume, un peu derrière l'avant de la manette
        static readonly Vector3 BowInHand = new Vector3(0.07f, 0f, 0.02f);      // la poignée de l'arc, sur le côté intérieur de la main : la flèche passe à côté
        const float HandTilt = 35f;   // les mains tournées pouce vers le haut, comme quand on tient les manettes (pas paume à plat)
        static readonly Vector3 MapCenter = new Vector3(0f, 0f, 1000f);   // les deux scènes sont chargées ensemble : la carte est loin, hors de vue du hub

        static readonly Color Floor = new Color(0.35f, 0.35f, 0.38f);
        static readonly Color Wood = new Color(0.45f, 0.30f, 0.18f);
        static readonly Color DarkWood = new Color(0.30f, 0.19f, 0.10f);   // plateaux, plinthes, enseignes : un ton plus foncé, ça détache les pièces
        static readonly Color Slate = new Color(0.13f, 0.17f, 0.15f);      // les ardoises des tableaux
        static readonly Color Chalk = new Color(0.95f, 0.94f, 0.88f);      // texte « à la craie »
        static readonly Color TitleGold = new Color(1f, 0.83f, 0.35f);     // titres des enseignes
        static readonly Color Engraved = new Color(0.22f, 0.13f, 0.06f);   // texte foncé sur les plaques claires de la bibliothèque
        static readonly Color Parchment = new Color(0.93f, 0.86f, 0.68f);  // plaques claires de la bibliothèque : texte foncé, bien lisible
        static readonly Color Leaf = new Color(0.27f, 0.50f, 0.18f);       // la feuille de bananier posée sur l'étal
        static readonly Color LaunchColor = new Color(0.95f, 0.45f, 0.15f);
        static readonly Color PlayColor = new Color(0.2f, 0.8f, 0.3f);
        static readonly Color ClearColor = new Color(0.55f, 0.6f, 0.7f);
        static readonly Color HubColor = new Color(0.3f, 0.5f, 1f);
        static readonly Color Grass1 = new Color(0.40f, 0.68f, 0.25f);   // la pelouse du plateau du hub, proche de la prairie
        static readonly Color Iron = new Color(0.18f, 0.18f, 0.2f);        // la cage des lanternes de l'estrade
        static readonly Color FlameColor = new Color(1f, 0.72f, 0.25f);    // leur flamme

        [MenuItem("SAE/Générer le prototype")]
        public static void Generate()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Folder);
            TagSetup.EnsureTags();
            VRSetup.Configure();
            CabinArt.Build();   // la texture de bois des meubles du hub (la cabane elle-même vient de Blender)
            EditorTools.MonkeyVisualsSetup.Setup();   // les modèles des singes et de leurs projectiles, toujours à jour

            // 1. Le labyrinthe
            NewLevelScene();
            // L'ancienne scène unique (hub et carte ensemble) est remplacée par Hub + Labyrinthe : on la supprime
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(OldScenePath)) AssetDatabase.DeleteAsset(OldScenePath);
            var mapSpawn = Spawn("Spawn Carte", MapCenter + new Vector3(0, 0, -MapLayout.HalfExtent - 3f), Level.Carte);
            BuildMap();
            BuildPresence(Level.Carte, mapSpawn);   // pas de joueur ici : c'est celui du hub qui vient
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), MapScenePath);

            // 2. Le hub
            NewLevelScene();
            var hubSpawn = Spawn("Spawn Hub", Vector3.zero, Level.Hub);
            var hub = BuildHub();   // avant le joueur : l'installeur des bananes ajoute son TestSouris à Camera.main s'il en trouve une
            var player = BuildPlayers(hubSpawn.position);
            if (!player) return;
            BuildChest(hub, Around(97f, Ring - 0.2f));   // estrade de 1,4 m (le coffre de Maxens) : un peu plus près de VIDER, loin du comptoir du bananier
            UseWoodTexture(hub);   // encore une fois : l'estrade et le cadre du coffre sont posés après le reste du hub
            BuildPresence(Level.Hub, hubSpawn);
            new GameObject("Chargement du labyrinthe").AddComponent<LevelLoader>();

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), HubScenePath);
            // Le hub en premier : c'est la scène de départ du jeu
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(HubScenePath, true), new EditorBuildSettingsScene(MapScenePath, true) };
            PlayerModeMenu.Apply();   // VR ou PC, selon le menu SAE → Mode de jeu
            Debug.Log("Prototype généré : " + HubScenePath + " et " + MapScenePath);
        }

        // La « présence » d'un niveau : ce qui n'est allumé que quand on y est (le soleil, les réglages d'image),
        // rangé sous un seul objet ÉTEINT ; LevelPresence l'allume si c'est le niveau en cours.
        static void BuildPresence(Level level, Transform spawn)
        {
            var presence = new GameObject("Soleil et réglages du niveau");
            var parts = new[]
            {
                Object.FindFirstObjectByType<Light>() ? Object.FindFirstObjectByType<Light>().gameObject : null,
                Object.FindFirstObjectByType<Volume>() ? Object.FindFirstObjectByType<Volume>().gameObject : null,
            };
            foreach (var part in parts)
                if (part) part.transform.SetParent(presence.transform, true);
            presence.SetActive(false);

            var root = new GameObject(level == Level.Hub ? "Niveau Hub" : "Niveau Carte").AddComponent<LevelPresence>();
            root.level = level;
            root.presence = presence;
            root.spawn = spawn;
        }

        // Une scène vide avec sa lumière (le soleil) et la brume légère au loin ; la caméra est celle du joueur
        static void NewLevelScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            SetUpLighting();
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 60f;
            RenderSettings.fogEndDistance = 230f;
            RenderSettings.fogColor = new Color(0.72f, 0.82f, 0.93f);
            var defaultCam = GameObject.FindWithTag("MainCamera");
            if (defaultCam) Object.DestroyImmediate(defaultCam);
        }

        // ---------------- LUMIÈRE ----------------
        // Pas de faux rayons dessinés (ils faisaient artificiel) : la vraie lumière du soleil et ses ombres font des taches
        // de soleil sur le plancher derrière les fenêtres, des poussières dorées flottent dans le soleil, et un réglage
        // de l'image (tons, couleurs, léger halo) donne une ambiance chaude de fin d'après-midi.
        // Le soleil vient de l'arrière-droite du hub (150°), assez haut : il entre par la porte et deux fenêtres.
        static readonly Quaternion SunRotation = Quaternion.Euler(50f, -30f, 0f);
        static Vector3 SunDirection => SunRotation * Vector3.forward;   // le sens où va la lumière
        static readonly Color SunColor = new Color(1f, 0.93f, 0.8f);
        const string LightingProfilePath = "Assets/_Project/Art/Lumiere.asset";
        const string DustMaterialPath = "Assets/_Project/Art/Cabane/Poussiere.mat";

        // La lumière d'une scène : le soleil (ombres douces), une lumière ambiante en trois tons (ciel bleuté,
        // horizon neutre, sol chaud) et un volume de réglages de l'image
        static void SetUpLighting()
        {
            var sun = Object.FindFirstObjectByType<Light>();
            if (sun)
            {
                sun.name = "Soleil";
                sun.transform.rotation = SunRotation;
                sun.color = SunColor;
                sun.intensity = 1.45f;   // un peu plus fort (7 oct.) : des taches de soleil plus nettes sur le plancher
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.85f;   // les ombres restent un peu éclairées par le ciel
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            // Lumière ambiante un peu plus basse qu'avant (7 oct.) : la cabane était éclairée partout pareil, toute plate ;
            // ce sont maintenant le lustre, les lanternes et le soleil qui font les zones claires et sombres.
            RenderSettings.ambientSkyColor = new Color(0.5f, 0.6f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.44f, 0.42f, 0.37f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.24f, 0.17f);

            var volume = new GameObject("Réglages de l'image").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = LightingProfile();
            volume.gameObject.AddComponent<MobileLighting>();   // sur le casque : sans le halo (trop coûteux)
        }

        // Les réglages de l'image, enregistrés dans un fichier (Art/Lumiere.asset) pour pouvoir les retoucher dans l'Inspector
        static VolumeProfile LightingProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(LightingProfilePath);
            if (profile) return profile;
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, LightingProfilePath);
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.value = TonemappingMode.Neutral;                 // des couleurs naturelles, sans blanc brûlé
            var colors = profile.Add<ColorAdjustments>(true);
            colors.postExposure.value = 0.15f;
            colors.contrast.value = 12f;
            colors.saturation.value = 15f;                             // un peu plus vif, comme un dessin animé
            var white = profile.Add<WhiteBalance>(true);
            white.temperature.value = 10f;                             // légèrement chaud
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.value = 0.9f;                              // seules les choses très claires brillent (soleil, flammes)
            bloom.intensity.value = 0.35f;
            bloom.scatter.value = 0.6f;
            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        // La caméra d'un joueur applique les réglages de l'image
        static void UsePostProcessing(Camera camera) => camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;

        // Le matériau des poussières : notre shader transparent, avec la petite tache ronde et floue des particules de Unity
        static Material DustMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(DustMaterialPath);
            if (mat) return mat;
            mat = new Material(Shader.Find("SAE/Texte 3D")) { name = "Poussiere" };
            mat.mainTexture = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
            AssetDatabase.CreateAsset(mat, DustMaterialPath);
            return mat;
        }

        // Des poussières dorées qui flottent lentement dans le soleil, devant chaque fenêtre éclairée (et la porte) :
        // c'est ce qui rend un rayon de soleil visible dans une vraie pièce.
        static readonly float[] CabinWindows = { 60f, 150f, 210f };   // mêmes valeurs que WINDOWS dans cabane.py
        const float WindowBottom = 1.72f, WindowTop = 2.42f, WindowWidth = 0.9f, DoorWidth = 1.3f;

        static void BuildSunDust(Transform env)
        {
            var root = new GameObject("Poussières dans le soleil").transform;
            root.SetParent(env, false);
            var toSun = -new Vector3(SunDirection.x, 0f, SunDirection.z).normalized;
            void Dust(string name, float angle, float width, float bottom, float top)
            {
                if (Vector3.Dot(Around(angle, 1f), toSun) < 0.3f) return;   // ce mur est à l'ombre
                var opening = Around(angle, HubLayout.CabinRadius, (bottom + top) / 2f);
                const float Depth = 1.6f;                                   // sur 1,6 m dans le rayon
                var go = new GameObject(name);
                go.transform.SetParent(root, false);
                go.transform.SetPositionAndRotation(opening + SunDirection * (Depth / 2f), Quaternion.LookRotation(SunDirection));

                var ps = go.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.startLifetime = 8f;
                main.startSpeed = 0f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.016f);
                main.startColor = new Color(1f, 0.9f, 0.65f, 0.7f);
                main.maxParticles = 50;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.prewarm = true;                                        // déjà là quand on arrive
                var emission = ps.emission;
                emission.rateOverTime = 6f;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(width, top - bottom, Depth);     // le volume du rayon, dans son axe
                var noise = ps.noise;                                       // elles dérivent doucement, au hasard
                noise.enabled = true;
                noise.strength = 0.03f;
                noise.frequency = 0.3f;
                var fade = ps.colorOverLifetime;                            // elles apparaissent et disparaissent en douceur
                fade.enabled = true;
                var gradient = new Gradient();
                gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                                 new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
                fade.color = gradient;
                var r = go.GetComponent<ParticleSystemRenderer>();
                r.sharedMaterial = DustMaterial();
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
            foreach (var angle in CabinWindows) Dust($"Poussières fenêtre {angle}", angle, WindowWidth, WindowBottom, WindowTop);
            Dust("Poussières porte", 180f, DoorWidth, 0.3f, CabinDoorHeight);
        }

        // Le joueur, en deux versions (VR et PC, PlayerMode active la bonne), au point d'arrivée du hub.
        // Il a l'arc de Quincy, qui ne sort que sur la carte (BowHolster). Renvoie le joueur VR (null si les Starter Assets manquent).
        static PlayerRig BuildPlayers(Vector3 position)
        {
            var player = Player(position);
            if (!player) return null;
            var pcPlayer = DesktopPlayerObject(position);
            player.gameObject.AddComponent<FallGuard>();          // tombé dans le vide : retour au point d'arrivée
            player.gameObject.AddComponent<WaveShortcut>();       // B (manette droite) : lancer la vague
            pcPlayer.AddComponent<FallGuard>();
            BuildPlayerMode(player.gameObject, pcPlayer);
            return player;
        }

        // Distance d'affichage de la caméra : assez loin pour les montagnes (la brume finit à 230 m), mais pas plus.
        // Une plage courte (près 3 cm, loin 400 m) rend la profondeur plus précise : moins de surfaces qui
        // « clignotent » l'une sur l'autre selon l'angle (les bûches sur les fenêtres, par exemple).
        const float NearClip = 0.03f, FarClip = 400f;

        // Le point d'arrivée de la scène (le joueur y est posé, FallGuard l'y remet après une chute)
        static Transform Spawn(string name, Vector3 pos, Level level)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            go.AddComponent<LevelSpawn>().level = level;
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

            var cam = new GameObject("Camera");
            cam.tag = "MainCamera";
            cam.transform.SetParent(player.transform, false);
            cam.transform.localPosition = new Vector3(0, 1.6f, 0);
            var camera = cam.AddComponent<Camera>();
            camera.nearClipPlane = NearClip;
            camera.farClipPlane = FarClip;
            UsePostProcessing(camera);
            cam.AddComponent<AudioListener>();

            player.AddComponent<DesktopPlayer>();
            var rig = player.AddComponent<PlayerRig>();
            rig.head = rig.leftHand = rig.rightHand = cam.transform;

            // L'arc (sur la carte), en bas à droite de la vue, plus petit qu'en VR pour ne pas cacher l'écran (clic droit : tirer)
            var bow = ((GameObject)PrefabUtility.InstantiatePrefab(BowSetup.Setup().gameObject)).GetComponent<Bow>();
            bow.transform.localScale *= DesktopBowScale;
            bow.maxDraw *= DesktopBowScale;
            bow.HoldIn(cam.transform, new Vector3(0.3f, -0.3f, 0.7f));
            var archer = player.AddComponent<DesktopArcher>();
            archer.bow = bow;
            archer.aim = cam.transform;
            player.AddComponent<BowHolster>().bow = bow;
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
            origin.Camera.nearClipPlane = NearClip;
            origin.Camera.farClipPlane = FarClip;
            UsePostProcessing(origin.Camera);

            // Confort : pas de déplacement continu (il donne la nausée). Les deux sticks téléportent,
            // le stick droit tourne par crans (snap turn, réglage par défaut des Starter Assets).
            foreach (var manager in go.GetComponentsInChildren<ControllerInputActionManager>(true))
            {
                var so = new SerializedObject(manager);
                so.FindProperty("m_SmoothMotionEnabled").boolValue = false;
                so.FindProperty("m_SmoothTurnEnabled").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Pas de saut : on se déplace en se téléportant (le saut des Starter Assets secoue la vue, règle de confort)
            var jump = FindChild(go.transform, "Jump");
            if (jump) jump.gameObject.SetActive(false);

            ShowRays(go);

            var rig = go.AddComponent<PlayerRig>();
            rig.head = origin.Camera.transform;
            rig.leftHand = FindChild(go.transform, "Left Controller");
            rig.rightHand = FindChild(go.transform, "Right Controller");
            AddFingertip(rig.leftHand);
            AddFingertip(rig.rightHand);
            GiveHands(rig, out var leftHand, out var rightHand);
            GiveBow(go, rig, leftHand, rightHand);
            if (rig.leftHand) rig.leftHand.gameObject.AddComponent<FpsCounter>();   // les fps au poignet (éditeur et builds de dev)
            go.AddComponent<MonkeyInfoCard>();   // fiche du singe visé, dans le décor
            return rig;
        }

        // Les mains de Quincy à la place des modèles de manettes : elles se ferment avec le grip et la gâchette.
        // Renvoie la main gauche (celle qui tiendra l'arc).
        static void GiveHands(PlayerRig rig, out AnimateHandOnInput left, out AnimateHandOnInput right)
        {
            left = right = null;
            if (!rig.leftHand || !rig.rightHand) return;
            if (!HandSetup.Left || !HandSetup.Right) HandSetup.Setup();   // refaits seulement s'ils manquent (menu SAE → Préparer les mains)
            right = PutHand(HandSetup.Right, rig.rightHand, "Right");
            left = PutHand(HandSetup.Left, rig.leftHand, "Left");
        }

        static AnimateHandOnInput PutHand(AnimateHandOnInput prefab, Transform controller, string side)
        {
            var visual = controller.Find(side + " Controller Visual");
            if (visual) visual.gameObject.SetActive(false);   // on cache la manette blanche des Starter Assets
            var hand = ((GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject)).GetComponent<AnimateHandOnInput>();
            hand.transform.SetParent(controller, false);
            hand.transform.localPosition = HandOffset;
            // Pouce relevé vers l'intérieur : la main gauche tourne dans un sens, la droite (en miroir) dans l'autre
            hand.transform.localRotation = Quaternion.Euler(0f, 0f, side == "Left" ? HandTilt : -HandTilt);
            hand.gripValue = new InputActionProperty(InputReference($"XRI {side} Interaction/Select Value"));
            hand.triggerValue = new InputActionProperty(InputReference($"XRI {side} Interaction/Activate Value"));
            return hand;
        }

        // L'arc de Quincy dans la main gauche ; on tire la corde avec la main droite (grip ou gâchette).
        // Seulement sur la carte (BowHolster) ; la main gauche se ferme dessus.
        static void GiveBow(GameObject player, PlayerRig rig, AnimateHandOnInput leftHand, AnimateHandOnInput rightHand)
        {
            if (!rig.leftHand || !rig.rightHand) { Debug.LogWarning("Joueur VR : manette introuvable, pas d'arc."); return; }
            var bow = ((GameObject)PrefabUtility.InstantiatePrefab(BowSetup.Setup().gameObject)).GetComponent<Bow>();
            bow.HoldIn(rig.leftHand, BowInHand);
            if (leftHand) leftHand.heldBow = bow;
            var archer = player.AddComponent<VRArcher>();
            archer.bow = bow;
            archer.drawHand = rig.rightHand;
            archer.drawHandVisual = rightHand;
            archer.drawGrip = new InputActionProperty(InputReference("XRI Right Interaction/Select Value"));
            archer.drawTrigger = new InputActionProperty(InputReference("XRI Right Interaction/Activate Value"));
            player.AddComponent<BowHolster>().bow = bow;
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

        // Pupitre de commande en bois (même style que la cabane) : un meuble bas, le dessus incliné vers le joueur,
        // et sur ce dessus un gros bouton rond par commande (voir ConsoleButton), son nom gravé sur une plaque juste devant.
        // Tout se lit en baissant les yeux, rien ne flotte. pos : où le poser (on garde x et z, il est posé au sol).
        // yaw : null = tourné vers le centre du hub ; sinon l'angle voulu (sur la carte, 0 = face au joueur de l'estrade).
        // Renvoie le dessus incliné, sur lequel on pose les boutons (x = 0 au milieu).
        static readonly Color Brass = new Color(0.85f, 0.65f, 0.25f);
        const float ConsoleStep = 0.36f;     // espace entre deux boutons
        const float ConsoleDepth = 0.45f;

        static Transform BuildConsole(Transform parent, string name, Vector3 pos, int buttons, float? yaw = null)
        {
            var root = new GameObject($"Pupitre {name}").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(pos.x, 0f, pos.z);
            root.localRotation = Quaternion.Euler(0, yaw ?? AngleOf(pos), 0);   // +Z local = à l'opposé du joueur

            float width = buttons * ConsoleStep + 0.12f;
            Visuals.Box("Plinthe", root, new Vector3(0, 0.035f, 0), new Vector3(width + 0.04f, 0.07f, ConsoleDepth + 0.04f), DarkWood);
            Visuals.Solid("Meuble", root, new Vector3(0, 0.45f, 0), new Vector3(width, 0.8f, ConsoleDepth), Wood);
            Visuals.Box("Panneau avant", root, new Vector3(0, 0.42f, -ConsoleDepth / 2f - 0.01f), new Vector3(width - 0.1f, 0.5f, 0.02f), DarkWood);

            // Le dessus, penché de 20° vers le joueur. C'est un bloc épais : sa partie basse rentre dans le meuble,
            // donc on ne voit pas de vide sur les côtés sous la pente.
            var top = new GameObject("Dessus incliné").transform;
            top.SetParent(root, false);
            top.localPosition = new Vector3(0, 0.93f, 0);
            top.localRotation = Quaternion.Euler(-20f, 0, 0);
            Visuals.Solid("Plateau", top, new Vector3(0, -0.125f, 0), new Vector3(width + 0.06f, 0.25f, ConsoleDepth + 0.1f), DarkWood);
            return top;
        }

        // Un bouton du pupitre : une bague en laiton, un gros bouton rond de couleur (ActionCube, qui s'enfonce avec la main
        // ou se vise avec le rayon), et une plaque en laiton gravée à son nom, couchée sur le pupitre devant lui.
        static ActionCube ConsoleButton(Transform top, float x, string label, Color color, ActionCube.Action action, Level destination = Level.Hub)
        {
            var ring = Visuals.Box("Bague", top, new Vector3(x, 0.005f, 0.07f), new Vector3(0.2f, 0.01f, 0.2f), Brass);
            ring.GetComponent<MeshFilter>().sharedMesh = Cylinder;
            var button = Visuals.Box(label, top, new Vector3(x, 0.02f, 0.07f), new Vector3(0.15f, 0.02f, 0.15f), color);
            button.GetComponent<MeshFilter>().sharedMesh = Cylinder;
            var col = button.AddComponent<BoxCollider>();
            col.size = new Vector3(1.3f, 4f, 1.3f);     // un peu plus grand que le bouton : facile à toucher du bout de la manette
            button.tag = Tags.Bouton;
            var a = button.AddComponent<ActionCube>();
            button.AddComponent<RayPress>();             // on peut aussi l'enfoncer de loin, en le visant
            a.action = action;
            a.destination = destination;

            Visuals.Box("Plaque", top, new Vector3(x, 0.004f, -0.14f), new Vector3(ConsoleStep - 0.04f, 0.008f, 0.1f), DarkWood);   // plaque foncée, texte doré : bien lisible (le texte foncé sur le laiton ne se lisait pas)
            var text = Visuals.Text(top, label, new Vector3(x, 0.012f, -0.14f), 0.075f, TitleGold, title: true);
            text.transform.localRotation = Quaternion.Euler(90f, 0, 0);   // couché sur la plaque, le haut des lettres vers le mur
            return a;
        }

        // Le maillage du cylindre, pris sur un cylindre primitif de Unity (1 m de diamètre à l'échelle 1) :
        // celui de Resources « Cylinder.fbx » sortait deux fois trop large (boutons énormes, plaques cachées dessous).
        static Mesh cylinder;
        static Mesh Cylinder
        {
            get
            {
                if (cylinder) return cylinder;
                var tmp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cylinder = tmp.GetComponent<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(tmp);
                return cylinder;
            }
        }

        // Une ardoise encadrée de bois, avec son rebord à craie : le support de tous les tableaux (vague, caisse, chances,
        // comptoirs). Le texte s'écrit du côté -Z local (vers le joueur). w, h : taille de l'ardoise, en mètres.
        static Transform BuildChalkboard(Transform parent, string name, Vector3 localPos, Quaternion localRotation, float w, float h)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = localPos;
            root.localRotation = localRotation;
            Visuals.Box("Cadre", root, Vector3.zero, new Vector3(w + 0.12f, h + 0.12f, 0.06f), Wood);
            Visuals.Box("Ardoise", root, new Vector3(0, 0, -0.035f), new Vector3(w, h, 0.02f), Slate);
            Visuals.Box("Rebord", root, new Vector3(0, -h / 2f - 0.05f, -0.05f), new Vector3(w + 0.12f, 0.03f, 0.08f), DarkWood);
            return root;
        }

        // Tableau de la vague dans le décor (vague, vies, état) : remplace l'affichage à l'écran.
        // Comme la caisse : panneau fixe, +Z local tourné à l'opposé du joueur, texte côté joueur.
        static void BuildWaveBoard(Transform parent, Vector3 localPos, Quaternion rotation)
        {
            var root = BuildChalkboard(parent, "Tableau de la vague", localPos, rotation, 1.6f, 0.7f);
            var text = Visuals.Text(root, "", new Vector3(0, 0, -0.05f), 0.1f, Chalk);
            root.gameObject.AddComponent<WaveBoard>().text = text;   // il lit les vagues (WaveSpawner.Instance), même depuis l'autre scène
        }

        // ---------------- HUB ----------------
        // Le hub est une cabane en rondins (voir BuildCabin). Tout est posé EN ROND contre les murs, tourné vers le joueur
        // qui se tient au centre, sur le tapis (point d'apparition, regard vers +Z). Cercle de 2,8 m : un ou deux pas suffisent.
        //   devant (0°)        : le plateau incliné (la carte en direct), le tableau de la vague au mur au-dessus,
        //                        le pupitre LANCER / SE TP (-41°), le pupitre VIDER (+37°), et le panier sur son tabouret (+54°)
        //   gauche (-82°)      : LA bibliothèque, un seul meuble courbe (7 types × 8 raretés)
        //   derrière (180°)    : la grande porte ouverte : le bananier dehors, l'étal des bananes juste devant
        //   arrière-gauche     : le comptoir RÉCOLTEUR (-136°) et la caisse accrochée au mur au-dessus
        //   arrière-droite     : le comptoir du bananier (137°) ; droite : le coffre (100°) et ses chances
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

        static Transform BuildHub()
        {
            var env = new GameObject("Hub").transform;
            BuildCabin(env);
            BuildSunDust(env);

            // Devant : le plateau ; à sa gauche un pupitre avec LANCER (la vague) et SE TP (aller sur la carte),
            // à sa droite le pupitre VIDER, puis le panier
            BuildBoard(env);
            var commands = BuildConsole(env, "Commandes", Around(-41f, Ring - 0.5f), 2);
            ConsoleButton(commands, -ConsoleStep / 2f, "LANCER", LaunchColor, ActionCube.Action.StartWave);
            ConsoleButton(commands, ConsoleStep / 2f, "SE TP", PlayColor, ActionCube.Action.Teleport, Level.Carte);   // va sur la carte
            var clear = BuildConsole(env, "Vider", Around(37f, Ring - 0.5f), 1);
            ConsoleButton(clear, 0f, $"VIDER  {ActionCube.ClearBoardPrice}", ClearColor, ActionCube.Action.ClearBoard);
            BuildWaveBoard(env, Around(0f, HubLayout.CabinRadius - 0.25f, 2.25f), Quaternion.identity);   // accroché au mur

            // À gauche : la bibliothèque (tous les types dans un seul meuble)
            BuildShelf(env, "Bibliotheque", -82f, 0, MonkeyData.TypeCount);

            // Derrière : le bananier dehors, derrière la grande porte ; l'étal des bananes dedans, juste devant la porte
            var bananier = BuildBananas(env, Around(180f, Ring + 1.3f), 180f, out var panier);
            BuildMoneyBoard(env, HubLayout.HarvesterPanelAngle);
            if (bananier) BuildUpgradePanel(env, bananier, 137f);
            if (bananier && panier) BuildHarvesters(env, bananier, panier);

            BuildDecor(env);
            UseWoodTexture(env);
            return env;
        }

        // La cabane est un vrai modèle 3D, fabriqué dans Blender par un script : Blender/cabane.py (à relancer après
        // un changement, voir son en-tête) -> Art/Cabane/Cabane.glb, plus les accessoires Tonneau, Caisse et Regime.
        // Le modèle apporte tout ce qui se voit : rondins, toit de chaume, porte et fenêtres, lustre, lanternes, tapis, prairie.
        // Ici, on le pose, on l'aligne, et on ajoute ce qu'un modèle n'a pas : les colliders (invisibles),
        // la zone de téléportation et les vraies lumières (aux repères « Lumiere_* » placés dans le modèle).
        const string CabinFolder = "Assets/_Project/Art/Cabane/";
        const int CabinSides = 12;
        const float CabinDoorHeight = 2.4f;
        const float BlenderGroundY = -0.35f;   // GROUND_Y dans cabane.py : la hauteur de la prairie du modèle
        const float TeleportRadius = Ring - 0.9f;   // 1,9 m : les meubles commencent un peu au-delà   // même valeur que DOOR_H dans cabane.py : la porte est derrière (180°)

        static void BuildCabin(Transform env)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(CabinFolder + "Cabane.glb");
            if (!asset) { Debug.LogWarning("Hub : Cabane.glb introuvable. Lancer Blender/cabane.py (voir l'en-tête du script)."); return; }
            var cabin = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            cabin.name = "Cabane";
            cabin.transform.SetParent(env, false);
            AlignCabin(cabin.transform);

            // Plancher : dedans et la terrasse derrière la porte (mêmes limites que la plateforme du modèle)
            var floor = new GameObject("Plancher (collider)");
            floor.transform.SetParent(env, false);
            var floorBox = floor.AddComponent<BoxCollider>();
            floorBox.center = new Vector3(0f, -0.05f, -0.8f);
            floorBox.size = new Vector3(8f, 0.1f, 9.6f);

            // La zone de téléportation : un disque au centre de la cabane, devant tous les meubles. Le reste du plancher
            // arrête le rayon de téléportation : on ne se pose plus dans un meuble, contre un mur ou dehors.
            var zone = new GameObject("Zone de téléportation");
            zone.transform.SetParent(env, false);
            zone.transform.localPosition = new Vector3(0f, 0.005f, 0f);
            zone.transform.localScale = new Vector3(TeleportRadius * 2f, 0.005f, TeleportRadius * 2f);   // 1 cm d'épaisseur
            var disc = zone.AddComponent<MeshCollider>();
            disc.sharedMesh = Cylinder;
            disc.convex = true;
            Teleportable(zone);

            // Sous le plateau : un bloc invisible du sol jusqu'à la planche. Le rayon de téléportation s'y arrête
            // (ce n'est pas une zone de téléportation) : on ne se pose plus sous le plateau, la tête dedans.
            var underBoard = new GameObject("Sous le plateau (bloque la téléportation)");
            underBoard.transform.SetParent(env, false);
            var block = underBoard.AddComponent<BoxCollider>();
            float boardSide = MapLayout.Size * BoardTile + 0.1f;
            block.center = Around(0f, Ring - 0.9f, 0.45f);
            block.size = new Vector3(boardSide, 0.9f, boardSide * Mathf.Cos(25f * Mathf.Deg2Rad));

            // Un collider par mur : on ne traverse pas, ni en marchant ni en se téléportant (sauf par la porte)
            float r = HubLayout.CabinRadius, h = HubLayout.CabinHeight;
            float sideLength = 2f * r * Mathf.Tan(Mathf.PI / CabinSides);
            for (int i = 0; i < CabinSides; i++)
            {
                float angle = i * 360f / CabinSides;
                float bottom = Mathf.Approximately(angle, 180f) ? CabinDoorHeight : 0f;
                var wall = new GameObject($"Mur {i} (collider)");
                wall.transform.SetParent(env, false);
                wall.transform.SetPositionAndRotation(Around(angle, r), Quaternion.Euler(0, angle, 0));
                var box = wall.AddComponent<BoxCollider>();
                box.center = new Vector3(0, (bottom + h) / 2f, 0);
                box.size = new Vector3(sideLength, h - bottom, 0.25f);
            }

            // Les lumières, là où le modèle a mis ses repères : un lustre au centre, deux lanternes aux murs
            foreach (var t in cabin.GetComponentsInChildren<Transform>())
            {
                if (t.name == "Lumiere_Lustre") AddLight(t, 8f, 3.2f);                       // plus forts (7 oct.) : une vraie lumière chaude de lampe
                else if (t.name.StartsWith("Lumiere_Lanterne")) AddLight(t, 4.5f, 2f);
            }

            MakeStatic(cabin);

            // Les flammes des bougies vacillent (FlameFlicker) : elles bougent, donc elles ne sont pas « static »
            foreach (var t in cabin.GetComponentsInChildren<Transform>())
                if (t.name.StartsWith("Flamme_"))
                {
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
                    t.gameObject.AddComponent<FlameFlicker>();
                }

            // La cible de fléchettes du modèle devient jouable (petit bonus caché)
            var dartboard = cabin.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Cible");
            if (dartboard) BuildDarts(env, dartboard.gameObject);
        }

        // La cible jouable : un collider plat devant la cible du modèle (DartBoard), une petite ardoise des points en dessous,
        // et un présentoir avec trois fléchettes à lancer (Dart), qui y reviennent toutes seules.
        const float DartboardFront = HubLayout.CabinRadius - 0.2f;   // le devant de la cible : rondins (12 cm) + 8 cm (cabane.py)

        static void BuildDarts(Transform env, GameObject model)
        {
            // Le devant de la cible, mesuré sur le mur qui la porte (et pas avec la boîte englobante du modèle :
            // la cible est de biais, sa boîte est plus profonde qu'elle, et les fléchettes se retrouvaient DANS
            // le collider du mur : le rayon touchait le mur, on ne pouvait pas les prendre (bug du 8 oct.)).
            var b = Bounds(model);
            float step = 360f / CabinSides;
            float wallAngle = Mathf.Round(Mathf.Atan2(b.center.x, b.center.z) * Mathf.Rad2Deg / step) * step;
            var outward = Quaternion.Euler(0f, wallAngle, 0f) * Vector3.forward;   // de la pièce vers le mur
            var face = b.center + outward * (DartboardFront - Vector3.Dot(b.center, outward));

            var board = new GameObject("Cible (jeu)").transform;
            board.SetParent(env, false);
            board.SetPositionAndRotation(face, Quaternion.LookRotation(outward));   // +Z vers le mur, comme les ardoises
            var col = board.gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(0.5f, 0.5f, 0.04f);
            col.center = new Vector3(0f, 0f, 0.02f);                              // sa face avant est sur celle de la cible
            var game = board.gameObject.AddComponent<DartBoard>();

            var slate = BuildChalkboard(board, "Ardoise des fléchettes", new Vector3(0f, -0.45f, 0.01f), Quaternion.identity, 0.5f, 0.22f);
            game.label = Visuals.Text(slate, "", new Vector3(0, 0, -0.05f), 0.04f, Chalk);

            Visuals.Solid("Présentoir à fléchettes", board, new Vector3(0f, -0.68f, -0.05f), new Vector3(0.45f, 0.03f, 0.12f), DarkWood);
            for (int i = 0; i < 3; i++)
                Dart.Create(env, board.TransformPoint(new Vector3((i - 1) * 0.12f, -0.65f, -0.05f)), Quaternion.LookRotation(board.right));
        }

        // Rien ici ne bouge : Unity regroupe les maillages (moins d'appels de dessin, plus de fps dans le casque)
        static void MakeStatic(GameObject model)
        {
            foreach (var t in model.GetComponentsInChildren<Transform>())
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
        }

        // Le paysage autour de la carte (Blender/cabane.py → Art/Cabane/Paysage.glb) : prairie, herbes hautes, fleurs,
        // buissons, palmiers et montagnes, comme autour de la cabane ; la zone de jeu est laissée libre.
        static void BuildScenery(Transform map, float groundY)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(CabinFolder + "Paysage.glb");
            if (!asset) { Debug.LogWarning("Carte : Paysage.glb introuvable. Lancer Blender/cabane.py (voir l'en-tête du script)."); return; }
            var scenery = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            scenery.name = "Paysage";
            scenery.transform.SetParent(map, false);
            AlignCabin(scenery.transform);                                       // mêmes repères que la cabane
            scenery.transform.localPosition = new Vector3(0f, groundY - BlenderGroundY, 0f);   // la prairie au niveau du sol de la carte
            MakeStatic(scenery);
        }

        // Le modèle passe de Blender (Z en haut) à Unity (Y en haut) : selon l'importeur, il peut arriver tourné ou en miroir.
        // On le remet d'aplomb avec ses deux repères : « Repere_Porte » doit être derrière (180°), « Repere_Droite » à droite (+90°).
        static void AlignCabin(Transform cabin)
        {
            Transform door = null, right = null;
            foreach (var t in cabin.GetComponentsInChildren<Transform>())
            {
                if (t.name == "Repere_Porte") door = t;
                if (t.name == "Repere_Droite") right = t;
            }
            if (!door || !right) { Debug.LogWarning("Hub : repères de la cabane introuvables, cabane non alignée."); return; }

            cabin.Rotate(0f, 180f - AngleOf(door.position), 0f, Space.World);
            if (AngleOf(right.position) < 0f)
            {
                var s = cabin.localScale;
                cabin.localScale = new Vector3(-s.x, s.y, s.z);   // miroir gauche-droite : la porte (sur l'axe) ne bouge pas
            }
        }

        static float AngleOf(Vector3 p) => Mathf.Atan2(p.x, p.z) * Mathf.Rad2Deg;   // 0 = devant, positif = à droite

        static void AddLight(Transform at, float range, float intensity)
        {
            var light = new GameObject("Lumière").AddComponent<Light>();
            light.transform.SetParent(at, false);
            light.type = LightType.Point;
            light.range = range;
            light.intensity = intensity;
            light.color = new Color(1f, 0.8f, 0.55f);   // chaude, comme une flamme
            light.shadows = LightShadows.None;           // les ombres des lampes coûtent trop cher sur le casque
            light.gameObject.AddComponent<FlameFlicker>();   // la lumière vacille avec les flammes
        }

        // Les accessoires du modèle Blender (tonneaux, caisses, régimes de bananes), seulement dehors, sur la terrasse :
        // dans la cabane, ils gênaient (les caisses rentraient dans la bibliothèque, le tonneau collait au panier).
        static void BuildDecor(Transform env)
        {
            var decor = new GameObject("Décor").transform;
            decor.SetParent(env, false);

            Prop(decor, "Tonneau", Around(205f, Ring + 1.6f), 40f, 1f);
            Prop(decor, "Regime", Around(205f, Ring + 1.6f, 0.9f), 0f, 1.6f);
            Prop(decor, "Tonneau", Around(155f, Ring + 1.5f), 0f, 1f);
            Prop(decor, "Caisse", Around(145f, Ring + 1.9f), 30f, 1f);
            Prop(decor, "Caisse", Around(215f, Ring + 2.1f), -20f, 1f);
            Prop(decor, "Caisse", Around(215f, Ring + 2.1f, 0.5f), 10f, 0.7f);
        }

        static void Prop(Transform parent, string model, Vector3 pos, float yaw, float scale)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(CabinFolder + model + ".glb");
            if (!asset) { Debug.LogWarning($"Hub : {model}.glb introuvable (Blender/cabane.py)."); return; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            go.transform.localScale = Vector3.one * scale;
            if (model == "Regime") return;   // les bananes de déco ne bloquent rien (les vraies tombent du bananier)
            // Un collider qui l'entoure : les bananes lâchées s'y posent, on ne marche pas au travers
            var b = Bounds(go);
            var box = go.AddComponent<BoxCollider>();
            box.center = go.transform.InverseTransformPoint(b.center);
            box.size = b.size / scale;
        }

        // Les meubles en bois uni (pupitres, bibliothèque, socles, cadres) prennent la texture du bois, gardant leur teinte
        static void UseWoodTexture(Transform env)
        {
            foreach (var tint in env.GetComponentsInChildren<ColorTint>(true))
                if (tint.color == Wood || tint.color == DarkWood) tint.GetComponent<Renderer>().sharedMaterial = CabinArt.Furniture;
        }


        // Plateau incliné de 25° vers le joueur, posé sur une planche qui suit l'inclinaison + un pied.
        static void BuildBoard(Transform env)
        {
            float scale = BoardTile / MapLayout.Tile;
            float side = MapLayout.Size * BoardTile;
            var center = Around(0f, Ring - 0.9f, 0.95f);   // reculé jusqu'au cercle, devant le joueur

            var boardGo = new GameObject("Plateau");
            boardGo.tag = Tags.Plateau;
            boardGo.transform.SetParent(env, false);
            boardGo.transform.SetPositionAndRotation(center, Quaternion.Euler(-25f, 0, 0));
            var board = boardGo.AddComponent<Board>();
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
            // Le nom du meuble sur une enseigne posée dessus, et celui de chaque type sur une plaque claire accrochée
            // au bord de son étagère, dans la première colonne : rien ne flotte, rien ne se tourne vers le joueur,
            // et la plaque n'est plus cachée au fond du casier.
            Visuals.Box("Enseigne", shelf, Around(centerAngle, Ring + 0.2f, height + 0.12f), new Vector3(1.4f, 0.26f, 0.05f), DarkWood)
                .transform.rotation = Facing(centerAngle);
            Visuals.Text(shelf, "BIBLIOTHÈQUE", Around(centerAngle, Ring + 0.17f, height + 0.12f), 0.16f, TitleGold, title: true)
                .transform.rotation = Facing(centerAngle);

            for (int i = 0; i < count; i++)
            {
                var type = (MonkeyType)(firstType + i);
                float y = FirstShelfY + i * SlotStepY;
                // La plaque se lit et s'enfonce : tant que le type n'est pas débloqué, elle affiche son prix en bananes
                var plaque = Visuals.Box($"Plaque {type}", shelf, Around(Angle(-1), Ring - 0.19f, y - 0.03f), new Vector3(SlotStepX, 0.14f, 0.015f), Parchment);
                plaque.transform.rotation = Facing(Angle(-1));
                var plaqueText = Visuals.Text(shelf, type.ToString(), Around(Angle(-1), Ring - 0.2f, y - 0.03f), 0.045f, Engraved);
                plaqueText.transform.rotation = Facing(Angle(-1));
                plaque.tag = Tags.Bouton;
                plaque.AddComponent<BoxCollider>().size = new Vector3(1f, 1f, 4f);   // un peu épais : facile à toucher du bout de la manette
                var unlock = plaque.AddComponent<TypeUnlockPlaque>();
                unlock.type = type;
                unlock.label = plaqueText;
                unlock.plate = plaque.GetComponent<ColorTint>();
                plaque.AddComponent<RayPress>();   // ou de loin, en la visant

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
        static Bananier BuildBananas(Transform env, Vector3 pos, float yaw, out Panier panier)
        {
            panier = null;
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

            // L'étal des bananes : devant le bananier, à l'intérieur du cercle. Les bananes tombent DESSUS,
            // à hauteur de main : on ne se baisse pas pour les ramasser (règle de confort VR).
            // Une vraie table en bois (4 pieds, rebords, étagère basse avec une caisse), et une grande feuille de bananier
            // posée dessus : les bananes jaunes ressortent bien sur le vert (avant, jaune sur jaune).
            var tablePos = Around(180f, Ring - 0.1f);   // juste devant la porte, le bananier dehors à 1,4 m
            var table = new GameObject("Etal des bananes").transform;
            table.SetParent(env, false);
            table.SetPositionAndRotation(tablePos, Quaternion.LookRotation(new Vector3(tablePos.x, 0, tablePos.z) - new Vector3(pos.x, 0, pos.z)));
            const float StallW = 1.4f, StallD = 1.0f;   // un peu moins large qu'avant : de la place pour le comptoir du récolteur
            Visuals.Solid("Plateau", table, new Vector3(0, HandHeight - 0.025f, 0), new Vector3(StallW, 0.05f, StallD), Wood);
            Visuals.Box("Feuille", table, new Vector3(0, HandHeight + 0.003f, 0), new Vector3(StallW - 0.2f, 0.006f, StallD - 0.2f), Leaf);
            Visuals.Solid("Rebord avant", table, new Vector3(0, HandHeight + 0.03f, -StallD / 2f + 0.015f), new Vector3(StallW, 0.06f, 0.03f), DarkWood);
            Visuals.Solid("Rebord arriere", table, new Vector3(0, HandHeight + 0.03f, StallD / 2f - 0.015f), new Vector3(StallW, 0.06f, 0.03f), DarkWood);
            Visuals.Solid("Rebord gauche", table, new Vector3(-StallW / 2f + 0.015f, HandHeight + 0.03f, 0), new Vector3(0.03f, 0.06f, StallD), DarkWood);
            Visuals.Solid("Rebord droit", table, new Vector3(StallW / 2f - 0.015f, HandHeight + 0.03f, 0), new Vector3(0.03f, 0.06f, StallD), DarkWood);
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    Visuals.Box("Pied", table, new Vector3(sx * (StallW / 2f - 0.08f), (HandHeight - 0.05f) / 2f, sz * (StallD / 2f - 0.08f)), new Vector3(0.07f, HandHeight - 0.05f, 0.07f), Wood);
            Visuals.Box("Etagere basse", table, new Vector3(0, 0.22f, 0), new Vector3(StallW - 0.16f, 0.03f, StallD - 0.16f), DarkWood);
            Prop(table, "Caisse", table.TransformPoint(new Vector3(-0.35f, 0.235f, 0f)), table.eulerAngles.y + 10f, 0.6f);
            bananier.versCible = table;
            bananier.hauteurAuSol = HandHeight + 0.1f;   // la banane se pose sur la table, pas par terre
            bananier.margePanier = 0f;
            bananier.largeurZone = 0.8f;
            bananier.angleDispersion = 18f;

            // Le panier : à côté du plateau, sur un tabouret rond à hauteur de main (3 pieds, une étagère basse)
            var basket = root.transform.Find("Panier");
            if (basket)
            {
                var basketPos = Around(BasketAngle, BasketRadius);
                var stool = new GameObject("Tabouret du panier").transform;
                stool.SetParent(env, false);
                stool.localPosition = basketPos;
                Visuals.Solid("Assise", stool, new Vector3(0, HandHeight - 0.03f, 0), new Vector3(0.48f, 0.03f, 0.48f), DarkWood)
                    .GetComponent<MeshFilter>().sharedMesh = Cylinder;
                Visuals.Box("Etagere basse", stool, new Vector3(0, 0.3f, 0), new Vector3(0.4f, 0.015f, 0.4f), Wood)
                    .GetComponent<MeshFilter>().sharedMesh = Cylinder;
                for (int i = 0; i < 3; i++)
                    Visuals.Box("Pied", stool, Around(i * 120f, 0.17f, (HandHeight - 0.06f) / 2f), new Vector3(0.05f, HandHeight - 0.06f, 0.05f), Wood);
                basket.position = basketPos + Vector3.up * HandHeight;
                panier = basket.GetComponentInChildren<Panier>();
                // On LANCE les bananes dans le panier : plus de parois ni de poignée qui les renvoient (MeshCollider retiré),
                // et une zone de dépôt plus haute que le bord, pour qu'un lancer un peu court compte quand même.
                foreach (var wall in basket.GetComponentsInChildren<MeshCollider>()) Object.DestroyImmediate(wall);
                if (panier)
                {
                    var zone = panier.GetComponent<BoxCollider>();
                    zone.size = new Vector3(2.4f, 4f, 2.4f);   // dans le repère de Zone_Depot (déjà à la taille de l'intérieur)
                    zone.center = new Vector3(0f, 1f, 0f);
                }
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

            // Une ardoise fixe (pas de billboard) : c'est le panneau entier qui fait face au joueur, au-dessus du comptoir
            var panel = BuildChalkboard(root, "Panneau", new Vector3(0, 2.4f, 0), Quaternion.identity, 0.8f, 0.4f);
            FillMoneyBoard(panel, root.gameObject);
        }

        // Le texte d'une caisse (hub ou carte) : « MES BANANES », puis le nombre en gros chiffres dorés.
        // host : l'objet qui porte le script (au hub, le pied du mur : les « +50 » de fin de vague apparaissent à hauteur d'yeux).
        static MoneyBoard FillMoneyBoard(Transform panel, GameObject host)
        {
            Visuals.Text(panel, "MES BANANES", new Vector3(0, 0.12f, -0.05f), 0.08f, Chalk, title: true);
            var amount = Visuals.Text(panel, "0", new Vector3(0, -0.05f, -0.05f), 0.22f, TitleGold, title: true);
            var board = host.AddComponent<MoneyBoard>();
            board.amount = amount;
            board.panel = panel;
            board.frame = panel.Find("Cadre").GetComponent<ColorTint>();
            return board;
        }

        // Comptoir d'amélioration (bananier, récolteur) : un meuble bas en bois avec un bouton rond par amélioration
        // et, gravé sur une plaque devant chaque bouton, ce qu'il fait. Derrière, une ardoise encadrée (une colonne par
        // bouton : titre, niveau, effet, prix) tenue par deux montants qui portent aussi l'enseigne : rien ne flotte.
        // +Z local = vers le mur. Renvoie les boutons (leur dessus qui s'enfonce s'appelle « Bouton ») et le texte de chaque colonne.
        const float CounterStep = 0.36f;

        static Transform BuildCounter(Transform env, string name, string title, float angle, string[] names,
            out Transform[] buttons, out TextMesh[] labels)
        {
            var root = new GameObject(name).transform;
            root.SetParent(env, false);
            root.SetPositionAndRotation(Around(angle, Ring - 0.45f), Quaternion.Euler(0, angle, 0));
            int columns = names.Length;
            float width = columns * CounterStep + 0.16f;

            Visuals.Box("Plinthe", root, new Vector3(0, 0.035f, 0.06f), new Vector3(width + 0.04f, 0.07f, 0.4f), DarkWood);
            Visuals.Solid("Meuble", root, new Vector3(0, 0.45f, 0.06f), new Vector3(width, 0.8f, 0.36f), Wood);
            Visuals.Box("Dessus", root, new Vector3(0, 0.875f, 0.04f), new Vector3(width + 0.08f, 0.05f, 0.44f), DarkWood);

            // Deux montants posés sur le comptoir, de chaque côté de l'ardoise, jusqu'en haut de l'enseigne
            const float SignY = 1.85f, SignH = 0.24f;
            float postTop = SignY + SignH / 2f;
            for (int side = -1; side <= 1; side += 2)
                Visuals.Box("Montant", root, new Vector3(side * (width / 2f + 0.025f), (0.9f + postTop) / 2f, 0.2f), new Vector3(0.05f, postTop - 0.9f, 0.06f), Wood);
            var board = BuildChalkboard(root, "Ardoise", new Vector3(0, 1.36f, 0.2f), Quaternion.identity, width - 0.12f, 0.62f);
            Visuals.Box("Enseigne", root, new Vector3(0, SignY, 0.2f), new Vector3(width + 0.1f, SignH, 0.05f), DarkWood);   // posée sur le cadre de l'ardoise
            Visuals.Text(root, title, new Vector3(0, SignY, 0.17f), 0.15f, TitleGold, title: true);

            buttons = new Transform[columns];
            labels = new TextMesh[columns];
            for (int i = 0; i < columns; i++)
            {
                float x = (i - (columns - 1) / 2f) * CounterStep;
                labels[i] = Visuals.Text(board, "", new Vector3(x, 0.02f, -0.05f), 0.056f, Chalk);
                if (i > 0)   // un trait de craie entre deux colonnes
                    Visuals.Box("Trait", board, new Vector3(x - CounterStep / 2f, 0, -0.047f), new Vector3(0.008f, 0.5f, 0.004f), new Color(0.6f, 0.62f, 0.58f));
                Visuals.Box("Porte", root, new Vector3(x, 0.42f, -0.125f), new Vector3(CounterStep - 0.08f, 0.5f, 0.02f), DarkWood);
                buttons[i] = RoundButton(root, new Vector3(x, 0.9f, 0.04f));

                // Ce que fait le bouton, gravé sur une plaque en laiton couchée devant lui
                Visuals.Box("Plaque", root, new Vector3(x, 0.903f, -0.11f), new Vector3(CounterStep - 0.04f, 0.006f, 0.09f), DarkWood);   // plaque foncée, texte doré : bien lisible
                Visuals.Text(root, names[i], new Vector3(x, 0.91f, -0.11f), 0.06f, TitleGold, title: true)
                    .transform.localRotation = Quaternion.Euler(90f, 0, 0);
            }
            return root;
        }

        // Gros bouton rond de comptoir : la racine porte le collider (pour la main et le rayon),
        // « Bouton » est le dessus coloré qui s'enfonce, posé dans une bague en laiton.
        static Transform RoundButton(Transform parent, Vector3 localPos)
        {
            var button = new GameObject("Bouton").transform;
            button.SetParent(parent, false);
            button.localPosition = localPos;
            button.gameObject.tag = Tags.Bouton;
            var col = button.gameObject.AddComponent<BoxCollider>();   // avant RayPress : l'interactable récupère le collider
            col.size = new Vector3(0.2f, 0.12f, 0.2f);
            col.center = new Vector3(0, 0.04f, 0);

            Visuals.Box("Bague", button, new Vector3(0, 0.005f, 0), new Vector3(0.17f, 0.01f, 0.17f), Brass)
                .GetComponent<MeshFilter>().sharedMesh = Cylinder;
            Visuals.Box("Bouton", button, new Vector3(0, 0.02f, 0), new Vector3(0.12f, 0.02f, 0.12f), UpgradeButton.TooExpensive)
                .GetComponent<MeshFilter>().sharedMesh = Cylinder;
            return button;
        }

        // Comptoir d'amélioration du bananier : 3 gros boutons ronds à enfoncer (production, fraîcheur, valeur).
        static void BuildUpgradePanel(Transform env, Bananier bananier, float angle)
        {
            var stats = new[] { BananaStat.Frequence, BananaStat.Pourriture, BananaStat.Valeur };
            BuildCounter(env, "Comptoir bananier", "BANANIER", angle, new[] { "PRODUCTION", "FRAÎCHEUR", "VALEUR" }, out var buttons, out var labels);
            for (int i = 0; i < stats.Length; i++)
            {
                buttons[i].name = $"Bouton {stats[i]}";
                var up = buttons[i].gameObject.AddComponent<UpgradeButton>();
                buttons[i].gameObject.AddComponent<RayPress>();
                up.bananier = bananier;
                up.stat = stats[i];
                up.cap = buttons[i].Find("Bouton");
                up.label = labels[i];
            }
        }

        // Les singes récolteurs : le comptoir « RÉCOLTEUR » (acheter un singe de plus, puis vitesse, cadence et rendement
        // pour toute l'équipe, voir HarvesterCrew) et les singes eux-mêmes, cachés jusqu'à leur achat.
        // Chacun attend à sa place, en arc de cercle devant l'étal des bananes.
        const int HarvesterCount = 4;          // le maximum de singes qu'on peut acheter
        const float HarvesterSize = 0.55f;     // taille d'un singe, en mètres
        const float HarvesterSpacing = 12f;    // en degrés, entre les places de deux singes

        static void BuildHarvesters(Transform env, Bananier bananier, Panier panier)
        {
            var counter = BuildCounter(env, "Comptoir récolteur", "RÉCOLTEUR", HubLayout.HarvesterPanelAngle,
                new[] { "+1 SINGE", "VITESSE", "CADENCE", "RENDEMENT" }, out var buttons, out var labels);
            var crew = counter.gameObject.AddComponent<HarvesterCrew>();
            crew.monkeys = new HarvesterMonkey[HarvesterCount];
            for (int i = 0; i < HarvesterCount; i++)
            {
                float offset = (i % 2 == 0 ? 1 : -1) * ((i + 1) / 2) * HarvesterSpacing;   // 0, -12, +12, -24 : de part et d'autre
                crew.monkeys[i] = BuildHarvester(env, crew, bananier, panier, HubLayout.HarvesterHomeAngle + offset);
            }

            var stats = new[] { HarvesterStat.Vitesse, HarvesterStat.Vitesse, HarvesterStat.Cadence, HarvesterStat.Rendement };
            for (int i = 0; i < buttons.Length; i++)
            {
                bool buy = i == 0;   // le premier bouton achète un singe, les autres améliorent l'équipe
                buttons[i].name = buy ? "Bouton acheter" : $"Bouton {stats[i]}";
                var hb = buttons[i].gameObject.AddComponent<HarvesterButton>();
                buttons[i].gameObject.AddComponent<RayPress>();
                hb.crew = crew;
                hb.isBuyButton = buy;
                hb.stat = stats[i];
                hb.cap = buttons[i].Find("Bouton");
                hb.label = labels[i];
            }
        }

        static HarvesterMonkey BuildHarvester(Transform env, HarvesterCrew crew, Bananier bananier, Panier panier, float homeAngle)
        {
            var root = new GameObject("Singe récolteur");
            root.transform.SetParent(env, false);
            var home = Around(homeAngle, HubLayout.HarvesterHomeRadius);
            root.transform.SetPositionAndRotation(home, Quaternion.LookRotation(-home));   // tourné vers le centre

            // Le singe classique, posé au sol. Le modèle regarde vers -Z : on le retourne pour qu'il marche vers l'avant (+Z).
            var piece = Visuals.MonkeyPiece(new Monkey(MonkeyType.Classique, Rarity.Gris), root.transform,
                                            new Vector3(0f, HarvesterSize / 2f, 0f), HarvesterSize, withLabel: false);
            piece.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var monkey = root.AddComponent<HarvesterMonkey>();
            monkey.crew = crew;
            monkey.bananier = bananier;
            monkey.panier = panier;
            monkey.table = bananier.versCible;
            monkey.home = home;
            monkey.height = HarvesterSize;
            var view = piece.GetComponent<MonkeyView>();
            if (view.model) root.AddComponent<HarvesterAnimator>().model = view.model.transform;
            root.AddComponent<HarvesterGrab>();   // on peut le prendre et le lancer, pour rire
            root.SetActive(false);   // il apparaît quand on l'achète
            return monkey;
        }

        // Le coffre : le modèle de la cabane (Art/Coffre/Coffre.glb, fait par Blender/coffre.py), avec la roulette
        // et le texte de Nicolas, branché sur notre joueur (ChestClickable) et sur l'argent commun.
        // Il reste fixe, tourné vers le centre ; à l'ouverture : boing et couvercle (ChestLid).
        static void BuildChest(Transform env, Vector3 pos)
        {
            const string ChestModelPath = "Assets/_Project/Art/Coffre/Coffre.glb";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ChestModelPath);
            if (!model) { Debug.LogWarning("Hub : modèle du coffre introuvable (glTFast installé ?), coffre non placé."); return; }

            var chest = (GameObject)PrefabUtility.InstantiatePrefab(model);
            chest.name = "Coffre";
            chest.transform.SetParent(env, false);
            var b = Bounds(chest);
            // taille réelle du modèle (1,1 m de large) : bien visible dans la cabane
            // tourné vers le joueur (au centre), posé au sol
            chest.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(new Vector3(-pos.x, 0, -pos.z)));
            b = Bounds(chest);
            // posé sur une petite estrade en bois (comme un trésor qu'on expose)
            const float Dais = 0.1f;
            var dais = Visuals.Solid("Estrade du coffre", env, new Vector3(pos.x, Dais / 2f, pos.z), new Vector3(1.4f, Dais, 1.0f), Wood);
            dais.transform.rotation = Quaternion.Euler(0, AngleOf(pos), 0);
            chest.transform.position += new Vector3(pos.x - b.center.x, Dais - b.min.y, pos.z - b.center.z);
            float top = Bounds(chest).max.y;


            var rouletteGo = new GameObject("Roulette");
            rouletteGo.transform.SetParent(env, false);
            rouletteGo.transform.position = new Vector3(pos.x, top + 1.0f, pos.z);
            rouletteGo.AddComponent<Sae501.Coffres.Billboard>();
            var roulette = rouletteGo.AddComponent<Sae501.Coffres.RouletteView>();

            // Plus de texte au-dessus du coffre (ChestPrompt) : le prix est sur la pancarte devant, qui dit aussi ce qui manque
            var controller = chest.AddComponent<Sae501.Coffres.ChestController>();
            controller.roulette = roulette;
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

            // L'ouverture : boing, puis le couvercle (son origine est sur la charnière) ; le singe sort du centre du coffre
            var glowAnchor = new GameObject("Centre").transform;
            glowAnchor.SetParent(chest.transform, false);
            glowAnchor.position = chestBounds.center;
            // Un trésor dedans : les bananes du bananier, posées sur le lit de feuilles du double fond (Blender/coffre.py).
            // Deux rangées de quatre, un peu en vrac : le coffre déborde de bananes (critique de Maxens : « plus de bananes »).
            var bananaModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Bananier/FBX/Bananes_Collectible.fbx");
            if (bananaModel)
            {
                var spots = new[]
                {
                    new Vector3(-0.34f, 0f, 0.12f), new Vector3(-0.12f, 0f, 0.02f), new Vector3(0.1f, 0f, 0.14f), new Vector3(0.33f, 0f, 0.04f),
                    new Vector3(-0.3f, 0f, -0.12f), new Vector3(-0.08f, 0f, -0.16f), new Vector3(0.14f, 0f, -0.08f), new Vector3(0.36f, 0f, -0.15f),
                };
                for (int i = 0; i < spots.Length; i++)
                {
                    var banana = (GameObject)PrefabUtility.InstantiatePrefab(bananaModel);
                    banana.name = "Bananes du coffre";
                    banana.transform.SetParent(chest.transform, false);
                    banana.transform.localScale = Vector3.one * ChestBananaScale;
                    var spot = chestBounds.center + chest.transform.right * spots[i].x + chest.transform.forward * spots[i].z;
                    spot.y = chestBounds.min.y + ChestFloor + ChestBananaScale * 0.12f;
                    banana.transform.SetPositionAndRotation(spot, Quaternion.Euler(80f, chest.transform.eulerAngles.y + 50f * i + 20f, 0f));   // couchées sur les feuilles
                }
            }
            var lid = chest.AddComponent<ChestLid>();
            lid.lid = chest.GetComponentsInChildren<Transform>(true).First(t => t.name == "Coffre_Couvercle");
            lid.glowAnchor = glowAnchor;
            lid.chest = controller;

            // Panneau des chances : un tableau encadré de bois, accroché au mur derrière le coffre (comme la caisse)
            float chestAngle = AngleOf(pos);
            var oddsRoot = BuildChalkboard(env, "Chances du coffre", Around(chestAngle, HubLayout.CabinRadius - 0.3f, 2.18f),
                Quaternion.Euler(0, chestAngle, 0), 1.15f, 1.45f);   // +Z local = vers le mur
            var oddsText = Visuals.Text(oddsRoot, "", new Vector3(0, 0, -0.05f), 0.058f, Chalk);
            var oddsPanel = oddsRoot.gameObject.AddComponent<ChestOddsPanel>();
            oddsPanel.chest = controller;
            oddsPanel.text = oddsText;

            // Le prix, sur une petite pancarte plantée devant l'estrade (doré si on peut payer) : il ne flotte plus en l'air.
            // Devant l'estrade et pas dessus : le couvercle s'ouvre et le singe sort par là.
            var sign = new GameObject("Pancarte du prix").transform;
            sign.SetParent(env, false);
            sign.SetPositionAndRotation(Around(chestAngle, pos.magnitude - 0.55f), Quaternion.Euler(0, chestAngle, 0));
            for (int side = -1; side <= 1; side += 2)
                Visuals.Box("Piquet", sign, new Vector3(side * 0.2f, 0.15f, 0.01f), new Vector3(0.03f, 0.3f, 0.03f), Wood);
            var plank = new GameObject("Planche").transform;
            plank.SetParent(sign, false);
            plank.localPosition = new Vector3(0, 0.3f, 0);
            plank.localRotation = Quaternion.Euler(20f, 0, 0);   // penchée vers l'arrière : elle se lit en baissant les yeux
            Visuals.Box("Planche", plank, Vector3.zero, new Vector3(0.56f, 0.16f, 0.03f), DarkWood);
            var priceTag = sign.gameObject.AddComponent<ChestPriceTag>();
            priceTag.chest = controller;
            priceTag.label = Visuals.Text(plank, "", new Vector3(0, 0, -0.02f), 0.09f, TitleGold, title: true);
        }

        static Bounds Bounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        // Le terrain de jeu : une pelouse carrée, puis le chemin de dalles et le décor (MapDecor), façon Bloons TD.
        // Sur la carte, la pelouse est invisible : on voit l'herbe de la prairie tout autour (Paysage.glb), posée au même niveau ;
        // elle garde son collider pour s'y téléporter. Sur le plateau du hub, elle est verte (pas de prairie dessous).
        static void BuildGrid(Transform parent, float tile, float thickness, bool walkable)
        {
            float k = tile / MapLayout.Tile;
            float side = MapLayout.Size * tile;
            var size = new Vector3(side, thickness, side);
            var pos = new Vector3(0, -thickness / 2f, 0);
            var lawn = walkable ? Visuals.Solid("Pelouse", parent, pos, size, Grass1) : Visuals.Box("Pelouse", parent, pos, size, Grass1);
            lawn.tag = Tags.Terrain;
            if (walkable)
            {
                lawn.GetComponent<Renderer>().enabled = false;
                Teleportable(lawn);
            }
            MapDecor.Build(parent, k, withBushes: walkable);   // les buissons dépasseraient du plateau du hub
        }

        // ---------------- CARTE ----------------
        static Transform BuildMap()
        {
            var map = new GameObject("Carte");
            map.transform.position = MapCenter;
            BuildGrid(map.transform, MapLayout.Tile, 0.5f, true);
            map.AddComponent<TowerManager>();
            map.AddComponent<PlacementSurface>().scale = 1f; // prendre / poser / fusionner directement sur la carte
            map.AddComponent<WaveSpawner>();

            float edge = MapLayout.HalfExtent;
            const float GroundY = -0.02f;   // le dessus de la prairie : juste sous le terrain de jeu, c'est son herbe qu'on voit (8 oct.)
            Teleportable(Visuals.Solid("Estrade", map.transform, new Vector3(0, -0.25f, -edge - 2.5f), new Vector3(8, 0.5f, 5), Wood));
            // Le sol de toute la prairie (invisible : on voit l'herbe du paysage) : on peut s'y téléporter partout,
            // autour du labyrinthe comme plus loin, et sur la pelouse du terrain de jeu elle-même (voir BuildGrid)
            // On ne peut s'y téléporter que PRÈS du labyrinthe (TeleportMargin autour) : plus loin, le rayon devient rouge,
            // on ne part plus se perdre dans les montagnes. La prairie reste solide partout (on ne tombe pas).
            var meadow = Visuals.Solid("Prairie (collider)", map.transform, new Vector3(0, GroundY - 0.05f, 0), new Vector3(180f, 0.1f, 180f), Floor);
            meadow.GetComponent<Renderer>().enabled = false;
            const float TeleportMargin = 4f;   // 4 m (6 avant le 8 oct. : on se posait dans les arbres) ; arbres et buissons commencent 4 m plus loin (cabane.py, map_clear)
            float zoneSide = 2f * (edge + TeleportMargin);
            var zone = Visuals.Solid("Zone de téléportation", map.transform, new Vector3(0, GroundY - 0.04f, 0), new Vector3(zoneSide, 0.1f, zoneSide), Floor);
            zone.GetComponent<Renderer>().enabled = false;   // 1 cm au-dessus de la prairie : c'est elle que le rayon touche
            Teleportable(zone);
            BuildScenery(map.transform, GroundY);

            // Le même pupitre qu'au hub, un peu à droite du point d'arrivée : le passage vers la carte reste libre
            var commands = BuildConsole(map.transform, "Carte", new Vector3(1.5f, 0f, -edge - 2f), 2, 0f);
            ConsoleButton(commands, -ConsoleStep / 2f, "LANCER", LaunchColor, ActionCube.Action.StartWave);
            ConsoleButton(commands, ConsoleStep / 2f, "HUB", HubColor, ActionCube.Action.Teleport, Level.Hub);
            BuildWaveBoard(map.transform, new Vector3(0, 2.4f, -edge - 0.3f), Quaternion.identity);
            for (int side = -1; side <= 1; side += 2)   // le tableau tient sur deux poteaux plantés dans le sol (il ne flotte pas)
                Visuals.Solid("Poteau du tableau", map.transform, new Vector3(side * 0.92f, (2.4f + GroundY) / 2f, -edge - 0.25f), new Vector3(0.1f, 2.4f - GroundY, 0.1f), Wood);
            BuildBowUpgrades(map.transform, new Vector3(-2f, 0f, -edge - 0.6f), -30f);   // le pupitre ARC de Nicolas

            // La caisse de la carte : on voit ses bananes sans retourner au hub. À droite du pupitre, sur deux poteaux.
            var cashPos = new Vector3(3.3f, 1.5f, -edge - 1.6f);
            var cash = BuildChalkboard(map.transform, "Caisse de la carte", cashPos, Quaternion.Euler(0, 20f, 0), 0.8f, 0.4f);
            FillMoneyBoard(cash, cash.gameObject).spawnPopups = false;
            for (int side = -1; side <= 1; side += 2)   // plantés dans l'estrade (son dessus est à 0)
                Visuals.Solid("Poteau de la caisse", cash, new Vector3(side * 0.47f, -cashPos.y / 2f, 0.05f), new Vector3(0.06f, cashPos.y, 0.06f), Wood);
            BuildDeckRailing(map.transform, edge);
            UseWoodTexture(map.transform);   // l'estrade, les pupitres et les poteaux, en bois comme au hub
            return map.transform;
        }

        // L'habillage de l'estrade (8 x 5 m, son dessus à 0) : un liseré de bois foncé tout autour, une rambarde basse
        // sur les côtés et à l'arrière (l'avant reste ouvert : on voit la carte et on s'y téléporte), et une lanterne
        // sur chaque poteau d'angle avant. Pas de collider : la rambarde ne bloque ni la vue ni le rayon de téléportation.
        static void BuildDeckRailing(Transform map, float edge)
        {
            const float HalfWidth = 4f, Depth = 5f, RailHeight = 0.9f;
            float front = -edge, back = -edge - Depth;
            var deck = new GameObject("Rambarde de l'estrade").transform;
            deck.SetParent(map, false);

            // Le liseré, juste au-dessus du plancher : on voit le bord de l'estrade sur l'herbe
            Visuals.Box("Liseré avant", deck, new Vector3(0, 0.03f, front - 0.06f), new Vector3(HalfWidth * 2f, 0.06f, 0.12f), DarkWood);
            Visuals.Box("Liseré arrière", deck, new Vector3(0, 0.03f, back + 0.06f), new Vector3(HalfWidth * 2f, 0.06f, 0.12f), DarkWood);
            for (int side = -1; side <= 1; side += 2)
                Visuals.Box("Liseré du côté", deck, new Vector3(side * (HalfWidth - 0.06f), 0.03f, (front + back) / 2f), new Vector3(0.12f, 0.06f, Depth), DarkWood);

            // Les rambardes : poteaux tous les mètres environ, deux lisses
            void Rail(Vector3 from, Vector3 to)
            {
                float length = Vector3.Distance(from, to);
                int posts = Mathf.CeilToInt(length / 1.1f);
                for (int i = 0; i <= posts; i++)
                    Visuals.Box("Poteau", deck, Vector3.Lerp(from, to, i / (float)posts) + Vector3.up * RailHeight / 2f, new Vector3(0.09f, RailHeight, 0.09f), Wood);
                var rot = Quaternion.LookRotation(to - from);
                foreach (float y in new[] { RailHeight * 0.5f, RailHeight - 0.03f })
                {
                    var rail = Visuals.Box("Lisse", deck, (from + to) / 2f + Vector3.up * y, new Vector3(0.06f, 0.07f, length), DarkWood);
                    rail.transform.localRotation = rot;
                }
            }
            Rail(new Vector3(-HalfWidth + 0.1f, 0, back + 0.1f), new Vector3(HalfWidth - 0.1f, 0, back + 0.1f));
            for (int side = -1; side <= 1; side += 2)
                Rail(new Vector3(side * (HalfWidth - 0.1f), 0, back + 0.1f), new Vector3(side * (HalfWidth - 0.1f), 0, front - 0.6f));

            // Une lanterne au bout de chaque rambarde de côté (vers la carte) : un grand poteau, une cage de fer, une flamme
            for (int side = -1; side <= 1; side += 2)
            {
                var foot = new Vector3(side * (HalfWidth - 0.1f), 0, front - 0.6f);
                Visuals.Box("Poteau de la lanterne", deck, foot + Vector3.up * 0.8f, new Vector3(0.12f, 1.6f, 0.12f), Wood);
                var lantern = foot + Vector3.up * 1.75f;
                Visuals.Box("Toit de la lanterne", deck, lantern + Vector3.up * 0.15f, new Vector3(0.26f, 0.04f, 0.26f), Iron);
                Visuals.Box("Pied de la lanterne", deck, lantern - Vector3.up * 0.15f, new Vector3(0.22f, 0.03f, 0.22f), Iron);
                for (int cx = -1; cx <= 1; cx += 2)
                    for (int cz = -1; cz <= 1; cz += 2)
                        Visuals.Box("Montant", deck, lantern + new Vector3(cx * 0.1f, 0, cz * 0.1f), new Vector3(0.025f, 0.3f, 0.025f), Iron);
                var flame = Visuals.Box("Flamme", deck, lantern, new Vector3(0.07f, 0.14f, 0.07f), FlameColor);
                flame.AddComponent<FlameFlicker>();
            }
        }

        // Le pupitre « ARC » : les améliorations de l'arc (perforante, transperçante, tir triple, explosive, voir BowUpgrades),
        // sur l'estrade au bord de l'herbe, à gauche du point d'arrivée et tourné vers lui : on s'améliore là où l'on tire.
        // Un pupitre bas (on voit la carte par-dessus) et une petite ardoise posée dessus : niveau, effet et prix de chaque bouton.
        static void BuildBowUpgrades(Transform map, Vector3 pos, float yaw)
        {
            var upgrades = new[] { BowUpgrade.Perforation, BowUpgrade.Transpercante, BowUpgrade.TirTriple, BowUpgrade.Explosion };
            var names = new[] { "PERFORANTE", "TRANSPERÇANTE", "TRIPLE", "EXPLOSIVE" };
            var top = BuildConsole(map, "Arc", pos, upgrades.Length, yaw);
            var root = top.parent;
            float width = upgrades.Length * ConsoleStep + 0.12f;

            // L'ardoise, tenue par deux montants qui portent aussi l'enseigne, derrière les boutons
            const float BoardY = 1.4f, BoardH = 0.6f, SignY = 1.86f, SignH = 0.2f, BoardZ = 0.3f;
            for (int side = -1; side <= 1; side += 2)
                Visuals.Box("Montant", root, new Vector3(side * (width / 2f + 0.025f), (0.85f + SignY + SignH / 2f) / 2f, BoardZ),
                    new Vector3(0.05f, SignY + SignH / 2f - 0.85f, 0.06f), Wood);
            var board = BuildChalkboard(root, "Ardoise", new Vector3(0, BoardY, BoardZ), Quaternion.identity, width - 0.12f, BoardH);
            Visuals.Box("Enseigne", root, new Vector3(0, SignY, BoardZ), new Vector3(width + 0.1f, SignH, 0.05f), DarkWood);
            Visuals.Text(root, "ARC", new Vector3(0, SignY, BoardZ - 0.03f), 0.13f, TitleGold, title: true);

            for (int i = 0; i < upgrades.Length; i++)
            {
                float x = (i - (upgrades.Length - 1) / 2f) * ConsoleStep;
                var label = Visuals.Text(board, "", new Vector3(x, 0.02f, -0.05f), 0.04f, Chalk);
                if (i > 0)   // un trait de craie entre deux colonnes
                    Visuals.Box("Trait", board, new Vector3(x - ConsoleStep / 2f, 0, -0.047f), new Vector3(0.008f, BoardH - 0.1f, 0.004f), new Color(0.6f, 0.62f, 0.58f));

                var button = RoundButton(top, new Vector3(x, 0f, 0.07f));
                button.name = $"Bouton {upgrades[i]}";
                var up = button.gameObject.AddComponent<BowUpgradeButton>();
                button.gameObject.AddComponent<RayPress>();
                up.upgrade = upgrades[i];
                up.cap = button.Find("Bouton");
                up.label = label;

                // Le nom gravé sur une plaque couchée devant le bouton, comme les autres pupitres
                Visuals.Box("Plaque", top, new Vector3(x, 0.004f, -0.14f), new Vector3(ConsoleStep - 0.04f, 0.008f, 0.1f), DarkWood);
                Visuals.Text(top, names[i], new Vector3(x, 0.012f, -0.14f), 0.045f, TitleGold, title: true)   // petit : « TRANSPERÇANTE » tient sur sa plaque
                    .transform.localRotation = Quaternion.Euler(90f, 0, 0);
            }
            UseWoodTexture(root);
        }
    }
}
