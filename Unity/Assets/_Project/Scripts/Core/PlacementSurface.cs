using UnityEngine;

namespace SAE
{
    // Surface où l'on pose les singes : le plateau du hub (scale = taille réduite) ou la carte (scale = 1).
    // Son repère local = le repère de la carte multiplié par 'scale'.
    // On y lâche le singe tenu en main (MonkeyToken). Pendant qu'on le tient au-dessus :
    // singe fantôme vert (on peut poser) ou rouge (on ne peut pas) ;
    // si on peut fusionner, le fantôme DORÉ montre le singe qu'on va obtenir (rareté suivante), au-dessus du singe posé,
    // avec un halo doré qui pulse dessous (avant : un fantôme rouge, on croyait que c'était interdit).
    public class PlacementSurface : MonoBehaviour, IMonkeyInfo
    {
        public float scale = 1f;

        static readonly Color Ok = new Color(0.3f, 1f, 0.3f);
        static readonly Color No = new Color(1f, 0.2f, 0.2f);
        static readonly Color Fusion = new Color(1f, 0.82f, 0.25f);

        GameObject ghost;
        Monkey? ghostMonkey;
        GameObject halo;
        LineRenderer rangeRing;

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

            // Fantôme : le singe tenu là où l'on vise, ou (fusion) le singe qu'on va obtenir, au-dessus de celui posé.
            // Sans nom au-dessus : le texte cachait l'endroit où l'on pose.
            bool fuse = action == PlacementAction.Fuse;
            bool showGhost = action == PlacementAction.Place || action == PlacementAction.Blocked || fuse;
            Monkey? wanted = !showGhost ? (Monkey?)null : fuse ? target.monkey.Upgraded() : GameState.Held;
            if (!showGhost || !wanted.Equals(ghostMonkey))
            {
                if (ghost) Destroy(ghost);
                ghost = null;
            }
            if (showGhost)
            {
                if (!ghost)
                {
                    ghostMonkey = wanted;
                    ghost = Visuals.MonkeyPiece(wanted.Value, transform, Vector3.zero, TowerManager.TowerSize * scale, withLabel: false);
                    ghost.name = "Apercu";
                }
                float lift = fuse ? 1.5f + 0.15f * Mathf.Sin(Time.time * 6f) : 0.5f;   // le singe obtenu flotte et danse au-dessus
                ghost.transform.localPosition = ToLocal(fuse ? target.pos : pos, TowerManager.TowerSize * lift);
                ghost.GetComponent<MonkeyView>().Tint(fuse ? Fusion : action == PlacementAction.Place ? Ok : No);
            }

            // La portée du singe qu'on va poser (ou obtenir) : on voit ce qu'il couvrira avant de le lâcher
            float range = showGhost ? MonkeyData.Range(wanted.Value) : 0f;
            bool showRange = showGhost && range < 30f;   // pas de cercle géant pour le Sniper (toute la carte)
            if (showRange && !rangeRing) rangeRing = CreateRangeRing();
            if (rangeRing)
            {
                rangeRing.gameObject.SetActive(showRange);
                if (showRange) DrawRangeRing(ToLocal(fuse ? target.pos : pos, 0.03f), range * scale);
            }

            // Halo doré sous le singe avec lequel on peut fusionner
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

        // Le cercle de portée : une ligne fermée posée à plat sur la surface (il suit l'inclinaison du plateau)
        const int RingPoints = 48;

        LineRenderer CreateRangeRing()
        {
            var line = new GameObject("Portée").AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.sharedMaterial = Visuals.LineMaterial;
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = RingPoints;
            line.startColor = line.endColor = new Color(1f, 1f, 1f, 0.7f);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return line;
        }

        void DrawRangeRing(Vector3 localCenter, float localRadius)
        {
            rangeRing.widthMultiplier = Mathf.Max(0.004f, localRadius * 0.015f) * transform.lossyScale.x;
            for (int i = 0; i < RingPoints; i++)
            {
                float a = i * Mathf.PI * 2f / RingPoints;
                rangeRing.SetPosition(i, localCenter + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * localRadius);
            }
        }

        GameObject CreateHalo()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Halo fusion";
            Visuals.Kill(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.AddComponent<ColorTint>().Set(Fusion);
            return go;
        }
    }
}
