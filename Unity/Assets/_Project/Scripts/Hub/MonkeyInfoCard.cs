using System.Text;
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
    public class MonkeyInfoCard : MonoBehaviour
    {
        public float textHeight = 0.04f;        // hauteur d'une ligne, en mètres, vue de près
        public float readableDistance = 1.2f;   // au-delà, la carte grandit pour rester lisible
        public float reach = 15f;               // portée de la visée, en mètres

        const float CardWidth = 0.95f, CardHeight = 0.4f;   // en mètres, vue de près
        const int OnTopQueue = 4000;                         // après tout le reste (file « Overlay ») : la fiche passe devant
        static readonly Color FrameColor = new Color(0.45f, 0.3f, 0.18f);
        static readonly Color BackColor = new Color(0.1f, 0.13f, 0.12f, 0.95f);
        static readonly Color TitleColor = new Color(1f, 0.83f, 0.35f);
        static readonly Color ChalkColor = new Color(0.95f, 0.94f, 0.88f);

        // Le bouton d'infos : A sur la manette droite.
        InputAction showInfo;
        Component aimedSurface;                 // ce qu'on vise : sert à incliner le cercle de portée comme le plateau

        Transform card;
        TextMesh title, text;
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
            var frame = Visuals.Box("Cadre", card, new Vector3(0, 0, 0.004f), new Vector3(CardWidth + 0.04f, CardHeight + 0.04f, 0.002f), FrameColor);
            var back = Visuals.Box("Fond", card, new Vector3(0, 0, 0.002f), new Vector3(CardWidth, CardHeight, 0.002f), BackColor);
            title = Visuals.Text(card, "", new Vector3(0, CardHeight / 2f - 0.055f, 0), textHeight * 1.6f, TitleColor, title: true);
            text = Visuals.Text(card, "", new Vector3(0, -0.035f, 0), textHeight, ChalkColor);
            // Dessinés dans l'ordre (cadre, fond, textes), par-dessus tout le décor
            DrawOnTop(frame.GetComponent<Renderer>(), null, OnTopQueue);
            DrawOnTop(back.GetComponent<Renderer>(), null, OnTopQueue + 1);
            DrawOnTop(title.GetComponent<Renderer>(), title.GetComponent<Renderer>().sharedMaterial, OnTopQueue + 2);
            DrawOnTop(text.GetComponent<Renderer>(), text.GetComponent<Renderer>().sharedMaterial, OnTopQueue + 2);
            card.gameObject.SetActive(false);

            rangeCircle = new GameObject("Cercle de portée").AddComponent<LineRenderer>();
            rangeCircle.sharedMaterial = Visuals.LineMaterial;
            rangeCircle.loop = true;
            rangeCircle.positionCount = 48;
            rangeCircle.startColor = rangeCircle.endColor = new Color(1f, 1f, 1f, 0.8f);
            rangeCircle.gameObject.SetActive(false);
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
            if (!visible) { shown = null; return; }

            // Texte (refait seulement quand le singe visé change)
            if (!shown.HasValue || !shown.Value.Equals(monkey))
            {
                shown = monkey;
                title.text = monkey.type.ToString().ToUpper();
                text.text = Describe(monkey);
                RefreshFont(title);
                RefreshFont(text);
                smoothAnchor = anchor;
            }

            // Position : au-dessus du singe, un peu lissée ; taille : grandit avec la distance pour rester lisible
            smoothAnchor = Vector3.Lerp(smoothAnchor, anchor, 1f - Mathf.Exp(-15f * Time.deltaTime));
            card.position = smoothAnchor;
            var cam = Camera.main;
            float distance = cam ? Vector3.Distance(cam.transform.position, anchor) : readableDistance;
            card.localScale = Vector3.one * Mathf.Max(1f, distance / readableDistance);

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

        // Les caractéristiques, et en vert ce que donnerait la fusion (2 singes identiques → niveau suivant).
        static string Describe(Monkey m)
        {
            string hex = ColorUtility.ToHtmlStringRGB(MonkeyData.RarityColor(m.level));
            var sb = new StringBuilder();
            sb.AppendLine($"<color=#{hex}>{MonkeyData.RarityName(m.level)}</color>  ·  niveau {(int)m.level + 1}");
            sb.AppendLine($"<color=#B8C8B8>{MonkeyData.Effect(m.type)}</color>");
            sb.AppendLine($"Dégâts {MonkeyData.Damage(m):0.#}   Portée {Range(m)}");
            sb.AppendLine($"Cadence {MonkeyData.FireRate(m):0.#} tir/s   Cibles {Targets(m)}");
            if (m.level < Rarity.Blanc)
            {
                var up = m.Upgraded();
                sb.Append($"<color=#7CFC7C>Fusion → {MonkeyData.RarityName(up.level)} : dégâts {MonkeyData.Damage(up):0.#}, " +
                          $"{MonkeyData.FireRate(up):0.#} tir/s</color>");
            }
            else sb.Append("<color=#FFFFFF>Niveau maximum</color>");
            return sb.ToString();
        }

        static string Range(Monkey m) => MonkeyData.Range(m) >= 30f ? "toute la carte" : $"{MonkeyData.Range(m):0.#} m";
        static string Targets(Monkey m) => MonkeyData.Targets(m) >= 99 ? "toutes" : MonkeyData.Targets(m).ToString();
    }
}
