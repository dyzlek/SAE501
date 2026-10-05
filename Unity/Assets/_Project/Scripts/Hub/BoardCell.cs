using UnityEngine;

namespace SAE
{
    // Une case du plateau. Avec un singe en main : poser, fusionner (2 identiques → niveau +1) ou échanger.
    // Main vide : reprendre le singe posé.
    public class BoardCell : MonoBehaviour, IClickable
    {
        public int row;
        public int col;

        bool IsPath => MapLayout.IsPath(row, col);
        Monkey? Current { get => GameState.Board[row, col]; set => GameState.Board[row, col] = value; }

        public string Hint
        {
            get
            {
                if (IsPath) return "Piste : on ne pose rien ici";
                var held = GameState.Held;
                var cur = Current;
                if (held == null) return cur == null ? "Case libre" : $"Reprendre : {cur}";
                if (cur == null) return $"Poser : {held}";
                return cur.Value.CanFuseWith(held.Value) ? $"FUSION → {cur.Value.Upgraded()}" : $"Échanger avec : {cur}";
            }
        }

        public void OnClick(PlayerController player)
        {
            if (IsPath) return;
            var held = GameState.Held;
            var cur = Current;

            if (held == null)
            {
                if (cur == null) return;
                GameState.Held = cur;
                Current = null;
            }
            else if (cur == null)
            {
                Current = held;
                GameState.Held = null;
            }
            else if (cur.Value.CanFuseWith(held.Value))
            {
                Current = cur.Value.Upgraded();
                GameState.Held = null;
            }
            else
            {
                Current = held;
                GameState.Held = cur;
            }
            GameState.NotifyChanged();
        }
    }
}
