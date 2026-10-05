using UnityEngine;

namespace SAE
{
    // Surface où l'on pose les singes : le plateau du hub (scale = taille réduite) ou la carte (scale = 1).
    // Son repère local = le repère de la carte multiplié par 'scale'.
    // Aperçu : singe fantôme vert (on peut poser) ou rouge (on ne peut pas),
    // et halo blanc autour du singe déjà posé quand on peut fusionner.
    public class PlacementSurface : MonoBehaviour, IClickable
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

        public string GetHint(Vector3 point) => Placement.Hint(ToMap(point));

        public void OnClick(PlayerController player, Vector3 point) => Placement.Apply(ToMap(point));

        void LateUpdate()
        {
            var player = PlayerController.Local;
            bool aimed = player && player.enabled && ReferenceEquals(player.Target, this);
            var action = PlacementAction.None;
            PlacedMonkey target = null;
            var pos = Vector2.zero;
            if (aimed)
            {
                pos = ToMap(player.AimPoint);
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
