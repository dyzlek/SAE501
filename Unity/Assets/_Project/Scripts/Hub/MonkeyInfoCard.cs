using UnityEngine;
using UnityEngine.InputSystem;

namespace SAE
{
    // Fiche d'un singe : quand on vise un singe avec la manette droite (bibliothèque, plateau ou carte)
    // EN MAINTENANT son bouton A (mode PC : viser au centre de l'écran + touche A), une petite carte apparaît
    // DANS LE DÉCOR, au-dessus de lui, avec ses caractéristiques et ce que donnerait une fusion.
    // Sur le plateau et la carte, un cercle montre aussi sa portée.
    // Pas d'affichage collé à l'écran (règle de confort VR) : la carte reste posée près du singe.
    // Elle se dessine PAR-DESSUS le décor (shader avec ZTest Always) : sinon une étagère ou un meuble la cachait.
    // Même style que les ardoises du hub : cadre en bois, fond ardoise, titre doré, texte à la craie.
    // À gauche, une vitrine montre le singe en 3D avec son aura (il tourne doucement) ;
    // à droite, son nom, sa rareté, ce qu'il fait, puis une ligne par caractéristique avec une barre.
    public class MonkeyInfoCard : MonoBehaviour
    {
        public float textHeight = 0.04f;        // hauteur d'une ligne, en mètres, vue de près
        public float readableDistance = 1.2f;   // au-delà, la carte grandit pour rester lisible
        public float reach = 15f;               // portée de la visée, en mètres

        const float CardWidth = 1.1f, CardHeight = 0.5f;    // en mètres, vue de près
        const float ShowcaseX = -0.36f, ShowcaseSize = 0.3f; // la vitrine du singe, à gauche
        const float ColumnX = -0.15f;                        // début de la colonne de droite
        const float BarX = 0.12f, BarWidth = 0.24f, BarHeight = 0.022f;
        const float RowsTop = 0.03f, RowStep = 0.055f;       // les 4 lignes de caractéristiques
        const int OnTopQueue = 4000;                         // après tout le reste (file « Overlay ») : la fiche passe devant
        static readonly Color FrameColor = new Color(0.45f, 0.3f, 0.18f);
        static readonly Color BackColor = new Color(0.1f, 0.13f, 0.12f, 0.95f);
        static readonly Color TitleColor = new Color(1f, 0.83f, 0.35f);
        static readonly Color ChalkColor = new Color(0.95f, 0.94f, 0.88f);
        static readonly Color DimChalk = new Color(0.72f, 0.76f, 0.7f);
        static readonly Color ShowcaseColor = new Color(0.06f, 0.08f, 0.07f, 0.95f);
        static readonly Color BarBackColor = new Color(0.22f, 0.27f, 0.24f);
        static readonly Color FusionColor = new Color(0.49f, 0.99f, 0.49f);
        static readonly string[] StatNames = { "Perce", "Portée", "Cadence", "Cibles" };

        // Le bouton d'infos : A sur la manette droite.
        InputAction showInfo;
        Component aimedSurface;                 // ce qu'on vise : sert à incliner le cercle de portée comme le plateau

        Transform card;
        TextMesh title, rarity, effect, fusion;
        readonly TextMesh[] statValues = new TextMesh[StatNames.Length];
        readonly Transform[] statBars = new Transform[StatNames.Length];
        Transform showcase;        // la vitrine : le singe 3D y est recréé quand le singe visé change
        GameObject preview;
        int queue = OnTopQueue;    // ordre de dessin : chaque nouveau rectangle passe devant le précédent
        LineRenderer rangeCircle;
        Monkey? shown;
        Vector3 smoothAnchor;

        void OnEnable()
        {
            showInfo = new InputAction("Infos du singe", InputActionType.Button);
            showInfo.AddBinding("<XRController>{RightHand}/primaryButton");
            showInfo.AddBinding("<Keyboard>/q");   // mode PC : touche A en AZERTY (le clavier est lu par position de touche)
            showInfo.Enable();
        }

        void OnDisable() => showInfo?.Disable();

