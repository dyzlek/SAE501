using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SAE
{
    // Fiche d'un singe : quand on vise un singe (bibliothèque, plateau ou carte) EN MAINTENANT le bouton d'infos
    // (touche A du clavier AZERTY, bouton A de la manette droite en VR), une petite carte apparaît
    // DANS LE DÉCOR, au-dessus de lui, avec ses caractéristiques et ce que donnerait une fusion.
    // Sur le plateau et la carte, un cercle montre aussi sa portée.
    // Pas d'affichage collé à l'écran (règle de confort VR) : la carte reste posée près du singe.
    public class MonkeyInfoCard : MonoBehaviour
    {
        public float textHeight = 0.04f;        // hauteur d'une ligne, en mètres, vue de près
        public float readableDistance = 1.2f;   // au-delà, la carte grandit pour rester lisible

        // Le bouton d'infos. Le clavier est lu par position de touche : « <Keyboard>/q » = la touche A en AZERTY.
        InputAction showInfo;

        Transform card;
        TextMesh text;
        LineRenderer rangeCircle;
        Monkey? shown;
        Vector3 smoothAnchor;

        void OnEnable()
        {
            showInfo = new InputAction("Infos du singe", InputActionType.Button);
            showInfo.AddBinding("<Keyboard>/q");
            showInfo.AddBinding("<XRController>{RightHand}/primaryButton");
            showInfo.Enable();
        }

        void OnDisable() => showInfo?.Disable();

        void Start()
        {
            card = new GameObject("Fiche du singe").transform;
            card.gameObject.AddComponent<Billboard>();
            Visuals.Box("Fond", card, new Vector3(0, 0, 0.01f), new Vector3(0.95f, 0.3f, 0.01f), new Color(0.08f, 0.07f, 0.06f));
            text = Visuals.Label(card, "", Vector3.zero, textHeight);
            Destroy(text.GetComponent<Billboard>());   // c'est la carte entière qui fait face au joueur
            card.gameObject.SetActive(false);

            rangeCircle = new GameObject("Cercle de portée").AddComponent<LineRenderer>();
            rangeCircle.sharedMaterial = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit"));
            rangeCircle.loop = true;
            rangeCircle.positionCount = 48;
            rangeCircle.startColor = rangeCircle.endColor = new Color(1f, 1f, 1f, 0.8f);
            rangeCircle.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            var player = PlayerController.Local;
            Monkey monkey = default;
            Vector3 anchor = default, rangeCenter = default;
            float rangeScale = 0f;
            bool visible = showInfo.IsPressed() && player && player.Target is IMonkeyInfo info
                           && info.TryGetMonkeyInfo(player.AimPoint, out monkey, out anchor, out rangeCenter, out rangeScale);

            card.gameObject.SetActive(visible);
            rangeCircle.gameObject.SetActive(visible && rangeScale > 0f && MonkeyData.Range(monkey) < 30f);   // pas de cercle géant pour le Sniper
            if (!visible) { shown = null; return; }

            // Texte (refait seulement quand le singe visé change)
            if (!shown.HasValue || !shown.Value.Equals(monkey))
            {
                shown = monkey;
                text.text = Describe(monkey);
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
                var surface = PlayerController.Local.Target as Component;
                var right = surface ? surface.transform.right : Vector3.right;
                var forward = surface ? surface.transform.forward : Vector3.forward;
                rangeCircle.SetPosition(i, center + (right * Mathf.Cos(a) + forward * Mathf.Sin(a)) * radius);
            }
        }

        // Les caractéristiques, et en vert ce que donnerait la fusion (2 singes identiques → niveau suivant).
        static string Describe(Monkey m)
        {
            string hex = ColorUtility.ToHtmlStringRGB(MonkeyData.RarityColor(m.level));
            var sb = new StringBuilder();
            sb.AppendLine($"<b>{m.type}</b>  <color=#{hex}>{MonkeyData.RarityName(m.level)}</color>  (niv {(int)m.level + 1})");
            sb.AppendLine($"<color=#CCCCCC>{MonkeyData.Effect(m.type)}</color>");
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
