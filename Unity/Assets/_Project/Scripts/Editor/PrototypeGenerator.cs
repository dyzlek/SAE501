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
        // Le joueur apparaît au centre, regard vers +Z.
        // Devant : le plateau incliné vers lui. À gauche et à droite : deux meubles de bibliothèque
        // (4 types + 3 types), rangées basses pour rester à portée d'un petit joueur en VR.
        // Derrière : les boutons.
        const float SlotSize = 0.24f;     // ancienne taille ×1,1
        const float SlotStepX = 0.45f;    // espace entre deux raretés
        const float SlotStepY = 0.40f;    // espace entre deux étagères
        const float FirstShelfY = 0.45f;  // rangée la plus basse

        static void BuildHub(Transform mapRoot, Transform mapSpawn)
        {
            var env = new GameObject("Hub").transform;
            Visuals.Solid("Sol", env, new Vector3(0, -0.05f, 0), new Vector3(7, 0.1f, 7), Floor);

            BuildBoard(env, mapRoot);

            // Bibliothèque en deux meubles qui se font face, de part et d'autre du joueur
            BuildShelf(env, "Bibliotheque gauche", new Vector3(-2.0f, 0, 0.6f), -90f, 0, 4);
            BuildShelf(env, "Bibliotheque droite", new Vector3(2.0f, 0, 0.6f), 90f, 4, 3);

            // Boutons derrière le joueur
            MakeActionCube(env, "Jouer", new Vector3(0.6f, 1.0f, -1.3f), 0.35f, new Color(0.2f, 0.85f, 0.3f),
                "JOUER", ActionCube.Action.Teleport, mapSpawn, "Aller sur la carte");
            MakeActionCube(env, "Vider", new Vector3(-0.6f, 0.9f, -1.3f), 0.25f, new Color(0.6f, 0.6f, 0.6f),
                "Vider", ActionCube.Action.ClearBoard, null, "Vider le plateau");
        }

        // Plateau incliné de 25° vers le joueur, posé sur une planche qui suit l'inclinaison + un pied.
        static void BuildBoard(Transform env, Transform mapRoot)
        {
            float scale = BoardTile / MapLayout.Tile;
            float side = MapLayout.Size * BoardTile;
            var center = new Vector3(0, 0.95f, 1.0f);

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
            float legTop = center.y - 0.16f;
            Visuals.Solid("Pied", env, new Vector3(0, legTop / 2f, center.z), new Vector3(0.12f, legTop, 0.12f), Wood);
            Visuals.Solid("Base du pied", env, new Vector3(0, 0.02f, center.z), new Vector3(0.6f, 0.04f, 0.6f), Wood);
        }

        // Un meuble : une étagère par type (de firstType à firstType+count-1), une case par rareté.
        // Repère local : les raretés vont vers +X, les étagères vers le haut, le joueur est du côté -Z.
        static void BuildShelf(Transform env, string name, Vector3 pos, float yaw, int firstType, int count)
        {
            var shelf = new GameObject(name).transform;
            shelf.tag = Tags.Bibliotheque;
            shelf.SetParent(env, false);
            shelf.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));

            float width = MonkeyData.LevelCount * SlotStepX;
            float height = FirstShelfY + count * SlotStepY;
            Visuals.Solid("Fond", shelf, new Vector3(0, height / 2f, 0.25f), new Vector3(width + 0.9f, height, 0.05f), Wood);
            Visuals.Label(shelf, "BIBLIOTHÈQUE", new Vector3(0, height + 0.15f, 0), 0.1f);

            float x0 = -(MonkeyData.LevelCount - 1) / 2f * SlotStepX;
            for (int i = 0; i < count; i++)
            {
                var type = (MonkeyType)(firstType + i);
                float y = FirstShelfY + i * SlotStepY;
                Visuals.Solid($"Etagere {type}", shelf, new Vector3(0, y - SlotSize / 2f - 0.02f, 0.05f), new Vector3(width + 0.9f, 0.03f, 0.45f), Wood);
                Visuals.Label(shelf, type.ToString(), new Vector3(x0 - 0.6f, y, -0.05f), 0.07f);

                for (int l = 0; l < MonkeyData.LevelCount; l++)
                {
                    var slot = new GameObject($"Slot {type} {(Rarity)l}");
                    slot.tag = Tags.Bibliotheque;
                    slot.transform.SetParent(shelf, false);
                    slot.transform.localPosition = new Vector3(x0 + l * SlotStepX, y, 0);
                    slot.AddComponent<BoxCollider>().size = Vector3.one * (SlotSize + 0.04f);
                    var s = slot.AddComponent<LibrarySlot>();
                    s.type = type;
                    s.level = (Rarity)l;
                    Visuals.MonkeyPiece(new Monkey(type, (Rarity)l), slot.transform, Vector3.zero, SlotSize);
                }
            }
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
