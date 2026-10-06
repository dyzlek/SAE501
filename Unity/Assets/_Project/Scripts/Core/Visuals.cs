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

        // Un singe = son modèle 3D (s'il existe, sinon un cube couleur de sa rareté), son aura de la couleur
        // de la rareté, et son type écrit au-dessus. Tout tient dans un cube de côté size, centré sur localPos.
        // Le modèle regarde vers -Z local, comme la face du cube tournée vers le joueur (voir MonkeyVisuals.yaw).
        public static GameObject MonkeyPiece(Monkey m, Transform parent, Vector3 localPos, float size, bool withLabel = true)
        {
            var root = new GameObject($"Singe {m}");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            var view = root.AddComponent<MonkeyView>();
            view.body = Box("Corps", root.transform, Vector3.zero, Vector3.one * size, MonkeyData.RarityColor(m.level)).GetComponent<ColorTint>();
            view.body.Set(MonkeyData.RarityColor(m.level), MonkeyData.IsRainbow(m.level));

            var modelAsset = MonkeyVisuals.Model(m.type, out float yaw);
            if (modelAsset)
            {
                view.body.GetComponent<Renderer>().enabled = false;   // le cube reste pour la miniature du plateau
                view.model = FitModel(modelAsset, yaw, root.transform, size);
            }
            view.aura = Aura.Add(root, m.level, size);
            if (withLabel)
                Label(root.transform, $"{MonkeyData.ShortName(m.type)}{(int)m.level + 1}", new Vector3(0, size * 0.9f, 0), size * 0.45f, Color.black);
            return root;
        }

        // Pose une copie du modèle dans parent, à la hauteur size et centrée. Les bras écartés (pose en T)
        // peuvent un peu déborder sur les côtés : on les compte pour 80 %, sinon les singes seraient minuscules.
        static GameObject FitModel(GameObject asset, float yaw, Transform parent, float size)
        {
            var model = Object.Instantiate(asset);
            model.name = "Modele";
            var rotation = Quaternion.Euler(0f, yaw, 0f) * asset.transform.rotation;
            model.transform.SetPositionAndRotation(Vector3.zero, rotation);

            // Boîte englobante du modèle à l'échelle 1, posé à l'origine
            var renderers = model.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            float scale = size / Mathf.Max(bounds.size.y, bounds.size.x * 0.8f, bounds.size.z * 0.8f);

            model.transform.SetParent(parent, false);
            model.transform.localScale = asset.transform.localScale * scale;
            model.transform.localRotation = rotation;
            model.transform.localPosition = -bounds.center * scale;
            return model;
        }

    }
}
