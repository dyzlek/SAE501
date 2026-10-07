using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Le plateau du hub = la carte en miniature : il montre les singes posés (GameState.Placed), chacun à sa place.
    // La vraie carte est dans une autre scène (Labyrinthe), mais les singes posés sont partagés (GameState) :
    // ce qu'on pose ici apparaît sur la carte, et inversement.
    // La pose des singes est gérée par PlacementSurface, sur le même objet.
    public class Board : MonoBehaviour
    {
        public float scale = 0.05f;        // taille du plateau / taille de la carte

        readonly Dictionary<PlacedMonkey, GameObject> pieces = new Dictionary<PlacedMonkey, GameObject>();

        void OnEnable()
        {
            GameState.Changed += Sync;
            Sync();
        }

        void OnDisable() => GameState.Changed -= Sync;

        // Même travail que TowerManager sur la carte, en petit
        void Sync()
        {
            var gone = new List<PlacedMonkey>();
            foreach (var pair in pieces)
                if (!GameState.Placed.Contains(pair.Key)) gone.Add(pair.Key);
            foreach (var p in gone)
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
    }
}
