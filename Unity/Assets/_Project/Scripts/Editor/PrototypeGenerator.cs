using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SAE.EditorTools
{
    // Menu SAE → Générer le prototype : construit la scène Jeu en cubes (greybox).
    // Une seule scène, deux zones : le hub (autour de l'origine) et la carte (plus loin en Z).
    // Ainsi le plateau du hub montre la carte en direct. Relancer le menu écrase la scène.
    public static class PrototypeGenerator
    {
        const string Folder = "Assets/_Project/Scenes";
        const string ScenePath = Folder + "/Jeu.unity";
        const float BoardTile = 0.175f;  // plateau de 1,4 m, sur l'établi : assez grand pour reconnaître chaque singe, assez près pour l'atteindre
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

            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var defaultCam = GameObject.FindWithTag("MainCamera");
            if (defaultCam) Object.DestroyImmediate(defaultCam); // la caméra est celle du joueur

            var hubSpawn = Spawn("Spawn Hub", Vector3.zero, canWalk: false);   // au hub, on fait tout du regard
            var mapSpawn = Spawn("Spawn Carte", MapCenter + new Vector3(0, 0, -MapLayout.HalfExtent - 3f), canWalk: true);

            var mapRoot = BuildMap(hubSpawn);
            var spawner = mapRoot.GetComponent<WaveSpawner>();
            var hub = BuildHub(mapRoot, mapSpawn, spawner);   // avant le joueur : l'installeur des bananes ajoute son TestSouris à Camera.main s'il en trouve une
            var player = Player(hubSpawn.position);
            BuildChest(hub, Around(ChestAngle, ChestDistance), player);

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("Prototype généré : " + ScenePath);
        }

        static Transform Spawn(string name, Vector3 pos, bool canWalk)
        {
            var go = new GameObject(name);
            go.AddComponent<SpawnPoint>().canWalk = canWalk;
            go.transform.position = pos;
            return go.transform;
        }

        static Transform Player(Vector3 position)
        {
            var player = new GameObject("Player");
            player.tag = Tags.Joueur;
            player.transform.position = position;
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0, 0.9f, 0);

            // Corps visible : c'est lui qu'on voit en miniature sur le plateau (et plus tard un 2e joueur).
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Corps";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = new Vector3(0, 0.9f, 0);
            body.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
            body.AddComponent<ColorTint>().Set(new Color(1f, 0.55f, 0.1f));
            body.AddComponent<Mirrored>().label = "Toi";

            var cam = new GameObject("Camera");
            cam.tag = "MainCamera";
            cam.transform.SetParent(player.transform, false);
            cam.transform.localPosition = new Vector3(0, 1.6f, 0);
            cam.AddComponent<Camera>().nearClipPlane = 0.05f;
            cam.AddComponent<AudioListener>();

            var controller = player.AddComponent<PlayerController>();
            controller.reach = 60f;
            controller.canWalk = false;           // on apparaît au hub
            player.AddComponent<BananaHand>().reach = HubReach;   // prendre les bananes (clic maintenu), même de loin
            player.AddComponent<MonkeyInfoCard>();   // fiche du singe visé, dans le décor
            return player.transform;
        }

        static ActionCube MakeActionCube(Transform parent, string name, Vector3 pos, float size, Color color,
            string label, ActionCube.Action action, Transform destination, string hint)
        {
            var cube = Visuals.Solid(name, parent, pos, Vector3.one * size, color);
            cube.tag = Tags.Bouton;
            var a = cube.AddComponent<ActionCube>();
            a.action = action;
            a.destination = destination;
            a.hint = hint;
            var text = Visuals.Label(cube.transform, label, new Vector3(0, 0.9f, 0), 0.12f);
            text.transform.localScale = Vector3.one / size; // le cube parent est mis à l'échelle : on compense
            return a;
        }

        // Bouton LANCER : la vague ne part que quand le joueur appuie dessus.
        static void MakeLaunchCube(Transform parent, Vector3 pos, float size, WaveSpawner spawner)
        {
            var a = MakeActionCube(parent, "Lancer la vague", pos, size, new Color(0.95f, 0.45f, 0.15f),
                "LANCER", ActionCube.Action.StartWave, null, "Lancer la vague");
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
        // Le joueur reste AU CENTRE (il ne marche pas au hub) et TOUT EST À PORTÉE DE MAIN (règle de confort VR) :
        // on se tourne vers un élément et on tend le bras. Angles : 0° = devant, positif = à droite.
        //   devant (0°)        : l'établi, avec le plateau incliné (la carte en direct) ; JOUER sur un socle à droite,
        //                        LANCER et Vider à gauche ; le tableau de la vague plus loin, au-dessus, juste à lire
        //   côtés (±75°)       : les deux étagères tournantes de la bibliothèque (une face par type de singe)
        //   derrière-gauche    : le coffre de Nicolas (-112°) et le panneau d'amélioration du bananier (-160°)
        //   derrière-droite    : le bananier (+140°) ; ses bananes tombent sur une table à hauteur de main qui vient
        //                        jusqu'au joueur, avec le panier à côté (+105°) et la caisse plus loin (+100°)
        const float HubReach = 3f;        // portée pour prendre les bananes et ouvrir le coffre depuis le centre (m)
        const float HandHeight = 0.95f;   // hauteur des boutons et des tables : à hauteur de main
        const float ChestAngle = -112f;
        const float ChestDistance = 1.3f;
        const float CarouselDistance = 1.0f;   // centre de l'étagère tournante : sa face avant est à 0,7 m
        const float SlotSize = 0.12f;          // un singe sur l'étagère
        const float SlotStep = 0.14f;          // espace entre deux cases
        const float TreeAngle = 140f;
        const float TreeScale = 1.2f;          // le palmier de Maxens (tronc agrandi), un peu agrandi
        const float CloseUiScale = 0.5f;       // textes du coffre (roulette, consigne, chances, prix) : faits pour être lus de loin, réduits de près

        // Position sur le cercle. angle 0 = devant, positif = à droite.
        static Vector3 Around(float angleDeg, float radius, float height = 0f)
        {
            float a = angleDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(a) * radius, height, Mathf.Cos(a) * radius);
        }

        static Transform BuildHub(Transform mapRoot, Transform mapSpawn, WaveSpawner spawner)
        {
            var env = new GameObject("Hub").transform;
            Visuals.Solid("Sol", env, new Vector3(0, -0.05f, 0), new Vector3(15, 0.1f, 15), Floor);

            // Devant : l'établi avec le plateau, et les boutons sur des socles à portée de main
            BuildBoard(env, mapRoot);
            MakeButton(env, 40f, "Jouer", new Color(0.2f, 0.85f, 0.3f), "JOUER", ActionCube.Action.Teleport, "Aller sur la carte").destination = mapSpawn;
            MakeButton(env, -40f, "Lancer la vague", new Color(0.95f, 0.45f, 0.15f), "LANCER", ActionCube.Action.StartWave, "Lancer la vague").spawner = spawner;
            MakeButton(env, -52f, "Vider", new Color(0.6f, 0.6f, 0.6f), "Vider", ActionCube.Action.ClearBoard, "Vider le plateau");
            BuildWaveBoard(env, Around(0f, 2.3f, 1.9f), Quaternion.identity, spawner);

            // Sur les côtés : les deux étagères tournantes (4 types à gauche, 3 à droite)
            BuildCarousel(env, "Bibliotheque gauche", -75f, 0, 4);
            BuildCarousel(env, "Bibliotheque droite", 75f, 4, 3);

            // Derrière : le bananier à droite avec sa table, son panier et la caisse ; le panneau d'amélioration
            // à gauche (le coffre est placé après le joueur, derrière à gauche lui aussi)
            var bananier = BuildBananas(env, Around(TreeAngle, 2.2f), TreeAngle);
            BuildMoneyBoard(env, 100f, 1.6f);
            if (bananier) BuildUpgradePanel(env, bananier, -160f, 1.1f);
            return env;
        }

        // Un bouton à enfoncer (cube) sur un socle, à portée de main.
        static ActionCube MakeButton(Transform env, float angle, string name, Color color, string label, ActionCube.Action action, string hint)
        {
            const float Size = 0.12f;
            const float Distance = 0.7f;
            float top = HandHeight - Size / 2f;
            var socle = Visuals.Solid($"Socle {name}", env, Around(angle, Distance, top / 2f), new Vector3(0.15f, top, 0.15f), Wood);
            socle.transform.rotation = Quaternion.Euler(0, angle, 0);
            var button = MakeActionCube(env, name, Around(angle, Distance, HandHeight), Size, color, label, action, null, hint);
            // Le texte de MakeActionCube est fait pour être lu de loin : de près, on le réduit et on le pose juste au-dessus
            var text = button.GetComponentInChildren<TextMesh>().transform;
            text.localPosition = new Vector3(0, 0.75f, 0);
            text.localScale *= 0.3f;
            return button;
        }

        // L'établi : le plateau incliné de 30° vers le joueur, à portée de main, posé sur une planche
        // qui suit l'inclinaison + un pied.
        static void BuildBoard(Transform env, Transform mapRoot)
        {
            float scale = BoardTile / MapLayout.Tile;
            float side = MapLayout.Size * BoardTile;
            var center = Around(0f, 1.05f, HandHeight);   // le bord le plus proche est à 0,45 m du joueur

            var boardGo = new GameObject("Plateau");
            boardGo.tag = Tags.Plateau;
            boardGo.transform.SetParent(env, false);
            boardGo.transform.SetPositionAndRotation(center, Quaternion.Euler(-30f, 0, 0));
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

        // Une étagère tournante (Carousel) à portée de main : un meuble carré sur un pied, une face par type
        // (de firstType à firstType+count-1), 8 cases par face (2 rangées de 4 raretés). La face 0 regarde le joueur.
        // On la fait tourner avec la manivelle du dessus.
        static void BuildCarousel(Transform env, string name, float angle, int firstType, int count)
        {
            const int Faces = 4;
            const int Columns = 4;
            const float Half = Columns * SlotStep / 2f;            // demi-largeur d'une face
            float rowLow = HandHeight, rowHigh = HandHeight + SlotStep + 0.04f;

            var root = new GameObject(name).transform;
            root.tag = Tags.Bibliotheque;
            root.SetParent(env, false);
            root.SetPositionAndRotation(Around(angle, CarouselDistance), Quaternion.Euler(0, angle, 0));   // -Z local = vers le joueur
            root.gameObject.AddComponent<Carousel>().faces = Faces;

            // Pied, cœur du meuble, une planche sous chaque rangée, et la manivelle sur le dessus
            Visuals.Box("Pied", root, new Vector3(0, (rowLow - 0.1f) / 2f, 0), new Vector3(0.1f, rowLow - 0.1f, 0.1f), Wood);
            Visuals.Box("Coeur", root, new Vector3(0, (rowLow + rowHigh) / 2f, 0), new Vector3(Half * 2f - 0.1f, rowHigh - rowLow + SlotSize + 0.06f, Half * 2f - 0.1f), Wood);
            foreach (float y in new[] { rowLow, rowHigh })
                Visuals.Box("Planche", root, new Vector3(0, y - SlotSize / 2f - 0.01f, 0), new Vector3(Half * 2f + 0.08f, 0.02f, Half * 2f + 0.08f), Wood);
            var crank = GameObject.CreatePrimitive(PrimitiveType.Cylinder);   // son collider sert à cliquer pour tourner
            crank.name = "Manivelle";
            crank.transform.SetParent(root, false);
            crank.transform.localPosition = new Vector3(0, rowHigh + SlotSize / 2f + 0.06f, 0);
            crank.transform.localScale = new Vector3(0.16f, 0.03f, 0.16f);
            crank.AddComponent<ColorTint>().Set(new Color(0.95f, 0.75f, 0.2f));

            for (int f = 0; f < count; f++)
            {
                var type = (MonkeyType)(firstType + f);
                var faceRotation = Quaternion.Euler(0, f * 360f / Faces, 0);
                // Le nom du type au-dessus de la face, écrit côté extérieur
                var title = Visuals.Label(root, type.ToString(), faceRotation * new Vector3(0, rowHigh + SlotSize / 2f + 0.02f, -Half - 0.05f), 0.04f);
                Object.DestroyImmediate(title.GetComponent<Billboard>());
                title.transform.localRotation = faceRotation;

                for (int l = 0; l < MonkeyData.LevelCount; l++)
                {
                    float x = (l % Columns - (Columns - 1) / 2f) * SlotStep;
                    float y = l < Columns ? rowHigh : rowLow;                  // raretés basses en haut, à lire en premier
                    var slot = new GameObject($"Slot {type} {(Rarity)l}");
                    slot.tag = Tags.Bibliotheque;
                    slot.transform.SetParent(root, false);
                    slot.transform.localPosition = faceRotation * new Vector3(x, y, -Half);
                    slot.transform.localRotation = faceRotation;                // +Z local = vers le cœur, le singe regarde dehors
                    slot.AddComponent<BoxCollider>().size = Vector3.one * (SlotSize + 0.02f);
                    var s = slot.AddComponent<LibrarySlot>();
                    s.type = type;
                    s.level = (Rarity)l;
                    Visuals.MonkeyPiece(new Monkey(type, (Rarity)l), slot.transform, Vector3.zero, SlotSize);   // le modèle dit le type, l'aura la rareté
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
            bananier.gameObject.AddComponent<BananaGuard>();   // plus de bananes coincées dans le bac ou sous les feuilles

            // Le panier : sur un socle à hauteur de main, au bout de la table, à côté du joueur
            var basketPos = Around(105f, 0.8f);
            Visuals.Solid("Socle du panier", env, new Vector3(basketPos.x, HandHeight / 2f - 0.1f, basketPos.z), new Vector3(0.35f, HandHeight - 0.2f, 0.35f), Wood);
            var basket = root.transform.Find("Panier");
            if (basket) basket.position = basketPos + Vector3.up * (HandHeight - 0.2f);

            // Table des bananes : elle part du pied du bananier et vient jusqu'au joueur, à hauteur de main
            // (règle de confort : pas de ramassage au sol). Les bananes tombent tout le long, sur le dessus
            // (de dMin, le bord du bac, à dMin + longueur), sans jamais tomber dans le bac ou dans le panier.
            var treePos = new Vector3(pos.x, 0, pos.z);
            var towardPlayer = (DropTarget - treePos).normalized;
            float dMin = TreeEdge(bananier);
            float length = Vector3.Distance(treePos, DropTarget) - dMin;
            var zonePos = treePos + towardPlayer * (dMin + length / 2f);
            var zone = Visuals.Solid("Table des bananes", env, zonePos + Vector3.up * (TableTop - 0.03f), new Vector3(0.6f, 0.06f, length), new Color(0.95f, 0.85f, 0.35f));
            zone.transform.rotation = Quaternion.LookRotation(towardPlayer);
            var leg = Visuals.Solid("Pied de la table", env, zonePos + Vector3.up * (TableTop - 0.06f) / 2f, new Vector3(0.4f, TableTop - 0.06f, length * 0.8f), Wood);
            leg.transform.rotation = zone.transform.rotation;
            bananier.hauteurAuSol = TableTop + 0.05f;   // les bananes se posent sur la table
            bananier.GetComponent<BananaGuard>().groundY = TableTop + 0.05f;
            bananier.versCible = zone.transform;
            bananier.largeurZone = length;
            bananier.margePanier = -length / 2f;   // la cible est le milieu du tapis : on autorise jusqu'au bout
            bananier.angleDispersion = 6f;           // reste dans la largeur de la table
            return bananier;
        }

        // Bout de la table des bananes, côté joueur : à 0,6 m de lui, à portée de main.
        static readonly Vector3 DropTarget = Around(TreeAngle, 0.6f);
        const float TableTop = HandHeight - 0.1f;   // dessus de la table des bananes

        // Distance du centre du bananier au bord de son bac (même calcul que Bananier.Start, qui la recalcule en jeu).
        static float TreeEdge(Bananier bananier)
        {
            if (!bananier.socle) return bananier.distanceMin;
            var e = bananier.socle.bounds.extents;
            return Mathf.Max(e.x, e.z) + bananier.margeSocle;
        }

        // La caisse : un panneau en bois avec l'argent total en gros chiffres dorés, à côté du panier.
        static void BuildMoneyBoard(Transform env, float angle, float distance)
        {
            var pos = Around(angle, distance);
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
        static void BuildUpgradePanel(Transform env, Bananier bananier, float angle, float distance)
        {
            var pos = Around(angle, distance);
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
            rouletteGo.transform.position = new Vector3(pos.x, top + 0.7f, pos.z);
            rouletteGo.transform.localScale = Vector3.one * CloseUiScale;
            rouletteGo.AddComponent<Sae501.Coffres.Billboard>();
            var roulette = rouletteGo.AddComponent<Sae501.Coffres.RouletteView>();

            var promptGo = new GameObject("PromptCoffre");
            promptGo.transform.SetParent(env, false);
            promptGo.transform.position = new Vector3(pos.x, top + 0.25f, pos.z);
            promptGo.transform.localScale = Vector3.one * CloseUiScale;
            promptGo.AddComponent<Sae501.Coffres.Billboard>();
            var prompt = promptGo.AddComponent<Sae501.Coffres.ChestPrompt>();

            var controller = chest.AddComponent<Sae501.Coffres.ChestController>();
            controller.interactDistance = HubReach;   // le joueur reste au centre : on ouvre le coffre de loin

            controller.roulette = roulette;
            controller.prompt = prompt;
            prompt.chest = controller;
            prompt.player = player;
            chest.AddComponent<ChestClickable>().chest = controller;
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
            oddsRoot.localPosition = new Vector3(0.6f, 1.0f, 0f);
            oddsRoot.localRotation = Quaternion.Euler(0, 180, 0);   // le pivot regarde le joueur : on retourne le texte pour qu'il soit lisible
            oddsRoot.localScale = Vector3.one * CloseUiScale;
            Visuals.Box("Fond", oddsRoot, new Vector3(0, 0, 0.02f), new Vector3(1.15f, 1.5f, 0.02f), new Color(0.1f, 0.09f, 0.08f));
            var oddsText = Visuals.Label(oddsRoot, "", Vector3.zero, 0.055f);
            Object.DestroyImmediate(oddsText.GetComponent<Billboard>());
            var oddsPanel = oddsRoot.gameObject.AddComponent<ChestOddsPanel>();
            oddsPanel.chest = controller;
            oddsPanel.text = oddsText;

            // Prix du coffre, toujours visible au-dessus (doré si on peut payer)
            var tag = new GameObject("Prix du coffre");
            tag.transform.SetParent(env, false);
            tag.transform.position = new Vector3(pos.x, top + 0.5f, pos.z);
            tag.transform.localScale = Vector3.one * CloseUiScale;
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
            Visuals.Solid("Estrade", map.transform, new Vector3(0, -0.25f, -edge - 2.5f), new Vector3(8, 0.5f, 5), Floor);
            Visuals.Solid("Sol autour", map.transform, new Vector3(0, -0.6f, 0), new Vector3(edge * 2 + 20, 0.1f, edge * 2 + 20), Floor);

            MakeActionCube(map.transform, "Retour hub", new Vector3(3f, 0.9f, -edge - 2f), 0.6f, new Color(0.3f, 0.5f, 1f),
                "HUB", ActionCube.Action.Teleport, hubSpawn, "Retour au hub");
            MakeLaunchCube(map.transform, new Vector3(-3f, 0.9f, -edge - 2f), 0.6f, spawner);
            BuildWaveBoard(map.transform, map.transform.position + new Vector3(0, 2.4f, -edge - 0.3f), Quaternion.identity, spawner);
            return map.transform;
        }
    }
}
