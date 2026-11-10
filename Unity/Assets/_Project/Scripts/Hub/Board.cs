using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Le plateau du hub = la carte en miniature, en direct :
    //   - les singes posés (GameState.Placed), chacun à sa place ;
    //   - les ballons de la vague en cours : la carte (scène Labyrinthe) tourne en arrière-plan, on recopie leur position.
    //     Optimisation (GPU instancing) : les miniatures ne sont plus des objets. À chaque image, on dessine toutes
    //     les boules d'une même couleur en UN appel (Graphics.RenderMeshInstanced) : 500 ballons = quelques appels.
    //     Sauf les boss et les dirigeables : en boule étirée, on ne les reconnaissait plus. Ils sont peu nombreux,
    //     donc ils gardent une vraie copie de leur modèle 3D (Miniature).
    // La pose des singes est gérée par PlacementSurface, sur le même objet.
    public class Board : MonoBehaviour
    {
        public float scale = 0.05f;        // taille du plateau / taille de la carte

        readonly Dictionary<PlacedMonkey, GameObject> pieces = new Dictionary<PlacedMonkey, GameObject>();
        // Pour chaque ballon déjà vu : sa boîte dans son propre repère (centre, taille) et sa teinte. Un ballon recyclé
        // par le pool garde sa sorte, donc sa boîte : on la calcule une seule fois.
        readonly Dictionary<Balloon, (Vector3 center, Vector3 size, ColorTint tint)> shapes = new Dictionary<Balloon, (Vector3, Vector3, ColorTint)>();
        // Les miniatures à dessiner cette image, rangées par couleur (une couleur = un matériau = un appel de dessin)
        readonly Dictionary<Color, List<Matrix4x4>> batches = new Dictionary<Color, List<Matrix4x4>>();
        static Material baseMaterial;   // le matériau des cubes du jeu, qu'on teinte (ColorTint.Shared)
        // Boss et dirigeables : leur miniature (copie du modèle). Un ballon rangé dans le pool garde la sienne, cachée.
        readonly Dictionary<Balloon, GameObject> models = new Dictionary<Balloon, GameObject>();
        readonly List<Balloon> gone = new List<Balloon>();

        void OnEnable()
        {
            GameState.Changed += Sync;
            Sync();
        }

        void OnDisable() => GameState.Changed -= Sync;

        // Les singes : même travail que TowerManager sur la carte, en petit
        void Sync()
        {
            var removed = new List<PlacedMonkey>();
            foreach (var pair in pieces)
                if (!GameState.Placed.Contains(pair.Key)) removed.Add(pair.Key);
            foreach (var p in removed)
            {
                Destroy(pieces[p]);
                pieces.Remove(p);
            }

            float size = TowerManager.TowerSize * scale;
            foreach (var p in GameState.Placed)
            {
                if (pieces.ContainsKey(p)) continue;
                var pos = new Vector3(p.pos.x, TowerManager.TowerSize / 2f, p.pos.y) * scale;
                var piece = Visuals.MonkeyPiece(p.monkey, transform, pos, size, withLabel: false);
                foreach (var r in piece.GetComponentsInChildren<Renderer>())   // singe de 5 cm : son ombre ne se voit pas
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                pieces.Add(p, piece);
            }
        }

        // Les ballons bougent sans arrêt : on les redessine à chaque image
        void LateUpdate()
        {
            HideGoneModels();
            var spawner = WaveSpawner.Instance;
            if (!spawner) return;   // la carte n'est pas encore chargée
            var map = spawner.transform;

            foreach (var list in batches.Values) list.Clear();
            foreach (var balloon in Balloon.All)
            {
                // Même place sur le plateau que sur la carte, en petit (et la même orientation, pour les dirigeables)
                var pos = transform.TransformPoint(map.InverseTransformPoint(balloon.transform.position) * scale);
                var rot = transform.rotation * (Quaternion.Inverse(map.rotation) * balloon.transform.rotation);

                if (HasOwnModel(balloon))
                {
                    if (!models.TryGetValue(balloon, out var mini)) models[balloon] = mini = Miniature(balloon);
                    mini.SetActive(true);
                    mini.transform.SetPositionAndRotation(pos, rot);
                    mini.transform.localScale = balloon.transform.lossyScale * scale;
                    continue;
                }

                if (!shapes.TryGetValue(balloon, out var shape)) shapes[balloon] = shape = Shape(balloon);
                var size = Vector3.Scale(transform.lossyScale, balloon.transform.lossyScale * scale);
                var matrix = Matrix4x4.TRS(pos, rot, size) * Matrix4x4.TRS(shape.center, Quaternion.identity, shape.size);

                var color = shape.tint ? shape.tint.color : Color.white;   // la couleur de sa couche
                if (!batches.TryGetValue(color, out var group)) batches[color] = group = new List<Matrix4x4>();
                group.Add(matrix);
            }

            if (!baseMaterial) baseMaterial = BaseMaterial();
            foreach (var pair in batches)
            {
                if (pair.Value.Count == 0) continue;
                var rp = new RenderParams(ColorTint.Shared(baseMaterial, pair.Key))
                {
                    layer = gameObject.layer,
                    shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,   // ombre invisible à cette taille
                    receiveShadows = false,
                };
                Graphics.RenderMeshInstanced(rp, MeshBatch.SmallBall, 0, pair.Value);   // boule de 80 faces
            }
        }

        static bool HasOwnModel(Balloon b) => b.Model && (b.Kind == BalloonKind.Boss || b.Kind == BalloonKind.Dirigeable);

        // Boss éclaté (rangé dans le pool) : sa miniature est cachée ; détruit avec sa scène : elle part aussi
        void HideGoneModels()
        {
            gone.Clear();
            foreach (var pair in models)
            {
                if (!pair.Key) { gone.Add(pair.Key); Destroy(pair.Value); }
                else if (!pair.Key.isActiveAndEnabled) pair.Value.SetActive(false);
            }
            foreach (var b in gone) models.Remove(b);
        }

        // La miniature d'un boss ou d'un dirigeable : une copie de son modèle 3D, sans collider ni hélice qui tourne
        GameObject Miniature(Balloon balloon)
        {
            var mini = new GameObject($"Miniature {balloon.Kind}");
            mini.transform.SetParent(transform, false);
            var src = balloon.Model.transform;
            var copy = Instantiate(balloon.Model, mini.transform, false);
            copy.transform.SetLocalPositionAndRotation(src.localPosition, src.localRotation);
            copy.transform.localScale = src.localScale;
            BalloonVisuals.ApplyTexture(copy, balloon.Kind);   // la copie ne garde pas la texture posée par le code
            foreach (var spinner in copy.GetComponentsInChildren<Spinner>()) Destroy(spinner);
            foreach (var c in copy.GetComponentsInChildren<Collider>()) Destroy(c);   // une miniature ne se touche pas
            foreach (var r in copy.GetComponentsInChildren<Renderer>())                // ombre invisible à cette taille
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return mini;
        }

        // La boîte d'un ballon dans son propre repère (sans son échelle ni sa rotation, ajoutées à chaque image)
        static (Vector3, Vector3, ColorTint) Shape(Balloon balloon)
        {
            var bounds = new Bounds();
            bool first = true;
            foreach (var r in balloon.GetComponentsInChildren<Renderer>())
            {
                var local = r.localBounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = local.center + Vector3.Scale(local.extents, new Vector3(i & 1, (i >> 1) & 1, (i >> 2) & 1) * 2f - Vector3.one);
                    var p = balloon.transform.InverseTransformPoint(r.transform.TransformPoint(corner));
                    if (first) { bounds = new Bounds(p, Vector3.zero); first = false; }
                    else bounds.Encapsulate(p);
                }
            }
            return (bounds.center, bounds.size, balloon.GetComponent<ColorTint>());
        }

        // Le matériau par défaut des cubes (celui de Visuals.Box), lu une fois sur un cube temporaire
        static Material BaseMaterial()
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var material = cube.GetComponent<Renderer>().sharedMaterial;
            Destroy(cube);
            return material;
        }
    }
}
