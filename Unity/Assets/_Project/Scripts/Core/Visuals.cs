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

        // Les deux polices des textes 3D (publiques : la roulette du coffre s'en sert aussi) (Art/Resources/Fonts, licence libre OFL) : Oswald pour lire (panneaux, prix),
        // Bangers pour les titres (style dessin animé, comme Bloons). Sans elles, on retombe sur la police de Unity.
        // Le matériau utilise notre shader « SAE/Texte 3D » (Art/Resources) : celui de Unity ne s'affiche pas
        // correctement dans le casque (un seul œil, à travers les murs). Un matériau par police (chacune a sa texture).
        static Font bodyFont, titleFont;
        static readonly System.Collections.Generic.Dictionary<Font, Material> textMaterials = new System.Collections.Generic.Dictionary<Font, Material>();

        public static Font BodyFont => bodyFont ? bodyFont : bodyFont = LoadFont("Fonts/Oswald-Bold");
        public static Font TitleFont => titleFont ? titleFont : titleFont = LoadFont("Fonts/Bangers");

        static Font LoadFont(string path)
        {
            var f = Resources.Load<Font>(path);
            return f ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        static Material TextMaterial(Font f)
        {
            if (!textMaterials.TryGetValue(f, out var m) || !m)
            {
                m = new Material(Shader.Find("SAE/Texte 3D")) { name = "Texte 3D " + f.name };
                textMaterials[f] = m;
                // Quand Unity agrandit la texture de la police (nouvelles lettres), on la redonne au matériau
                Font.textureRebuilt += rebuilt => { if (m && rebuilt == f) m.mainTexture = f.material.mainTexture; };
            }
            m.mainTexture = f.material.mainTexture;
            return m;
        }

        // Matériau des lignes (rayons, cercles, tirs) : le même shader, sans texture, la couleur vient de la ligne.
        static Material lineMaterial;
        public static Material LineMaterial => lineMaterial ? lineMaterial : lineMaterial = new Material(Shader.Find("SAE/Texte 3D")) { name = "Lignes" };

        // Au lancement du jeu, les textes déjà posés dans la scène prennent le matériau du jeu (créé à ce moment-là).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void FixSceneTexts()
        {
            foreach (var tm in Object.FindObjectsByType<TextMesh>())
                if (tm.font == BodyFont || tm.font == TitleFont) tm.GetComponent<MeshRenderer>().sharedMaterial = TextMaterial(tm.font);
        }

        // Un texte 3D qui se tourne vers le joueur (Billboard). title = police des titres.
        public static TextMesh Label(Transform parent, string text, Vector3 localPos, float height, Color? color = null, bool title = false)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            tm.font = title ? TitleFont : BodyFont;
            go.GetComponent<MeshRenderer>().sharedMaterial = TextMaterial(tm.font);
            tm.text = text;
            tm.fontSize = 48;
            tm.characterSize = height / 4.8f; // ~ hauteur d'une ligne en mètres
            tm.lineSpacing = 0.9f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color ?? Color.white;
            go.AddComponent<Billboard>();
            return tm;
        }

        // Un texte fixe, écrit sur un panneau : il ne se tourne pas vers le joueur, il suit le panneau.
        // Lisible du côté -Z local du parent (le côté du joueur pour nos meubles, tournés « +Z vers le mur »).
        public static TextMesh Text(Transform parent, string text, Vector3 localPos, float height, Color color, bool title = false)
        {
            var tm = Label(parent, text, localPos, height, color, title);
            Kill(tm.GetComponent<Billboard>());
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
            view.monkey = m;
            view.body = Box("Corps", root.transform, Vector3.zero, Vector3.one * size, MonkeyData.RarityColor(m.level)).GetComponent<ColorTint>();
            view.body.Set(MonkeyData.RarityColor(m.level), MonkeyData.IsRainbow(m.level));

            var bodySize = Vector3.one * size;   // taille de ce qu'on voit, pour ajuster l'aura
            var modelAsset = MonkeyVisuals.Model(m.type, out float yaw);
            if (modelAsset)
            {
                view.body.GetComponent<Renderer>().enabled = false;   // le cube reste comme collider et repère
                view.model = FitModel(modelAsset, yaw, root.transform, size, out bodySize);
                RestPose(view.model.transform, root.transform);
            }
            // L'aura n'est créée qu'en jeu : les particules ne bougent pas hors Play, et enregistrées dans la scène
            // pour chaque case elles l'alourdiraient beaucoup. Les cases de la bibliothèque la créent au lancement.
            if (Application.isPlaying) view.aura = Aura.Add(root, m.level, bodySize);
            if (withLabel)
                Label(root.transform, $"{MonkeyData.ShortName(m.type)}{(int)m.level + 1}", new Vector3(0, size * 0.9f, 0), size * 0.45f, Color.black);
            return root;
        }

        // Les modèles sont exportés en pose en T (bras écartés à l'horizontale) : on baisse les bras le long du corps,
        // un peu écartés et un peu en avant, pour une pose naturelle au hub (bibliothèque, plateau, fiche, coffre).
        // Les singes animés (carte, récolteurs) partent de cette pose. Le Canon et le Tireur n'ont pas de bras : rien ne change.
        static void RestPose(Transform model, Transform root)
        {
            foreach (var (armName, foreArmName) in new[] { ("LeftArm", "LeftForeArm"), ("RightArm", "RightForeArm") })
            {
                Transform arm = null, foreArm = null;
                foreach (var t in model.GetComponentsInChildren<Transform>())
                {
                    if (t.name.EndsWith(armName)) arm = t;
                    if (t.name.EndsWith(foreArmName)) foreArm = t;
                }
                if (!arm || !foreArm) continue;
                var current = foreArm.position - arm.position;
                if (current.sqrMagnitude < 1e-8f) continue;
                // Le singe regarde vers -Z local : le côté de ce bras se lit sur l'axe X de la racine
                float side = Mathf.Sign(Vector3.Dot(arm.position - root.position, root.right));
                var down = (-root.up + root.right * side * 0.3f - root.forward * 0.12f).normalized;
                arm.rotation = Quaternion.FromToRotation(current, down) * arm.rotation;
            }
        }

        // Les bras écartés (pose en T) peuvent un peu déborder sur les côtés : on ne compte la largeur
        // qu'à 80 % pour mettre le modèle à l'échelle, sinon les singes seraient minuscules.
        const float WidthWeight = 0.8f;

        // Pose une copie du modèle dans parent, à la hauteur size et centrée.
        // fittedSize = taille finale du modèle (largeur, hauteur, profondeur) en mètres.
        public static GameObject FitModel(GameObject asset, float yaw, Transform parent, float size, out Vector3 fittedSize)
        {
#if UNITY_EDITOR
            // Hors Play (menu qui met les singes dans la scène) : un lien vers le FBX plutôt qu'une copie complète,
            // sinon chaque case enregistre tout le modèle et la scène passe de 1,5 à 9 Mo.
            var model = Application.isPlaying ? Object.Instantiate(asset) : (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(asset);
#else
            var model = Object.Instantiate(asset);
#endif
            model.name = "Modele";
            var rotation = Quaternion.Euler(0f, yaw, 0f) * asset.transform.rotation;
            model.transform.SetPositionAndRotation(Vector3.zero, rotation);

            // Boîte englobante du modèle à l'échelle 1, posé à l'origine
            var renderers = model.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            float scale = size / Mathf.Max(bounds.size.y, bounds.size.x * WidthWeight, bounds.size.z * WidthWeight);

            model.transform.SetParent(parent, false);
            model.transform.localScale = asset.transform.localScale * scale;
            model.transform.localRotation = rotation;
            model.transform.localPosition = -bounds.center * scale;
            fittedSize = bounds.size * scale;
            return model;
        }

    }
}
