using UnityEngine;

namespace SAE
{
    // La salle du casino (scène Casino), construite par le code au lancement : le décor seulement, les jeux sont posés
    // à part par le générateur (RouletteTable, BlackjackTable, MonkeyRace, DartStand, CasinoCar).
    // Style art déco, chaud et doré, inspiré de « Gamble With Your Friends » :
    //   - une moquette rouge sombre à losanges dorés, dessinée par le code (une petite texture répétée, pas de fichier) ;
    //   - des murs bleu canard, un lambris en bois en bas, des moulures dorées, des colonnes crème à chapiteau doré ;
    //   - pas de plafond plein : une verrière, une grille de poutres sombres qui laisse voir le ciel (et le soleil
    //     dessine la grille sur la moquette) ;
    //   - trois lustres dorés, une grande enseigne « CASINO DES SINGES » au fond, des palmiers en pot, des machines à sous.
    // Pour le casque (72 fps) : tout le décor est fusionné au lancement en un maillage par couleur (MeshBatch),
    // et seules trois lampes éclairent en plus du soleil, sans ombres.
    // Repère local : origine au milieu du sol, l'entrée (la voiture) côté -Z, l'enseigne au fond côté +Z.
    public class CasinoRoom : MonoBehaviour
    {
        public Renderer floor;   // le sol (posé par le générateur, c'est lui qui porte la zone de téléportation)

        public const float Width = 18f, Depth = 22f, Height = 5f;   // m

        static readonly Color Teal = new Color(0.07f, 0.24f, 0.27f);
        static readonly Color Panel = new Color(0.32f, 0.18f, 0.1f);
        static readonly Color Gold = new Color(1f, 0.78f, 0.3f);
        static readonly Color Cream = new Color(0.95f, 0.9f, 0.78f);
        static readonly Color Beam = new Color(0.1f, 0.1f, 0.14f);
        static readonly Color Bulb = new Color(1f, 0.95f, 0.75f);
        static readonly Color Purple = new Color(0.22f, 0.08f, 0.3f);
        static readonly Color Pink = new Color(1f, 0.35f, 0.65f);
        static readonly Color Pot = new Color(0.55f, 0.2f, 0.12f);
        static readonly Color Trunk = new Color(0.5f, 0.36f, 0.2f);
        static readonly Color Leaf = new Color(0.2f, 0.55f, 0.22f);
        static readonly Color Slot = new Color(0.75f, 0.1f, 0.15f);
        static readonly Color Screen = new Color(0.1f, 0.1f, 0.15f);
        static readonly Color Banana = new Color(1f, 0.85f, 0.2f);

        Transform decor;   // tout ce qui ne bouge pas, fusionné à la fin

        void Awake()
        {
            decor = new GameObject("Décor").transform;
            decor.SetParent(transform, false);
            Carpet();
            Walls();
            Glasshouse();
            Sign();
            foreach (var x in new[] { -5f, 0f, 5f }) Chandelier(new Vector3(x, Height - 0.9f, 1f));
            foreach (var p in new[] { new Vector3(-8f, 0f, -10f), new Vector3(8f, 0f, -10f), new Vector3(-8f, 0f, 10f), new Vector3(8f, 0f, 10f),
                                      new Vector3(-8f, 0f, 6f), new Vector3(8f, 0f, 6f) })
                Palm(p);
            foreach (var z in new[] { -5.75f, -4.25f, -2.75f, -1.25f }) SlotMachine(new Vector3(-8.3f, 0f, z), -90f);   // entre les colonnes, face à la salle
            Merge();
        }

        // Pour le casque : les centaines de formes du décor deviennent UN maillage par couleur (MeshBatch, comme le décor
        // de la carte). Les formes gardent leur collider (murs, machines) mais ne sont plus dessinées une par une.
        void Merge()
        {
            var batch = new MeshBatch();
            var toDecor = decor.worldToLocalMatrix;
            foreach (var r in decor.GetComponentsInChildren<MeshRenderer>())
            {
                var tint = r.GetComponent<ColorTint>();
                var filter = r.GetComponent<MeshFilter>();
                if (!tint || !filter) continue;
                batch.Add(filter.sharedMesh, toDecor * r.localToWorldMatrix, tint.color);
                r.enabled = false;
            }
            batch.Build(decor, "Décor fusionné");
        }

