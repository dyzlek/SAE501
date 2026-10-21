using UnityEngine;

namespace SAE
{
    // Le compteur de singes posés (« SINGES 42 / 60 »), sur une petite plaque en bois au bord de la surface de pose,
    // côté joueur : au bord du plateau du hub et au bord de la carte, près du point d'arrivée.
    // Doré tant qu'il reste de la place, orange quand il en reste peu, rouge et qui bat quand c'est plein
    // (voir Placement.MaxPlaced et docs/opti.md). Ajouté par PlacementSurface.Start.
    // Le texte n'est réécrit que quand le nombre change (GameState.Changed), pas à chaque image.
    public class PlacedCounter : MonoBehaviour
    {
        const int AlmostFull = 50;
        static readonly Color Wood = new Color(0.38f, 0.24f, 0.12f);
        static readonly Color DarkWood = new Color(0.22f, 0.14f, 0.07f);
        static readonly Color Gold = new Color(1f, 0.83f, 0.35f);
        static readonly Color Orange = new Color(1f, 0.55f, 0.2f);
        static readonly Color Red = new Color(1f, 0.3f, 0.25f);

        TextMesh count;
        Transform plate;
        int shown = -1;

        // Pose la plaque au bord de 'surface' (repère carte × scale), tournée vers le joueur (-Z de la surface).
        // size = largeur de la plaque en mètres.
        public static PlacedCounter Build(PlacementSurface surface, float size, float height)
        {
            var go = new GameObject("Compteur de singes");
            go.transform.SetParent(surface.transform, false);
            float edge = MapLayout.HalfExtent * surface.scale;
            go.transform.localPosition = new Vector3(0f, height, -edge - size * 0.3f);
            go.transform.localScale = Vector3.one * size;   // tout est dessiné pour une plaque de 1 m
            return go.AddComponent<PlacedCounter>();
        }

        void Start()
        {
            plate = new GameObject("Plaque").transform;
            plate.SetParent(transform, false);
            Visuals.Box("Cadre", plate, Vector3.zero, new Vector3(1f, 0.42f, 0.05f), DarkWood);
            Visuals.Box("Planche", plate, new Vector3(0f, 0f, -0.02f), new Vector3(0.92f, 0.34f, 0.03f), Wood);
            Visuals.Box("Pied", plate, new Vector3(0f, -0.4f, 0.01f), new Vector3(0.08f, 0.4f, 0.04f), DarkWood);
            Visuals.Text(plate, "SINGES", new Vector3(0f, 0.09f, -0.04f), 0.09f, new Color(0.95f, 0.9f, 0.8f), title: true);
            count = Visuals.Text(plate, "", new Vector3(0f, -0.06f, -0.04f), 0.16f, Gold, title: true);
            Refresh();
        }

        void OnEnable() => GameState.Changed += Refresh;
        void OnDisable() => GameState.Changed -= Refresh;

        void Refresh()
        {
            if (!count) return;
            int n = GameState.Placed.Count;
            if (n == shown) return;
            shown = n;
            count.text = $"{n} / {Placement.MaxPlaced}";
            count.color = Placement.IsFull ? Red : n >= AlmostFull ? Orange : Gold;
        }

        // Plein : la plaque bat doucement pour attirer l'œil (rien sinon)
        void Update()
        {
            if (!plate) return;
            float s = Placement.IsFull ? 1f + 0.06f * Mathf.Sin(Time.time * 6f) : 1f;
            plate.localScale = Vector3.one * s;
        }
    }
}