        void Start()
        {
            card = new GameObject("Fiche du singe").transform;
            card.gameObject.AddComponent<Billboard>();   // c'est la carte entière qui fait face au joueur
            // Dessinés dans l'ordre (cadre, fond, vitrine, barres, puis singe et textes), par-dessus tout le décor
            Panel("Cadre", new Vector3(0, 0, 0.004f), new Vector2(CardWidth + 0.04f, CardHeight + 0.04f), FrameColor);
            Panel("Fond", new Vector3(0, 0, 0.002f), new Vector2(CardWidth, CardHeight), BackColor);
            Panel("Bord de la vitrine", new Vector3(ShowcaseX, 0, 0.001f), new Vector2(ShowcaseSize + 0.02f, CardHeight - 0.04f), FrameColor);
            Panel("Vitrine", new Vector3(ShowcaseX, 0, 0f), new Vector2(ShowcaseSize, CardHeight - 0.06f), ShowcaseColor);
            showcase = new GameObject("Singe de la vitrine").transform;
            showcase.SetParent(card, false);
            showcase.localPosition = new Vector3(ShowcaseX, 0.02f, -ShowcaseSize * 0.5f);   // devant la vitrine, côté joueur

            title = Line(new Vector3(ColumnX, 0.19f, 0), textHeight * 1.6f, TitleColor, title: true);
            rarity = Line(new Vector3(ColumnX, 0.135f, 0), textHeight, ChalkColor);
            effect = Line(new Vector3(ColumnX, 0.09f, 0), textHeight * 0.85f, DimChalk);
            for (int i = 0; i < StatNames.Length; i++)
            {
                float y = RowsTop - i * RowStep;
                Line(new Vector3(ColumnX, y, 0), textHeight, DimChalk).text = StatNames[i];
                Panel("Barre vide", new Vector3(BarX, y, -0.001f), new Vector2(BarWidth, BarHeight), BarBackColor);
                statBars[i] = Panel("Barre", new Vector3(BarX, y, -0.002f), new Vector2(BarWidth, BarHeight), TitleColor);
                statValues[i] = Line(new Vector3(BarX + BarWidth / 2f + 0.02f, y, 0), textHeight, ChalkColor);
            }
            fusion = Line(new Vector3(ColumnX, -0.2f, 0), textHeight * 0.85f, FusionColor);
            card.gameObject.SetActive(false);

            rangeCircle = new GameObject("Cercle de portée").AddComponent<LineRenderer>();
            rangeCircle.sharedMaterial = Visuals.LineMaterial;
            rangeCircle.loop = true;
            rangeCircle.positionCount = 48;
            rangeCircle.startColor = rangeCircle.endColor = new Color(1f, 1f, 1f, 0.8f);
            rangeCircle.gameObject.SetActive(false);
        }

        // Un rectangle plat de la fiche (cadre, fond, barre), dessiné par-dessus le décor.
        Transform Panel(string name, Vector3 position, Vector2 size, Color color)
        {
            var box = Visuals.Box(name, card, position, new Vector3(size.x, size.y, 0.002f), color);
            DrawOnTop(box.GetComponent<Renderer>(), null, queue++);
            return box.transform;
        }

        // Une ligne de texte de la colonne de droite, alignée à gauche, dessinée après tous les rectangles.
        TextMesh Line(Vector3 position, float height, Color color, bool title = false)
        {
            var tm = Visuals.Text(card, "", position, height, color, title);
            tm.anchor = TextAnchor.MiddleLeft;
            tm.alignment = TextAlignment.Left;
            DrawOnTop(tm.GetComponent<Renderer>(), tm.GetComponent<Renderer>().sharedMaterial, OnTopQueue + 50);
            return tm;
        }

        // Le singe de la vitrine (modèle + aura) : ses matériaux sont copiés et passent après la fiche,
        // sinon le fond, dessiné par-dessus tout, le cacherait. Il garde le test de profondeur normal pour rester en 3D.
        void ShowPreview(Monkey m)
        {
            DestroyPreview();
            preview = Visuals.MonkeyPiece(m, showcase, Vector3.zero, ShowcaseSize * 0.75f, withLabel: false);
            foreach (var r in preview.GetComponentsInChildren<Renderer>(true))
                foreach (var mat in r.materials) mat.renderQueue = OnTopQueue + 40;
        }

        void DestroyPreview()
        {
            if (!preview) return;
            foreach (var r in preview.GetComponentsInChildren<Renderer>(true))
                foreach (var mat in r.sharedMaterials) Destroy(mat);   // les copies faites par ShowPreview
            Destroy(preview);
            preview = null;
        }

        // Donne au rendu un matériau à lui (copie de celui du texte, ou neuf pour le cadre et le fond)
        // qui ignore la profondeur : il se dessine même si un meuble est devant.
        static void DrawOnTop(Renderer r, Material textMaterial, int queue)
        {
            var m = textMaterial ? new Material(textMaterial) : new Material(Shader.Find("SAE/Texte 3D"));
            if (!textMaterial) m.SetFloat("_VertexColor", 0f);   // un cube n'a pas de couleur de sommets : la teinte vient de ColorTint
            m.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
            m.renderQueue = queue;
            r.sharedMaterial = m;
        }

        // Unity peut agrandir la texture d'une police quand de nouvelles lettres apparaissent : on la redonne à nos copies
        static void RefreshFont(TextMesh tm) => tm.GetComponent<Renderer>().sharedMaterial.mainTexture = tm.font.material.mainTexture;

