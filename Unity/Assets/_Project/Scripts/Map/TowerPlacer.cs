using UnityEngine;

namespace SAE
{
    // Au chargement de la carte, recrée en grand les singes posés sur le plateau du hub.
    public class TowerPlacer : MonoBehaviour
    {
        public float tile = 3f;
        public float towerSize = 1.2f;

        void Start()
        {
            for (int r = 0; r < MapLayout.Size; r++)
                for (int c = 0; c < MapLayout.Size; c++)
                {
                    var m = GameState.Board[r, c];
                    if (m == null) continue;
                    var pos = MapLayout.CellLocal(r, c, tile) + Vector3.up * towerSize / 2f;
                    var piece = Visuals.MonkeyPiece(m.Value, transform, pos, towerSize);
                    piece.AddComponent<Tower>().Init(m.Value);
                }
        }
    }
}
