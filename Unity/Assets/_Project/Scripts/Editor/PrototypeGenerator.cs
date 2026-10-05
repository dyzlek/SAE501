using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SAE.EditorTools
{
    // Menu SAE → Générer le prototype : construit les scènes Hub et Map en cubes (greybox)
    // et les ajoute aux Build Profiles. Relancer le menu écrase les deux scènes.
    public static class PrototypeGenerator
    {
        const string Folder = "Assets/_Project/Scenes";
        const string HubPath = Folder + "/Hub.unity";
        const string MapPath = Folder + "/Map.unity";
        const float BoardTile = 0.22f;
        const float MapTile = 3f;

        static readonly Color Floor = new Color(0.35f, 0.35f, 0.38f);
        static readonly Color Wood = new Color(0.45f, 0.30f, 0.18f);
        static readonly Color PathColor = new Color(0.62f, 0.45f, 0.25f);
        static readonly Color Grass1 = new Color(0.30f, 0.62f, 0.28f);
        static readonly Color Grass2 = new Color(0.26f, 0.55f, 0.24f);

        [MenuItem("SAE/Générer le prototype (Hub + Map)")]
        public static void Generate()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Folder);

            NewScene();
            BuildHub();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), HubPath);

            NewScene();
            BuildMap();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), MapPath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(HubPath, true),
                new EditorBuildSettingsScene(MapPath, true),
            };
            EditorSceneManager.OpenScene(HubPath);
            Debug.Log("Prototype généré : Hub.unity et Map.unity. Lance Play depuis Hub.");
        }

        static void NewScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var defaultCam = GameObject.FindWithTag("MainCamera");
            if (defaultCam) Object.DestroyImmediate(defaultCam); // la caméra est celle du joueur
        }

        static void Player(Vector3 position, float yaw, float reach)
        {
            var player = new GameObject("Player");
            player.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0, 0.9f, 0);

            var cam = new GameObject("Camera");
            cam.tag = "MainCamera";
            cam.transform.SetParent(player.transform, false);
            cam.transform.localPosition = new Vector3(0, 1.6f, 0);
            cam.AddComponent<Camera>().nearClipPlane = 0.05f;
            cam.AddComponent<AudioListener>();

            player.AddComponent<PlayerController>().reach = reach;
        }

        static GameObject Root(string name) => new GameObject(name);

        static GameObject MakeActionCube(Transform parent, string name, Vector3 pos, float size, Color color,
            string label, ActionCube.Action action, string scene, string hint)
        {
            var cube = Visuals.Solid(name, parent, pos, Vector3.one * size, color);
            var a = cube.AddComponent<ActionCube>();
            a.action = action;
            a.sceneName = scene;
            a.hint = hint;
            Visuals.Label(cube.transform, label, new Vector3(0, 0.9f, 0), 0.35f);
            // le label est enfant d'un cube mis à l'échelle : on compense
            cube.transform.Find("Label").localScale = Vector3.one / size;
            return cube;
        }

        // ---------------- HUB ----------------
        // Joueur au centre, regard vers +Z. Bibliothèque à gauche, plateau devant, cube Jouer à droite.
        static void BuildHub()
        {
            var env = Root("Environnement").transform;
            Visuals.Solid("Sol", env, new Vector3(0, -0.05f, 1), new Vector3(12, 0.1f, 10), Floor);

            // Plateau (maquette de la carte)
            var table = Visuals.Solid("Table", env, new Vector3(0, 0.4f, 2.2f), new Vector3(2f, 0.8f, 2f), Wood);
            var boardGo = Root("Plateau");
            boardGo.transform.position = new Vector3(0, 0.82f, 2.2f);
            boardGo.AddComponent<Board>().tile = BoardTile;
            BuildGrid(boardGo.transform, BoardTile, 0.04f, true);
            Visuals.Label(boardGo.transform, "PLATEAU\npose et fusionne tes singes", new Vector3(0, 0.9f, 1.1f), 0.12f);

            // Bibliothèque : une ligne par type, une colonne par rareté
            var lib = Root("Bibliotheque").transform;
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
            var ui = Root("Boutons").transform;
            MakeActionCube(ui, "Jouer", new Vector3(3f, 1f, 1.8f), 0.6f, new Color(0.2f, 0.85f, 0.3f),
                "JOUER", ActionCube.Action.LoadScene, "Map", "Aller sur la carte");
            MakeActionCube(ui, "Vider", new Vector3(3f, 0.6f, 0.6f), 0.35f, new Color(0.6f, 0.6f, 0.6f),
                "Vider", ActionCube.Action.ClearBoard, "", "Vider le plateau");

            Player(new Vector3(0, 0, 0.4f), 0f, 5f);
        }

        // Grille 8x8 : cases cliquables (plateau) ou simples dalles (carte).
        static void BuildGrid(Transform parent, float tile, float thickness, bool clickable)
        {
            for (int r = 0; r < MapLayout.Size; r++)
                for (int c = 0; c < MapLayout.Size; c++)
                {
                    char ch = MapLayout.At(r, c);
                    Color color = ch == 'S' ? new Color(0.3f, 0.9f, 0.4f)
                        : ch == 'E' ? new Color(0.9f, 0.25f, 0.25f)
                        : ch == '#' ? PathColor
                        : clickable ? ((r + c) % 2 == 0 ? new Color(0.9f, 0.9f, 0.85f) : new Color(0.2f, 0.2f, 0.22f))
                        : ((r + c) % 2 == 0 ? Grass1 : Grass2);

                    var cell = new GameObject($"Case {r},{c}");
                    cell.transform.SetParent(parent, false);
                    cell.transform.localPosition = MapLayout.CellLocal(r, c, tile);
                    Visuals.Box("Dalle", cell.transform, new Vector3(0, -thickness / 2f, 0),
                        new Vector3(tile * 0.97f, thickness, tile * 0.97f), color);

                    var col = cell.AddComponent<BoxCollider>();
                    col.size = new Vector3(tile, thickness, tile);
                    col.center = new Vector3(0, -thickness / 2f, 0);
                    if (clickable)
                    {
                        var bc = cell.AddComponent<BoardCell>();
                        bc.row = r;
                        bc.col = c;
                    }
                }
        }

        // ---------------- MAP ----------------
        static void BuildMap()
        {
            var map = Root("Carte");
            BuildGrid(map.transform, MapTile, 0.5f, false);
            map.AddComponent<TowerPlacer>().tile = MapTile;
            map.AddComponent<WaveSpawner>().tile = MapTile;

            float edge = MapLayout.Size / 2f * MapTile;
            var env = Root("Environnement").transform;
            Visuals.Solid("Sol autour", env, new Vector3(0, -0.6f, 0), new Vector3(edge * 2 + 20, 0.1f, edge * 2 + 20), Floor);
            Visuals.Solid("Estrade", env, new Vector3(0, -0.25f, -edge - 2.5f), new Vector3(8, 0.5f, 5), Floor);

            MakeActionCube(env, "Retour hub", new Vector3(3f, 0.9f, -edge - 2f), 0.6f, new Color(0.3f, 0.5f, 1f),
                "HUB", ActionCube.Action.LoadScene, "Hub", "Retour au hub");

            Player(new Vector3(0, 0, -edge - 3f), 0f, 60f);
        }
    }
}
