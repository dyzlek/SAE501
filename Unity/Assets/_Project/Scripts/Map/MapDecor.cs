using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // L'habillage du terrain de jeu, façon Bloons TD (Monkey Lane) : le chemin de dalles de pierre sur du gravier,
    // et autour des fleurs, des touffes d'herbe, des cailloux, des champignons et des buissons.
    // Sert à la carte (scale = 1) et au plateau du hub (en petit). Le tirage au sort est fixe (même graine) :
    // la carte et le plateau ont exactement le même décor, et il ne change pas d'une génération à l'autre.
    // Rien n'a de collider : on pose ses singes par-dessus les fleurs, et on ne se cogne à rien.
    public static class MapDecor
    {
        static readonly Color Gravel = new Color(0.47f, 0.43f, 0.38f);
        static readonly Color[] StoneColors = { new Color(0.8f, 0.8f, 0.78f), new Color(0.7f, 0.71f, 0.69f), new Color(0.62f, 0.62f, 0.61f) };
        static readonly Color Petal = new Color(0.98f, 0.98f, 0.95f);
        static readonly Color YellowPetal = new Color(1f, 0.87f, 0.25f);
        static readonly Color FlowerHeart = new Color(1f, 0.75f, 0.1f);
        static readonly Color Tuft = new Color(0.36f, 0.62f, 0.2f);
        static readonly Color Rock = new Color(0.58f, 0.57f, 0.53f);
        static readonly Color Bush = new Color(0.22f, 0.55f, 0.16f);      // comme les buissons du paysage
        static readonly Color BushLight = new Color(0.3f, 0.64f, 0.2f);
        static readonly Color MushroomCap = new Color(0.85f, 0.18f, 0.15f);
        static readonly Color MushroomStem = new Color(0.95f, 0.92f, 0.82f);
        static readonly Color ArchStone = new Color(0.72f, 0.7f, 0.66f);
        static readonly Color ArchStoneDark = new Color(0.58f, 0.57f, 0.54f);
        static readonly Color Arrow = new Color(1f, 0.83f, 0.35f);              // doré, comme les titres du hub
        static readonly Color EntryGlow = new Color(0.35f, 0.85f, 1f);          // bleu : les ballons arrivent
        static readonly Color ExitGlow = new Color(1f, 0.3f, 0.2f);             // rouge : danger, ils s'échappent

        const float StoneStep = 1f;       // une rangée de dalles par mètre de chemin
        const float StoneHeight = 0.08f;  // épaisseur des dalles, en mètres

        public static void Build(Transform parent, float scale, bool withBushes)
        {
            var random = new System.Random(501);   // graine fixe : toujours le même décor
            BuildPath(parent, scale, random);
            BuildGrassDecor(parent, scale, random);
            BuildPortals(parent, scale);
            if (withBushes) BuildBushes(parent, scale, random);
        }

        // Le chemin : une bande de gravier, et dessus des dalles un peu de travers, de trois gris.
        static void BuildPath(Transform parent, float scale, System.Random random)
        {
            var points = MapLayout.PathPoints();
            var batch = new MeshBatch();
            batch.Add(Ribbon(points, (MapLayout.PathWidth + 0.4f) * scale, scale), Vector3.up * 0.01f * scale, Quaternion.identity, Vector3.one, Gravel);

            // On avance le long du chemin et on pose une rangée tous les StoneStep mètres
            float next = 0f, walked = 0f;
            for (int i = 0; i < points.Count - 1; i++)
            {
                var a = points[i];
                var b = points[i + 1];
                float length = Vector3.Distance(a, b);
                while (next <= walked + length)
                {
                    var center = Vector3.Lerp(a, b, (next - walked) / length);
                    var direction = Quaternion.LookRotation(b - a);
                    StoneRow(batch, center, direction, scale, random);
                    next += StoneStep;
                }
                walked += length;
            }
            batch.Build(parent, "Chemin de dalles");
        }

        // Les portails : une arche de pierre au début et à la fin du chemin, avec un voile coloré
        // (bleu à l'entrée, d'où sortent les ballons ; rouge à la sortie). Des flèches dorées sur les dalles donnent le sens.
        static void BuildPortals(Transform parent, float scale)
        {
            var points = MapLayout.PathPoints();
            var batch = new MeshBatch();
            Portal(batch, parent, points[0], points[1] - points[0], 0.4f, EntryGlow, scale, "Portail d'entrée");
            int last = points.Count - 1;
            Portal(batch, parent, points[last], points[last] - points[last - 1], -0.4f, ExitGlow, scale, "Portail de sortie");

            // Trois flèches juste après l'entrée et trois juste avant la sortie
            for (int k = 0; k < 3; k++)
            {
                ArrowAt(batch, points, 2.5f + k * 1.6f, scale);
                ArrowAt(batch, points, PathLength(points) - 2.5f - k * 1.6f, scale);
            }
            batch.Build(parent, "Portails");
        }

        // Une arche de pierre : deux piliers en blocs empilés (deux tons de gris), un arc de 11 blocs avec une clé de voûte
        // de la couleur du portail. Dedans, un voile lumineux et translucide (on voit la carte au travers) et un tourbillon
        // qui tourne : c'est de là que sortent les ballons (entrée) ou là qu'ils s'échappent (sortie).
        // end = le bout du chemin (au bord de la carte), forward = le sens de marche des ballons,
        // inward = de combien avancer l'arche dans la carte (en mètres, dans le sens de marche ; négatif à la sortie).
        static void Portal(MeshBatch batch, Transform parent, Vector3 end, Vector3 forward, float inward, Color glow, float scale, string name)
        {
            var rot = Quaternion.LookRotation(forward.normalized);
            var origin = end + rot * Vector3.forward * inward;
            float half = MapLayout.PathWidth / 2f + 0.4f;   // les piliers de chaque côté des dalles
            const float PillarHeight = 2.2f, Block = 0.55f;
            Vector3 At(Vector3 local) => (origin + rot * local) * scale;

            for (int side = -1; side <= 1; side += 2)
            {
                batch.Add(MeshBatch.Cube, At(new Vector3(side * half, 0.12f, 0f)), rot, new Vector3(0.85f, 0.24f, 0.85f) * scale, ArchStoneDark);   // le socle
                for (int k = 0; k < 4; k++)   // le pilier : 4 blocs, un sur deux plus foncé
                {
                    float y = 0.24f + PillarHeight / 4f * (k + 0.5f) - 0.06f;
                    batch.Add(MeshBatch.Cube, At(new Vector3(side * half, y, 0f)), rot, new Vector3(Block, PillarHeight / 4f - 0.04f, Block) * scale, k % 2 == 0 ? ArchStone : ArchStoneDark);
                }
                batch.Add(MeshBatch.Cube, At(new Vector3(side * half, PillarHeight + 0.12f, 0f)), rot, new Vector3(0.75f, 0.14f, 0.75f) * scale, ArchStoneDark);   // le chapiteau
            }
            float archY = PillarHeight + 0.2f, archRise = half * 0.85f;
            for (int i = 0; i <= 10; i++)
            {
                float a = Mathf.PI * i / 10f;   // de la droite (0) à la gauche (pi)
                var local = new Vector3(Mathf.Cos(a) * half, archY + Mathf.Sin(a) * archRise, 0f);
                var tangent = new Vector3(-Mathf.Sin(a) * half, Mathf.Cos(a) * archRise, 0f);   // le bloc suit la courbe de l'arc
                var block = rot * Quaternion.Euler(0f, 0f, Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg + 90f);
                var color = i == 5 ? glow : (i % 2 == 0 ? ArchStone : ArchStoneDark);
                float size = i == 5 ? 0.7f : 0.6f;   // la clé de voûte dépasse un peu
                batch.Add(MeshBatch.Cube, At(local), block, new Vector3(0.42f, size, size) * scale, color);
            }

            // Le voile et le tourbillon bougent : ils sont à part, avec un matériau lumineux et translucide
            var center = At(new Vector3(0f, (archY + archRise) * 0.52f, 0f));
            float width = half * 2f - Block, height = archY + archRise - 0.35f;
            var veil = Visuals.Box(name, parent, center, Vector3.one, new Color(glow.r, glow.g, glow.b, 0.35f));
            veil.GetComponent<MeshFilter>().sharedMesh = MeshBatch.Ball;
            veil.GetComponent<Renderer>().sharedMaterial = GlowMaterial;
            veil.transform.localRotation = rot;
            veil.transform.localScale = new Vector3(width, height, 0.04f) * scale;

            var swirl = new GameObject("Tourbillon").transform;
            swirl.SetParent(parent, false);
            swirl.localPosition = center;
            swirl.localRotation = rot;
            var spiral = new MeshBatch();
            for (int arm = 0; arm < 3; arm++)
                for (int k = 0; k < 9; k++)
                {
                    float angle = (arm * 120f + k * 32f) * Mathf.Deg2Rad;
                    float r = 0.12f + k * 0.1f;
                    float ball = 0.09f + k * 0.018f;
                    for (int face = -1; face <= 1; face += 2)   // des deux côtés du voile
                        spiral.Add(MeshBatch.Ball, new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r * height / width, face * 0.04f) * scale,
                                   Quaternion.identity, new Vector3(ball, ball, 0.03f) * scale, Color.Lerp(glow, Color.white, 0.55f));
                }
            spiral.Build(swirl, "Tourbillon");
            foreach (var r in swirl.GetComponentsInChildren<Renderer>()) r.sharedMaterial = GlowMaterial;

            var glowFx = veil.AddComponent<PortalGlow>();
            glowFx.swirl = swirl;
            glowFx.spin = inward > 0f ? -90f : 90f;   // l'entrée tourne dans un sens, la sortie dans l'autre
        }

        // Matériau lumineux (pas d'ombre, pas de lumière à calculer) et translucide : le shader des textes 3D, teinté par ColorTint.
        static Material glowMaterial;
        static Material GlowMaterial
        {
            get
            {
                if (glowMaterial) return glowMaterial;
                glowMaterial = new Material(Shader.Find("SAE/Texte 3D")) { name = "Portail lumineux" };
                glowMaterial.SetFloat("_VertexColor", 0f);   // la couleur vient de ColorTint
                return glowMaterial;
            }
        }

        // Une flèche dorée (un chevron) posée sur les dalles, à distance mètres du début du chemin, pointée dans le sens de marche.
        static void ArrowAt(MeshBatch batch, List<Vector3> points, float distance, float scale)
        {
            for (int i = 0; i < points.Count - 1; i++)
            {
                float length = Vector3.Distance(points[i], points[i + 1]);
                if (distance > length) { distance -= length; continue; }
                var rot = Quaternion.LookRotation(points[i + 1] - points[i]);
                var center = Vector3.Lerp(points[i], points[i + 1], distance / length) + Vector3.up * (StoneHeight * 1.25f);
                for (int side = -1; side <= 1; side += 2)   // les deux branches du chevron, en V vers l'avant
                {
                    var arm = rot * Quaternion.Euler(0f, -side * 45f, 0f);
                    var pos = center + rot * new Vector3(side * 0.28f, 0f, -0.28f);
                    batch.Add(MeshBatch.Cube, pos * scale, arm, new Vector3(0.18f, 0.03f, 0.85f) * scale, Arrow);
                }
                return;
            }
        }

        static float PathLength(List<Vector3> points)
        {
            float total = 0f;
            for (int i = 0; i < points.Count - 1; i++) total += Vector3.Distance(points[i], points[i + 1]);
            return total;
        }

        // Une rangée : une grande dalle sur toute la largeur, ou deux plus petites côte à côte.
        static void StoneRow(MeshBatch batch, Vector3 center, Quaternion direction, float scale, System.Random random)
        {
            float width = MapLayout.PathWidth - 0.25f;
            bool single = random.NextDouble() < 0.3;
            int count = single ? 1 : 2;
            for (int s = 0; s < count; s++)
            {
                float across = single ? 0f : (s == 0 ? -1f : 1f) * width / 4f;
                var size = new Vector3(width / count - 0.12f, StoneHeight * Range(random, 0.8f, 1.2f), StoneStep - 0.12f)
                           * Range(random, 0.92f, 1f);
                var pos = center + direction * new Vector3(across, size.y / 2f, Range(random, -0.05f, 0.05f));
                var rot = direction * Quaternion.Euler(0f, Range(random, -6f, 6f), 0f);
                var color = StoneColors[random.Next(StoneColors.Length)];
                batch.Add(MeshBatch.Cube, pos * scale, rot, size * scale, color);
            }
        }

        // Une bande plate qui suit le chemin, de la largeur donnée (déjà à l'échelle).
        static Mesh Ribbon(List<Vector3> points, float width, float scale)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int i = 0; i < points.Count; i++)
            {
                var forward = points[Mathf.Min(i + 1, points.Count - 1)] - points[Mathf.Max(i - 1, 0)];
                var side = Vector3.Cross(Vector3.up, forward.normalized) * width / 2f;
                vertices.Add(points[i] * scale - side);
                vertices.Add(points[i] * scale + side);
                if (i == 0) continue;
                int a = vertices.Count - 4;
                triangles.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
            }
            var mesh = new Mesh { name = "Gravier" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            return mesh;
        }

        // Sur l'herbe, loin du chemin : bouquets de fleurs, touffes d'herbe, cailloux et quelques champignons.
        static void BuildGrassDecor(Transform parent, float scale, System.Random random)
        {
            var batch = new MeshBatch();
            for (int i = 0; i < 45; i++)
                if (FreeSpot(random, 0.5f, out var p)) Flowers(batch, p, scale, random);
            for (int i = 0; i < 40; i++)
                if (FreeSpot(random, 0.3f, out var p)) GrassTuft(batch, p, scale, random);
            for (int i = 0; i < 12; i++)
                if (FreeSpot(random, 0.6f, out var p)) Pebbles(batch, p, scale, random);
            for (int i = 0; i < 6; i++)
                if (FreeSpot(random, 0.5f, out var p)) Mushroom(batch, p, scale, random);
            batch.Build(parent, "Décor du terrain");
        }

        // Un point au hasard dans la carte, à au moins margin mètres du bord du chemin.
        static bool FreeSpot(System.Random random, float margin, out Vector3 point)
        {
            float half = MapLayout.HalfExtent - 0.4f;
            var p = new Vector2(Range(random, -half, half), Range(random, -half, half));
            point = new Vector3(p.x, 0f, p.y);
            return MapLayout.DistanceToPath(p) > MapLayout.PathWidth / 2f + margin;
        }

        // 3 à 6 petites fleurs : cinq pétales autour d'un cœur jaune.
        static void Flowers(MeshBatch batch, Vector3 at, float scale, System.Random random)
        {
            var petal = random.NextDouble() < 0.7 ? Petal : YellowPetal;
            int count = random.Next(3, 7);
            for (int f = 0; f < count; f++)
            {
                var center = at + new Vector3(Range(random, -0.35f, 0.35f), 0.06f, Range(random, -0.35f, 0.35f));
                float turn = Range(random, 0f, 72f);
                for (int k = 0; k < 5; k++)
                {
                    var dir = Quaternion.Euler(0f, turn + k * 72f, 0f);
                    batch.Add(MeshBatch.Ball, (center + dir * Vector3.forward * 0.045f) * scale, dir, new Vector3(0.05f, 0.02f, 0.08f) * scale, petal);
                }
                batch.Add(MeshBatch.Ball, (center + Vector3.up * 0.01f) * scale, Quaternion.identity, new Vector3(0.05f, 0.03f, 0.05f) * scale, FlowerHeart);
            }
        }

        // Quelques brins d'herbe plus hauts, penchés dans tous les sens.
        static void GrassTuft(MeshBatch batch, Vector3 at, float scale, System.Random random)
        {
            for (int k = 0; k < 5; k++)
            {
                float height = Range(random, 0.18f, 0.32f);
                var rot = Quaternion.Euler(Range(random, -25f, 25f), Range(random, 0f, 360f), Range(random, -25f, 25f));
                var pos = at + new Vector3(Range(random, -0.08f, 0.08f), 0f, Range(random, -0.08f, 0.08f)) + rot * Vector3.up * height / 2f;
                batch.Add(MeshBatch.Cube, pos * scale, rot, new Vector3(0.025f, height, 0.012f) * scale, Tuft);
            }
        }

        // Un gros caillou et un ou deux petits à côté.
        static void Pebbles(MeshBatch batch, Vector3 at, float scale, System.Random random)
        {
            int count = random.Next(1, 4);
            for (int k = 0; k < count; k++)
            {
                float size = k == 0 ? Range(random, 0.35f, 0.6f) : Range(random, 0.12f, 0.25f);
                var pos = at + (k == 0 ? Vector3.zero : new Vector3(Range(random, -0.45f, 0.45f), 0f, Range(random, -0.45f, 0.45f)));
                var rot = Quaternion.Euler(Range(random, -10f, 10f), Range(random, 0f, 360f), Range(random, -10f, 10f));
                batch.Add(MeshBatch.Ball, (pos + Vector3.up * size * 0.2f) * scale, rot, new Vector3(size * 1.3f, size * 0.6f, size) * scale, Rock);
            }
        }

        // Un champignon rouge (comme ceux de la cabane) : un pied blanc et un chapeau aplati.
        static void Mushroom(MeshBatch batch, Vector3 at, float scale, System.Random random)
        {
            float h = Range(random, 0.15f, 0.25f);
            batch.Add(MeshBatch.Cube, (at + Vector3.up * h / 2f) * scale, Quaternion.Euler(0f, Range(random, 0f, 90f), 0f), new Vector3(0.06f, h, 0.06f) * scale, MushroomStem);
            batch.Add(MeshBatch.Ball, (at + Vector3.up * h) * scale, Quaternion.identity, new Vector3(0.22f, 0.12f, 0.22f) * h / 0.2f * scale, MushroomCap);
        }

        // Des buissons tout autour, juste au bord du terrain (pas sur l'entrée, la sortie ni l'estrade du joueur).
        static void BuildBushes(Transform parent, float scale, System.Random random)
        {
            var batch = new MeshBatch();
            float edge = MapLayout.HalfExtent;
            for (int i = 0; i < 36; i++)
            {
                // un point sur le pourtour, 0,8 à 2,5 m hors du terrain
                int side = random.Next(4);
                float along = Range(random, -edge - 2f, edge + 2f);
                float outside = edge + Range(random, 0.8f, 2.5f);
                var p = side switch
                {
                    0 => new Vector2(along, outside),
                    1 => new Vector2(along, -outside),
                    2 => new Vector2(outside, along),
                    _ => new Vector2(-outside, along),
                };
                if (p.y < 0f && Mathf.Abs(p.x) < 5f) continue;          // l'estrade et la sortie
                if (MapLayout.DistanceToPath(Clamp(p, edge)) < 2.5f) continue;   // l'entrée du chemin
                float size = Range(random, 0.8f, 1.5f);
                for (int k = 0; k < 3; k++)
                {
                    var offset = new Vector3(Range(random, -0.4f, 0.4f), 0f, Range(random, -0.4f, 0.4f)) * size;
                    float s = size * (k == 0 ? 1f : 0.7f);
                    var rot = Quaternion.Euler(0f, Range(random, 0f, 360f), 0f);
                    batch.Add(MeshBatch.Ball, (new Vector3(p.x, s * 0.35f, p.y) + offset) * scale, rot, new Vector3(s, s * 0.8f, s) * scale, k == 0 ? Bush : BushLight);
                }
            }
            batch.Build(parent, "Buissons");
        }

        static Vector2 Clamp(Vector2 p, float edge) => new Vector2(Mathf.Clamp(p.x, -edge, edge), Mathf.Clamp(p.y, -edge, edge));

        static float Range(System.Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);
    }
}