        // --- La moquette ---

        // Une texture de 128 × 128 : fond rouge sombre, un losange doré fin, un rond bleu canard au milieu. Répétée tous les 2 m.
        void Carpet()
        {
            if (!floor) return;
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGB24, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear };
            var red = new Color(0.36f, 0.05f, 0.08f);
            var dark = new Color(0.25f, 0.03f, 0.06f);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size - 0.5f, v = (y + 0.5f) / size - 0.5f;
                    float diamond = Mathf.Abs(u) + Mathf.Abs(v);                 // 0 au centre, 0,5 sur le losange, 1 aux coins
                    float dot = Mathf.Sqrt(u * u + v * v);
                    var c = diamond < 0.5f ? red : dark;
                    if (Mathf.Abs(diamond - 0.5f) < 0.018f || Mathf.Abs(diamond - 0.38f) < 0.008f) c = Gold * 0.85f;
                    if (dot < 0.06f) c = Teal * 1.6f;
                    pixels[y * size + x] = c;
                }
            tex.SetPixels(pixels);
            tex.Apply(true);
            var mat = new Material(Visuals.PrimitiveMaterial) { name = "Moquette du casino" };
            mat.SetTexture("_BaseMap", tex);
            mat.SetTextureScale("_BaseMap", new Vector2(Width / 2f, Depth / 2f));
            floor.sharedMaterial = mat;
            var tint = floor.GetComponent<ColorTint>();
            if (tint) tint.Set(Color.white);
        }

        // --- Les murs ---

        void Walls()
        {
            float hw = Width / 2f, hd = Depth / 2f;
            Wall(new Vector3(0f, 0f, hd), Width, 0f);          // le fond
            Wall(new Vector3(0f, 0f, -hd), Width, 180f);       // l'entrée
            Wall(new Vector3(hw, 0f, 0f), Depth, 90f);         // droite
            Wall(new Vector3(-hw, 0f, 0f), Depth, -90f);       // gauche
        }

        // Un mur : son repère a +Z vers dehors ; le lambris, les moulures et les colonnes sont sur sa face intérieure (-Z)
        void Wall(Vector3 pos, float length, float yaw)
        {
            var w = new GameObject("Mur").transform;
            w.SetParent(decor, false);
            w.localPosition = pos;
            w.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Visuals.Solid("Mur", w, new Vector3(0f, Height / 2f, 0.15f), new Vector3(length + 0.3f, Height, 0.3f), Teal);
            Visuals.Box("Lambris", w, new Vector3(0f, 0.55f, -0.02f), new Vector3(length, 1.1f, 0.04f), Panel);
            Visuals.Box("Moulure basse", w, new Vector3(0f, 1.12f, -0.05f), new Vector3(length, 0.06f, 0.06f), Gold);
            Visuals.Box("Moulure haute", w, new Vector3(0f, Height - 0.5f, -0.05f), new Vector3(length, 0.08f, 0.06f), Gold);
            Visuals.Box("Plinthe", w, new Vector3(0f, 0.06f, -0.05f), new Vector3(length, 0.12f, 0.06f), Panel * 0.7f);
            for (float x = -length / 2f + 1.5f; x < length / 2f - 1f; x += 3f)
            {
                Visuals.Box("Colonne", w, new Vector3(x, Height / 2f, -0.12f), new Vector3(0.36f, Height, 0.2f), Cream);
                Visuals.Box("Chapiteau", w, new Vector3(x, Height - 0.3f, -0.15f), new Vector3(0.5f, 0.2f, 0.26f), Gold);
                Visuals.Box("Base", w, new Vector3(x, 0.15f, -0.15f), new Vector3(0.48f, 0.3f, 0.26f), Gold * 0.8f);
            }
        }

        // La verrière : une grille de poutres sombres tous les 2 m, et un cadre plus épais autour
        void Glasshouse()
        {
            for (float x = -Width / 2f; x <= Width / 2f + 0.01f; x += 2f)
                Visuals.Box("Poutre", decor, new Vector3(x, Height, 0f), new Vector3(0.12f, 0.16f, Depth), Beam);
            for (float z = -Depth / 2f; z <= Depth / 2f + 0.01f; z += 2f)
                Visuals.Box("Poutre", decor, new Vector3(0f, Height, z), new Vector3(Width, 0.16f, 0.12f), Beam);
            Visuals.Box("Corniche", decor, new Vector3(0f, Height - 0.15f, Depth / 2f - 0.1f), new Vector3(Width, 0.3f, 0.3f), Gold * 0.7f);
            Visuals.Box("Corniche", decor, new Vector3(0f, Height - 0.15f, -Depth / 2f + 0.1f), new Vector3(Width, 0.3f, 0.3f), Gold * 0.7f);
        }

        // L'enseigne du fond : un grand panneau violet bordé d'or et d'ampoules, « CASINO DES SINGES » et deux bananes
        void Sign()
        {
            float z = Depth / 2f - 0.12f, y = 3.55f;
            Visuals.Box("Enseigne", decor, new Vector3(0f, y, z), new Vector3(7.4f, 1.3f, 0.06f), Purple);
            Visuals.Box("Cadre doré", decor, new Vector3(0f, y, z + 0.02f), new Vector3(7.6f, 1.5f, 0.05f), Gold);
            for (int i = 0; i < 24; i++)   // les ampoules tout autour du cadre
            {
                float t = i / 24f * 2f;
                var p = t < 1f ? new Vector3(Mathf.Lerp(-3.75f, 3.75f, t), 0.72f, 0f) : new Vector3(Mathf.Lerp(3.75f, -3.75f, t - 1f), -0.72f, 0f);
                Ball("Ampoule", new Vector3(p.x, y + p.y, z - 0.05f), 0.08f, Bulb);
            }
            Visuals.Text(transform, "CASINO DES SINGES", new Vector3(0f, y + 0.1f, z - 0.06f), 0.7f, Banana, title: true);
            Visuals.Text(transform, "roulette · blackjack · course des singes · fléchettes", new Vector3(0f, y - 0.42f, z - 0.06f), 0.16f, Pink);
            for (int side = -1; side <= 1; side += 2)
            {
                var banana = Visuals.Primitive(PrimitiveType.Capsule);
                banana.name = "Banane";
                Visuals.Kill(banana.GetComponent<Collider>());
                banana.transform.SetParent(decor, false);
                banana.transform.localPosition = new Vector3(side * 4.4f, y, z - 0.1f);
                banana.transform.localRotation = Quaternion.Euler(0f, 0f, side * 35f);
                banana.transform.localScale = new Vector3(0.28f, 0.6f, 0.2f);
                banana.AddComponent<ColorTint>().Set(Banana);
            }
        }

        // Un lustre : une chaîne, deux anneaux dorés, des ampoules, et une vraie lampe (sans ombre)
        void Chandelier(Vector3 pos)
        {
            Visuals.Box("Chaîne", decor, pos + new Vector3(0f, 0.45f, 0f), new Vector3(0.03f, 0.9f, 0.03f), Gold);
            Disc("Anneau", pos, 1.1f, 0.05f, Gold);
            Disc("Anneau", pos + new Vector3(0f, -0.25f, 0f), 0.7f, 0.05f, Gold);
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2f / 10f;
                Ball("Ampoule", pos + new Vector3(Mathf.Cos(a) * 0.55f, 0.06f, Mathf.Sin(a) * 0.55f), 0.09f, Bulb);
            }
            var lamp = new GameObject("Lumière du lustre").AddComponent<Light>();
            lamp.transform.SetParent(transform, false);
            lamp.transform.localPosition = pos + Vector3.down * 0.3f;
            lamp.type = LightType.Point;
            lamp.color = new Color(1f, 0.82f, 0.55f);
            lamp.range = 9f;
            lamp.intensity = 1.6f;
            lamp.shadows = LightShadows.None;
        }

        // Un palmier en pot : un pot en terre, un tronc en trois morceaux un peu penchés, une couronne de 7 palmes
        void Palm(Vector3 pos)
        {
            Disc("Pot", pos + new Vector3(0f, 0.3f, 0f), 0.7f, 0.3f, Pot);
            Disc("Bord du pot", pos + new Vector3(0f, 0.6f, 0f), 0.8f, 0.04f, Gold * 0.8f);
            var top = pos + new Vector3(0f, 0.6f, 0f);
            for (int i = 0; i < 3; i++)
            {
                var piece = Visuals.Box("Tronc", decor, top + new Vector3(0.04f * i, 0.4f, 0f), new Vector3(0.16f - i * 0.02f, 0.8f, 0.16f - i * 0.02f), Trunk);
                piece.transform.localRotation = Quaternion.Euler(0f, 0f, -3f * i);
                top += new Vector3(0.04f, 0.8f, 0f);
            }
            for (int i = 0; i < 7; i++)
            {
                var leaf = Visuals.Box("Palme", decor, top, new Vector3(0.28f, 0.03f, 1.3f), Leaf * (0.85f + 0.05f * (i % 3)));
                leaf.transform.localRotation = Quaternion.Euler(0f, i * 360f / 7f, 0f) * Quaternion.Euler(28f, 0f, 0f);
                leaf.transform.localPosition = top + leaf.transform.localRotation * new Vector3(0f, 0f, 0.6f);
            }
        }

        // Une machine à sous (décor) : une borne rouge, un écran avec trois fenêtres, un levier à boule
        void SlotMachine(Vector3 pos, float yaw)
        {
            var m = new GameObject("Machine à sous").transform;
            m.SetParent(decor, false);
            m.localPosition = pos;
            m.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Visuals.Solid("Borne", m, new Vector3(0f, 0.8f, 0f), new Vector3(0.7f, 1.6f, 0.6f), Slot);
            Visuals.Box("Fronton", m, new Vector3(0f, 1.72f, -0.05f), new Vector3(0.74f, 0.25f, 0.5f), Gold);
            Visuals.Box("Écran", m, new Vector3(0f, 1.2f, -0.31f), new Vector3(0.55f, 0.3f, 0.02f), Screen);
            for (int i = -1; i <= 1; i++)
                Visuals.Box("Rouleau", m, new Vector3(i * 0.17f, 1.2f, -0.325f), new Vector3(0.14f, 0.22f, 0.01f), Cream);
            Visuals.Box("Levier", m, new Vector3(0.4f, 1.25f, 0f), new Vector3(0.04f, 0.4f, 0.04f), Gold);
            Ball("Boule du levier", m.TransformPoint(0.4f, 1.47f, 0f), 0.09f, Slot * 1.2f, world: true);
        }

        // --- Petites formes ---

        void Disc(string name, Vector3 pos, float diameter, float height, Color color)
        {
            var go = Visuals.Primitive(PrimitiveType.Cylinder);
            go.name = name;
            Visuals.Kill(go.GetComponent<Collider>());
            go.transform.SetParent(decor, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(diameter, height / 2f, diameter);   // un cylindre de Unity fait 2 m de haut
            go.AddComponent<ColorTint>().Set(color);
        }

        void Ball(string name, Vector3 pos, float size, Color color, bool world = false)
        {
            var go = Visuals.Primitive(PrimitiveType.Sphere);
            go.name = name;
            Visuals.Kill(go.GetComponent<Collider>());
            go.transform.SetParent(decor, false);
            if (world) go.transform.position = pos;
            else go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one * size;
            go.AddComponent<ColorTint>().Set(color);
        }
    }
}
