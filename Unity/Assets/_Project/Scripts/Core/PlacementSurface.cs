using UnityEngine;

namespace SAE
{
    // Surface où l'on pose les singes : le plateau du hub (scale = taille réduite) ou la carte (scale = 1).
    // Son repère local = le repère de la carte multiplié par 'scale'.
    // On y lâche le singe tenu en main (MonkeyToken). Pendant qu'on le tient au-dessus :
    // singe fantôme vert (on peut poser) ou rouge (on ne peut pas),
    // et halo blanc autour du singe déjà posé quand on peut fusionner.
    public class PlacementSurface : MonoBehaviour, IMonkeyInfo
    {
        public float scale = 1f;

        static readonly Color Ok = new Color(0.3f, 1f, 0.3f);
        static readonly Color No = new Color(1f, 0.2f, 0.2f);

        GameObject ghost;
        Monkey? ghostMonkey;
        GameObject halo;

        Vector2 ToMap(Vector3 worldPoint)
        {
            var local = transform.InverseTransformPoint(worldPoint) / scale;
            return new Vector2(local.x, local.z);
        }

        Vector3 ToLocal(Vector2 mapPos, float height) => new Vector3(mapPos.x, height, mapPos.y) * scale;

        // Lâcher le singe tenu en 'point' : il est posé, ou fusionné avec le singe identique déjà là.
        // false si c'est impossible (place prise par un autre singe, sur la piste…) : il retourne alors à la bibliothèque.
        public bool Drop(Vector3 point)
        {
            var pos = ToMap(point);
            var action = Placement.Evaluate(pos, out _);
            if (action != PlacementAction.Place && action != PlacementAction.Fuse) return false;
            Placement.Apply(pos);
            return true;
        }

        // Fiche du singe posé que l'on vise (sur le plateau ou la carte), avec son cercle de portée.
        public bool TryGetMonkeyInfo(Vector3 point, out Monkey monkey, out Vector3 anchor, out Vector3 rangeCenter, out float rangeScale)
        {
            var placed = GameState.Nearest(ToMap(point), Placement.Radius * 2f);
            monkey = placed?.monkey ?? default;
            rangeCenter = placed != null ? transform.TransformPoint(ToLocal(placed.pos, 0.05f)) : Vector3.zero;
            anchor = rangeCenter + Vector3.up * (TowerManager.TowerSize * scale * 2f + 0.2f);   // au-dessus, sans cacher le singe
            rangeScale = scale;
            return placed != null;
        }

        void LateUpdate()
        {
            // Le singe tenu en main est-il juste au-dessus de cette surface ?
            var held = MonkeyToken.Held;
            var action = PlacementAction.None;
            PlacedMonkey target = null;
            var pos = Vector2.zero;
            if (held && held.TryGetSurfacePoint(out var surface, out var point) && surface == this)
            {
                pos = ToMap(point);
                action = Placement.Evaluate(pos, out target);
            }

            // Fantôme du singe tenu, là où l'on vise
            bool showGhost = action == PlacementAction.Place || action == PlacementAction.Blocked || action == PlacementAction.Fuse;
            if (!showGhost || !GameState.Held.Equals(ghostMonkey))
            {
                if (ghost) Destroy(ghost);
                ghost = null;
            }
            if (showGhost)
            {
                if (!ghost)
                {
                    ghostMonkey = GameState.Held;
                    ghost = Visuals.MonkeyPiece(GameState.Held.Value, transform, Vector3.zero, TowerManager.TowerSize * scale);
                    ghost.name = "Apercu";
                }
                ghost.transform.localPosition = ToLocal(pos, TowerManager.TowerSize * 0.5f);
                ghost.transform.Find("Corps").GetComponent<ColorTint>().Set(action == PlacementAction.Place ? Ok : No);
            }

            // Halo blanc sous le singe avec lequel on peut fusionner
            bool showHalo = action == PlacementAction.Fuse;
            if (showHalo && !halo) halo = CreateHalo();
            if (halo)
            {
                halo.SetActive(showHalo);
                if (showHalo)
                {
                    float pulse = 1f + 0.12f * Mathf.Sin(Time.time * 8f);
                    float d = TowerManager.TowerSize * 1.7f * pulse * scale;
                    halo.transform.localPosition = ToLocal(target.pos, 0.02f);
                    halo.transform.localScale = new Vector3(d, 0.01f * scale + 0.002f, d);
                }
            }
        }

        GameObject CreateHalo()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Halo fusion";
            Visuals.Kill(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.AddComponent<ColorTint>().Set(Color.white);
            return go;
        }
    }
}
