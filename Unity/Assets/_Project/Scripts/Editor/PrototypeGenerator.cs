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
        const float BoardTile = 0.15f;   // plateau de 1,2 m : à portée de bras
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

            var hubSpawn = Spawn("Spawn Hub", Vector3.zero);
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

            player.AddComponent<PlayerController>().reach = 60f;
        }

        static void MakeActionCube(Transform parent, string name, Vector3 pos, float size, Color color,
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
        }

        // ---------------- HUB ----------------
        // Pensé pour la VR : le joueur est au centre (point d'apparition) et tout est autour de lui,
        // à portée de bras (~0,85 m), tourné vers lui. Devant : le plateau incliné. À gauche : la bibliothèque.
        // À droite : le choix de la rareté. Derrière : les boutons.
        const float Reach = 0.85f;
        const float HandHeight = 1.05f;

        // Position sur un cercle autour du joueur. angle 0 = devant, négatif = à gauche.
        static Vector3 Around(float angleDeg, float radius, float height)
        {
            float a = angleDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(a) * radius, height, Mathf.Cos(a) * radius);
        }

        static Quaternion FacingCenter(Vector3 pos) => Quaternion.LookRotation(new Vector3(pos.x, 0, pos.z));

        static void Pillar(Transform parent, Vector3 top)
        {
            Visuals.Solid("Socle", parent, new Vector3(top.x, (top.y - 0.1f) / 2f, top.z), new Vector3(0.08f, top.y - 0.1f, 0.08f), Wood);
        }

        static void BuildHub(Transform mapRoot, Transform mapSpawn)
        {
            var env = new GameObject("Hub").transform;
            Visuals.Solid("Sol", env, new Vector3(0, -0.05f, 0), new Vector3(6, 0.1f, 6), Floor);

            // Plateau incliné devant le joueur : la carte en direct, et la surface où l'on pose les singes
            float scale = BoardTile / MapLayout.Tile;
            Visuals.Solid("Pied du plateau", env, new Vector3(0, 0.4f, 1.0f), new Vector3(0.6f, 0.8f, 0.6f), Wood);
            var boardGo = new GameObject("Plateau");
            boardGo.tag = Tags.Plateau;
            boardGo.transform.SetParent(env, false);
            boardGo.transform.SetPositionAndRotation(new Vector3(0, 0.9f, 1.0f), Quaternion.Euler(-25f, 0, 0));
            var board = boardGo.AddComponent<Board>();
            board.mapRoot = mapRoot;
            board.scale = scale;
            boardGo.AddComponent<PlacementSurface>().scale = scale;
            var col = boardGo.AddComponent<BoxCollider>();
            col.size = new Vector3(MapLayout.Size * BoardTile, 0.04f, MapLayout.Size * BoardTile);
            col.center = new Vector3(0, -0.02f, 0);
            BuildGrid(boardGo.transform, BoardTile, 0.04f, false);

            // Bibliothèque à gauche : un socle par type, en arc
            var lib = new GameObject("Bibliotheque").transform;
            lib.tag = Tags.Bibliotheque;
            lib.SetParent(env, false);
            Visuals.Label(lib, "SINGES", Around(-80f, Reach, HandHeight + 0.35f), 0.07f);
            for (int t = 0; t < MonkeyData.TypeCount; t++)
            {
                var pos = Around(-110f + t * 10f, Reach, HandHeight);
                Pillar(lib, pos);
                var slot = new GameObject($"Socle {(MonkeyType)t}");
                slot.tag = Tags.Bibliotheque;
                slot.transform.SetParent(lib, false);
                slot.transform.SetPositionAndRotation(pos, FacingCenter(pos));
                slot.AddComponent<BoxCollider>().size = Vector3.one * 0.16f;
                slot.AddComponent<LibrarySlot>().type = (MonkeyType)t;
                Visuals.Label(slot.transform, ((MonkeyType)t).ToString(), new Vector3(0, 0.17f, 0), 0.035f);
            }

            // Choix de la rareté à droite : 8 pastilles de couleur
            var picker = new GameObject("Rarete").transform;
            picker.SetParent(env, false);
            Visuals.Label(picker, "RARETÉ", Around(85f, Reach, HandHeight + 0.3f), 0.07f);
            for (int l = 0; l < MonkeyData.LevelCount; l++)
            {
                var r = (Rarity)l;
                var pos = Around(50f + l * 10f, Reach, HandHeight);
                Pillar(picker, pos);
                var button = Visuals.Solid($"Rarete {r}", picker, pos, Vector3.one * 0.07f, MonkeyData.RarityColor(r));
                button.transform.rotation = FacingCenter(pos);
                button.tag = Tags.Bouton;
                button.GetComponent<ColorTint>().Set(MonkeyData.RarityColor(r), MonkeyData.IsRainbow(r));
                button.AddComponent<RarityButton>().rarity = r;
            }

            // Boutons derrière le joueur
            MakeActionCube(env, "Jouer", Around(150f, 1.0f, 1.0f), 0.3f, new Color(0.2f, 0.85f, 0.3f),
                "JOUER", ActionCube.Action.Teleport, mapSpawn, "Aller sur la carte");
            MakeActionCube(env, "Vider", Around(-150f, 1.0f, 0.9f), 0.2f, new Color(0.6f, 0.6f, 0.6f),
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