        void LateUpdate()
        {
            // On vise avec la manette droite (là où elle pointe), seulement quand le bouton est maintenu
            var hand = PlayerRig.Local ? PlayerRig.Local.rightHand : null;
            Monkey monkey = default;
            Vector3 anchor = default, rangeCenter = default;
            float rangeScale = 0f;
            bool visible = false;
            if (showInfo.IsPressed() && hand
                && Physics.Raycast(hand.position, hand.forward, out var hit, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                var info = hit.collider.GetComponentInParent<IMonkeyInfo>();
                aimedSurface = info as Component;
                visible = info != null && info.TryGetMonkeyInfo(hit.point, out monkey, out anchor, out rangeCenter, out rangeScale);
            }

            card.gameObject.SetActive(visible);
            rangeCircle.gameObject.SetActive(visible && rangeScale > 0f && MonkeyData.Range(monkey) < 30f);   // pas de cercle géant pour le Sniper
            if (!visible) { DestroyPreview(); shown = null; return; }

            // Texte (refait seulement quand le singe visé change)
            if (!shown.HasValue || !shown.Value.Equals(monkey))
            {
                shown = monkey;
                Fill(monkey);
                ShowPreview(monkey);
                smoothAnchor = anchor;
            }

            // Position : au-dessus du singe, un peu lissée ; taille : grandit avec la distance pour rester lisible
            smoothAnchor = Vector3.Lerp(smoothAnchor, anchor, 1f - Mathf.Exp(-15f * Time.deltaTime));
            card.position = smoothAnchor;
            var cam = Camera.main;
            float distance = cam ? Vector3.Distance(cam.transform.position, anchor) : readableDistance;
            card.localScale = Vector3.one * Mathf.Max(1f, distance / readableDistance);

            showcase.Rotate(0f, 40f * Time.deltaTime, 0f);   // le singe tourne doucement dans sa vitrine

            if (rangeCircle.gameObject.activeSelf) DrawCircle(rangeCenter, MonkeyData.Range(monkey) * rangeScale);
        }

        void DrawCircle(Vector3 center, float radius)
        {
            rangeCircle.widthMultiplier = Mathf.Max(0.005f, radius * 0.02f);
            for (int i = 0; i < rangeCircle.positionCount; i++)
            {
                float a = i * Mathf.PI * 2f / rangeCircle.positionCount;
                // Le cercle suit l'inclinaison du plateau : on prend le plan de la surface visée
                var right = aimedSurface ? aimedSurface.transform.right : Vector3.right;
                var forward = aimedSurface ? aimedSurface.transform.forward : Vector3.forward;
                rangeCircle.SetPosition(i, center + (right * Mathf.Cos(a) + forward * Mathf.Sin(a)) * radius);
            }
        }

        // Les textes et les barres. Une barre pleine = à peu près le meilleur singe du jeu pour cette caractéristique.
        void Fill(Monkey m)
        {
            title.text = m.type.ToString().ToUpper();
            rarity.text = $"{MonkeyData.RarityName(m.level)}  ·  niveau {(int)m.level + 1}";
            rarity.color = MonkeyData.IsRainbow(m.level) ? Color.magenta : MonkeyData.RarityColor(m.level);
            effect.text = MonkeyData.Effect(m.type);

            SetStat(0, $"{MonkeyData.Pierce(m)} couche(s)", MonkeyData.Pierce(m) / 12f);
            SetStat(1, Range(m), MonkeyData.Range(m) / 10f);
            SetStat(2, $"{MonkeyData.FireRate(m):0.#} tir/s", MonkeyData.FireRate(m) / 3.5f);
            SetStat(3, Targets(m), MonkeyData.Targets(m) / 4f);

            if (m.level < Rarity.Blanc)
            {
                var up = m.Upgraded();
                fusion.text = $"Fusion → {MonkeyData.RarityName(up.level)} : perce {MonkeyData.Pierce(up)}, {MonkeyData.FireRate(up):0.#} tir/s";
            }
            else fusion.text = "Niveau maximum";

            foreach (var tm in card.GetComponentsInChildren<TextMesh>()) RefreshFont(tm);
        }

        // Une ligne de caractéristique : la valeur écrite et la barre remplie de 0 à 1, depuis la gauche.
        void SetStat(int i, string value, float fill)
        {
            fill = Mathf.Clamp(fill, 0.04f, 1f);
            statValues[i].text = value;
            var bar = statBars[i];
            bar.localScale = new Vector3(BarWidth * fill, BarHeight, 0.002f);
            bar.localPosition = new Vector3(BarX - BarWidth / 2f + BarWidth * fill / 2f, bar.localPosition.y, bar.localPosition.z);
        }

        static string Range(Monkey m) => MonkeyData.Range(m) >= 30f ? "toute la carte" : $"{MonkeyData.Range(m):0.#} m";
        static string Targets(Monkey m) => MonkeyData.Targets(m) >= 99 ? "toutes" : MonkeyData.Targets(m).ToString();
    }
}
