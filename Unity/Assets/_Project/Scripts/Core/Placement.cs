using UnityEngine;

namespace SAE
{
    public enum PlacementAction { None, Place, Fuse, PickUp, Blocked }

    // Règles de placement, partagées par le plateau du hub et la carte.
    // pos = position (x, z) en mètres dans le repère de la carte.
    public static class Placement
    {
        public const float Radius = TowerManager.TowerSize / 2f;

        public static PlacementAction Evaluate(Vector2 pos, out PlacedMonkey target)
        {
            var held = GameState.Held;
            target = GameState.Nearest(pos, Radius * 2f);

            if (held == null) return target != null ? PlacementAction.PickUp : PlacementAction.None;
            if (target != null) return target.monkey.CanFuseWith(held.Value) ? PlacementAction.Fuse : PlacementAction.Blocked;
            return MapLayout.CanPlace(pos, Radius) ? PlacementAction.Place : PlacementAction.Blocked;
        }

        public static string Hint(Vector2 pos)
        {
            var action = Evaluate(pos, out var target);
            return action switch
            {
                PlacementAction.PickUp => $"Prendre : {target.monkey}",
                PlacementAction.Fuse => $"FUSION → {target.monkey.Upgraded()}",
                PlacementAction.Place => $"Poser : {GameState.Held}",
                PlacementAction.Blocked => target != null ? "Place occupée" : "Impossible : sur la piste ou hors de la carte",
                _ => "Prends un singe dans la bibliothèque",
            };
        }

        public static void Apply(Vector2 pos)
        {
            var action = Evaluate(pos, out var target);
            switch (action)
            {
                case PlacementAction.PickUp:
                    GameState.Placed.Remove(target);
                    GameState.Held = target.monkey;
                    break;
                case PlacementAction.Fuse:   // 2 identiques → 1 du niveau suivant, à la place de celui posé
                    GameState.Placed.Remove(target);
                    GameState.Placed.Add(new PlacedMonkey(target.monkey.Upgraded(), target.pos));
                    GameState.Held = null;
                    break;
                case PlacementAction.Place:
                    GameState.Placed.Add(new PlacedMonkey(GameState.Held.Value, pos));
                    GameState.Held = null;
                    break;
                default:
                    return;
            }
            GameState.NotifyChanged();
        }
    }
}
