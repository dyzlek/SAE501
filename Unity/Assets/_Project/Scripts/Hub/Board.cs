using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Le plateau du hub = la carte en miniature, en direct :
    //   - les singes posés (GameState.Placed), chacun à sa place ;
    //   - les ballons de la vague en cours : la carte (scène Labyrinthe) tourne en arrière-plan, on recopie leur position
    //     (miniature du modèle 3D, comme l'avait fait Maxens, teinte de la couche comprise).
    // La pose des singes est gérée par PlacementSurface, sur le même objet.
    public class Board : MonoBehaviour
    {
        public float scale = 0.05f;        // taille du plateau / taille de la carte

        readonly Dictionary<PlacedMonkey, GameObject> pieces = new Dictionary<PlacedMonkey, GameObject>();
        readonly Dictionary<Balloon, GameObject> balloons = new Dictionary<Balloon, GameObject>();
        // La teinte de chaque miniature et celle de son ballon, gardées pour ne pas les rechercher à chaque image
        readonly Dictionary<Balloon, (ColorTint mini, ColorTint source)> tints = new Dictionary<Balloon, (ColorTint, ColorTint)>();
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

        // Les ballons bougent sans arrêt : on les suit à chaque image
        void LateUpdate()
        {
            var spawner = WaveSpawner.Instance;
            if (!spawner) return;   // la carte n'est pas encore chargée
            var map = spawner.transform;

            foreach (var balloon in Balloon.All)
            {
                if (!balloons.TryGetValue(balloon, out var mini))
                {
                    mini = Miniature(balloon);
                    balloons.Add(balloon, mini);
                    tints[balloon] = (mini.GetComponentInChildren<ColorTint>(), balloon.GetComponent<ColorTint>());
                }
                // Même place sur le plateau que sur la carte, en petit (et la même orientation, pour les dirigeables)
                mini.transform.position = transform.TransformPoint(map.InverseTransformPoint(balloon.transform.position) * scale);
                mini.transform.rotation = transform.rotation * (Quaternion.Inverse(map.rotation) * balloon.transform.rotation);
                mini.transform.localScale = balloon.transform.lossyScale * scale;
                // La couleur de sa couche, seulement quand elle change (Set change de matériau)
                var (miniTint, sourceTint) = tints[balloon];
                if (miniTint && sourceTint && miniTint.color != sourceTint.color) miniTint.Set(sourceTint.color);
            }

            // Ballons éclatés ou sortis : on retire leur miniature
            gone.Clear();
            foreach (var pair in balloons)
                if (!pair.Key || !pair.Key.isActiveAndEnabled) gone.Add(pair.Key);   // éclaté = rangé dans le pool (désactivé)
            foreach (var balloon in gone)
            {
                Destroy(balloons[balloon]);
                balloons.Remove(balloon);
                tints.Remove(balloon);
            }
        }

        // La miniature d'un ballon : une simple boule de 80 faces, à la taille et à la couleur du ballon.
        // Avant, c'était une copie de son modèle 3D : des centaines de modèles complets pour des ballons de 2 cm.
        // Les dirigeables gardent leur forme allongée (la boule est étirée comme leur boîte).
        GameObject Miniature(Balloon balloon)
        {
            var mini = new GameObject($"Miniature {balloon.name}");
            mini.transform.SetParent(transform, false);

            // Boîte du ballon dans son propre repère (sans son échelle ni sa rotation, que LateUpdate recopie à chaque image)
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
            var size = bounds.size;
            var offset = bounds.center;

            var ball = Visuals.Box("Boule", mini.transform, offset, size, balloon.GetComponent<ColorTint>().color);
            ball.GetComponent<MeshFilter>().sharedMesh = MeshBatch.SmallBall;
            ball.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;   // ombre invisible à cette taille
            return mini;
        }
    }
}
