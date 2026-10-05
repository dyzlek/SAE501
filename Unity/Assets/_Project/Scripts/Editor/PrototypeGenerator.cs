using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SAE.EditorTools
{
    // Menu SAE → Générer le prototype : construit la scène Jeu en cubes (greybox).
    // Une seule scène, deux zones : le hub (autour de l'origine) et la carte (plus loin en Z).
    // Ainsi le plateau du hub montre la carte en direct. Relancer le menu écrase la scène.
    public static class PrototypeGenerator
    {
        const string Folder = "Assets/_Project/Scenes";
        const string ScenePath = Folder + "/Jeu.unity";
        const float BoardTile = 0.22f;
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

            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var defaultCam = GameObject.FindWithTag("MainCamera");
            if (defaultCam) Object.DestroyImmediate(defaultCam); // la caméra est celle du joueur

            var hubSpawn = Spawn("Spawn Hub", new Vector3(0, 0, 0.4f));
            var mapSpawn = Spawn("Spawn Carte", MapCenter + new Vector3(0, 0, -MapLayout.HalfExtent - 3f));

            var mapRoot = BuildMap(hubSpawn);
            BuildHub(mapRoot, mapSpawn);
            Player(hubSpawn.position);

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("Prototype généré : " + ScenePath);
        }

        static Transform Spawn(string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            return go.transform;
        }

        static void Player(Vector3 position)
        {
            var player = new GameObject("Player");
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

            player.AddComponent<PlayerController>().reach = 60f;
        }

        static void MakeActionCube(Transform parent, string name, Vector3 pos, float size, Color color,
            string label, ActionCube.Action action, Transform destination, string hint)
        {
            var cube = Visuals.Solid(name, parent, pos, Vector3.one * size, color);
            var a = cube.AddComponent<ActionCube>();
            a.action = action;
            a.destination = destination;
            a.hint = hint;
            var text = Visuals.Label(cube.transform, label, new Vector3(0, 0.9f, 0), 0.35f);
            text.transform.localScale = Vector3.one / size; // le cube parent est mis à l'échelle : on compense
        }

        // ---------------- HUB ----------------
        // Joueur regard vers +Z. Bibliothèque à gauche, plateau devant, cube Jouer à droite.
        static void BuildHub(Transform mapRoot, Transform mapSpawn)
        {
            var env = new GameObject("Hub").transform;
            Visuals.Solid("Sol", env, new Vector3(0, -0.05f, 1), new Vector3(12, 0.1f, 10), Floor);

            // Plateau : la carte en miniature
            Visuals.Solid("Table", env, new Vector3(0, 0.4f, 2.2f), new Vector3(2f, 0.8f, 2f), Wood);
            var boardGo = new GameObject("Plateau");
            boardGo.transform.SetParent(env, false);
            boardGo.transform.position = new Vector3(0, 0.84f, 2.2f);
            var board = boardGo.AddComponent<Board>();
            board.mapRoot = mapRoot;
            board.scale = BoardTile / MapLayout.Tile;
            var col = boardGo.AddComponent<BoxCollider>();
            col.size = new Vector3(MapLayout.Size * BoardTile, 0.04f, MapLayout.Size * BoardTile);
            col.center = new Vector3(0, -0.02f, 0);
            BuildGrid(boardGo.transform, BoardTile, 0.04f, false);
            Visuals.Label(boardGo.transform, "PLATEAU = la carte en direct\npose et fusionne tes singes", new Vector3(0, 0.9f, 1.1f), 0.12f);

            // Bibliothèque : une ligne par type, une colonne par rareté
            var lib = new GameObject("Bibliotheque").transform;
            lib.SetParent(env, false);
            lib.position = new Vector3(-3f, 0, 0);
            Visuals.Solid("Fond", lib, new Vector3(-0.35f, 1.4f, 1.0f), new Vector3(0.1f, 2.8f, 3.4f), Wood);
            Visuals.Label(lib, "BIBLIOTHÈQUE", new Vector3(0, 2.75f, 1.0f), 0.18f);
            const float step = 0.36f;
            for (int t = 0; t < MonkeyData.TypeCount; t++)
            {
                float y = 0.4f + t * step;
                Visuals.Solid($"Etagere {t}", lib, new Vector3(-0.1f, y - 0.15f, 1.0f), new Vector3(0.45f, 0.03f, 3.2f), Wood);
                Visuals.Label(lib, ((MonkeyType)t).ToString(), new Vector3(0, y, -0.75f), 0.08f);
                for (int l = 0; l < MonkeyData.LevelCount; l++)
                {
                    var slot = new GameObject($"Slot {(MonkeyType)t} {(Rarity)l}");
                    slot.transform.SetParent(lib, false);
                    slot.transform.localPosition = new Vector3(0, y, -0.25f + l * step);
                    slot.AddComponent<BoxCollider>().size = Vector3.one * 0.26f;
                    var s = slot.AddComponent<LibrarySlot>();
                    s.type = (MonkeyType)t;
                    s.level = (Rarity)l;
                    Visuals.MonkeyPiece(new Monkey(s.type, s.level), slot.transform, Vector3.zero, 0.22f);
                }
            }

            // Boutons à droite
            MakeActionCube(env, "Jouer", new Vector3(3f, 1f, 1.8f), 0.6f, new Color(0.2f, 0.85f, 0.3f),
                "JOUER", ActionCube.Action.Teleport, mapSpawn, "Aller sur la carte");
            MakeActionCube(env, "Vider", new Vector3(3f, 0.6f, 0.6f), 0.35f, new Color(0.6f, 0.6f, 0.6f),
                "Vider", ActionCube.Action.ClearBoard, null, "Vider le plateau");
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
                    if (walkable) Visuals.Solid($"Case {r},{c}", parent, pos, size, color);
                    else Visuals.Box($"Case {r},{c}", parent, pos, size, color);
                }
        }

        // ---------------- CARTE ----------------
        static Transform BuildMap(Transform hubSpawn)
        {
            var map = new GameObject("Carte");
            map.transform.position = MapCenter;
            BuildGrid(map.transform, MapLayout.Tile, 0.5f, true);
            map.AddComponent<TowerManager>();
            map.AddComponent<WaveSpawner>();

            float edge = MapLayout.HalfExtent;
            Visuals.Solid("Estrade", map.transform, new Vector3(0, -0.25f, -edge - 2.5f), new Vector3(8, 0.5f, 5), Floor);
            Visuals.Solid("Sol autour", map.transform, new Vector3(0, -0.6f, 0), new Vector3(edge * 2 + 20, 0.1f, edge * 2 + 20), Floor);

            MakeActionCube(map.transform, "Retour hub", new Vector3(3f, 0.9f, -edge - 2f), 0.6f, new Color(0.3f, 0.5f, 1f),
                "HUB", ActionCube.Action.Teleport, hubSpawn, "Retour au hub");
            return map.transform;
        }
    }
}
