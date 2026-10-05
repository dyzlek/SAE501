using UnityEngine;

namespace SAE
{
    public static class Visuals
    {
        public static void Kill(Object o)
        {
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }

        // Cube coloré sans collider (le collider est porté par le parent cliquable).
        public static GameObject Box(string name, Transform parent, Vector3 localPos, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Kill(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            go.AddComponent<ColorTint>().Set(color);
            return go;
        }

        // Cube avec collider, pour le sol, les murs, les meubles.
        public static GameObject Solid(string name, Transform parent, Vector3 pos, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.AddComponent<ColorTint>().Set(color);
            return go;
        }

        public static TextMesh Label(Transform parent, string text, Vector3 localPos, float height, Color? color = null)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            tm.text = text;
            tm.fontSize = 48;
            tm.characterSize = height / 4.8f; // ~ hauteur d'une ligne en mètres
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color ?? Color.white;
            go.AddComponent<Billboard>();
            return tm;
        }

        // Un singe = un cube couleur de sa rareté + son type écrit dessus.
        public static GameObject MonkeyPiece(Monkey m, Transform parent, Vector3 localPos, float size)
        {
            var root = new GameObject($"Singe {m}");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            var cube = Box("Corps", root.transform, Vector3.zero, Vector3.one * size, MonkeyData.RarityColor(m.level));
            cube.GetComponent<ColorTint>().Set(MonkeyData.RarityColor(m.level), MonkeyData.IsRainbow(m.level));
            Label(root.transform, $"{MonkeyData.ShortName(m.type)}{(int)m.level + 1}", new Vector3(0, size * 0.9f, 0), size * 0.45f, Color.black);
            return root;
        }
    }
}
