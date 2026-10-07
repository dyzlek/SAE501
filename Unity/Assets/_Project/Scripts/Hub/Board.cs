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
                pieces.Add(p, Visuals.MonkeyPiece(p.monkey, transform, pos, size, withLabel: false));
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
                }
                // Même place sur le plateau que sur la carte, en petit (et la même orientation, pour les dirigeables)
                mini.transform.position = transform.TransformPoint(map.InverseTransformPoint(balloon.transform.position) * scale);
                mini.transform.rotation = transform.rotation * (Quaternion.Inverse(map.rotation) * balloon.transform.rotation);
                mini.transform.localScale = balloon.transform.lossyScale * scale;
                var color = balloon.GetComponent<ColorTint>().color;   // la couleur de sa couche
                foreach (var tint in mini.GetComponentsInChildren<ColorTint>()) tint.Set(color);
            }

            // Ballons éclatés ou sortis : on retire leur miniature
            gone.Clear();
            foreach (var pair in balloons)
                if (!pair.Key) gone.Add(pair.Key);
            foreach (var balloon in gone)
            {
                Destroy(balloons[balloon]);
                balloons.Remove(balloon);
            }
        }

        // La miniature d'un ballon : une copie de son modèle 3D (ou de sa sphère s'il n'en a pas)
        GameObject Miniature(Balloon balloon)
        {
            var mini = new GameObject($"Miniature {balloon.name}");
            mini.transform.SetParent(transform, false);
            if (balloon.Model)
            {
                var src = balloon.Model.transform;
                var copy = Instantiate(balloon.Model, mini.transform, false);
                copy.transform.SetLocalPositionAndRotation(src.localPosition, src.localRotation);
                copy.transform.localScale = src.localScale;
                BalloonVisuals.ApplyTexture(copy, balloon.Kind);   // la copie ne garde pas la texture posée par le code
                foreach (var spinner in copy.GetComponentsInChildren<Spinner>()) Destroy(spinner);   // pas d'hélice qui tourne en petit
                if (!balloon.TintedModel)                          // seuls les ballons « teintés » changent de couleur
                    foreach (var tint in copy.GetComponentsInChildren<ColorTint>()) Destroy(tint);
            }
            else
            {
                mini.AddComponent<MeshFilter>().sharedMesh = balloon.GetComponent<MeshFilter>().sharedMesh;
                mini.AddComponent<MeshRenderer>().sharedMaterial = balloon.GetComponent<MeshRenderer>().sharedMaterial;
                mini.AddComponent<ColorTint>();
            }
            foreach (var c in mini.GetComponentsInChildren<Collider>()) Destroy(c);   // une miniature ne se touche pas
            return mini;
        }
    }
}
