using UnityEngine;

namespace SAE
{
    // Le plateau façon échiquier : maquette de la carte. Redessine les singes à chaque changement.
    public class Board : MonoBehaviour
    {
        public float tile = 0.22f;

        BoardCell[,] cells;

        void OnEnable()
        {
            cells = new BoardCell[MapLayout.Size, MapLayout.Size];
            foreach (var c in GetComponentsInChildren<BoardCell>()) cells[c.row, c.col] = c;
            GameState.Changed += Refresh;
            Refresh();
        }

        void OnDisable() => GameState.Changed -= Refresh;

        void Refresh()
        {
            foreach (var cell in cells)
            {
                if (!cell) continue;
                var old = cell.transform.Find("Piece");
                if (old)
                {
                    old.name = "Piece (supprimée)"; // Destroy est différé : on renomme pour ne pas la retrouver
                    Destroy(old.gameObject);
                }

                var m = GameState.Board[cell.row, cell.col];
                if (m == null) continue;
                var piece = Visuals.MonkeyPiece(m.Value, cell.transform, new Vector3(0, tile * 0.4f, 0), tile * 0.6f);
                piece.name = "Piece";
            }
        }
    }
}
