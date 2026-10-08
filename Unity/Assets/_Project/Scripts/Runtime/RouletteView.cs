using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Sae501.Coffres
{
    // Roulette façon caisses CS : une bande de cases qui défile derrière une barre centrale,
    // et ralentit jusqu'à s'arrêter sur le singe tiré. Chaque case montre un vrai singe 3D avec l'aura
    // de sa rareté, sur un fond de la couleur de la rareté. Pour le casque, seuls les singes qui passent
    // dans la fenêtre sont allumés (environ 7 sur 48).
    // Le canvas est en World Space (dans le décor) : utilisable à l'écran comme en VR.
    // Même habillage que les ardoises du hub : cadre en bois, fond ardoise, barre et titre dorés, polices du jeu.
    public class RouletteView : MonoBehaviour
    {
        [Header("Taille (en pixels de canvas, 1 px = 2 mm)")]
        public float viewportWidth = 1060f;
        public float itemWidth = 150f;
        public float itemHeight = 170f;
        public float gap = 10f;

        [Header("Bande")]
        public int itemCount = 100;
        public int winnerIndex = 92;   // case sur laquelle la roulette s'arrête (loin : ça défile très vite au début)
        public int startIndex = 3;     // case centrée au départ

        GameObject canvasRoot;
        RectTransform strip;
        Image[] items;
        GameObject[] pieces;           // le singe 3D de chaque case (créé seulement quand la case passe dans la fenêtre)
        SAE.Monkey[] monkeys;          // le singe de chaque case
        Image bar;
        RectTransform viewportRt;
        Text message;
        Sprite rainbow;

        float Pitch => itemWidth + gap;

        // Couleurs de la cabane (les mêmes que la fiche du singe et les ardoises)
        static readonly Color Wood = new Color(0.45f, 0.3f, 0.18f);
        static readonly Color DarkWood = new Color(0.3f, 0.19f, 0.11f);
        static readonly Color Slate = new Color(0.13f, 0.17f, 0.15f, 0.97f);
        static readonly Color DeepSlate = new Color(0.07f, 0.09f, 0.08f);
        static readonly Color Gold = new Color(1f, 0.83f, 0.35f);

        void Awake() => Build();

        void Build()
        {
            var canvasGo = new GameObject("RouletteCanvas", typeof(RectTransform), typeof(Canvas));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localScale = Vector3.one * 0.002f; // 1100 px -> 2,2 m
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRt = (RectTransform)canvasGo.transform;
            canvasRt.sizeDelta = new Vector2(1100f, 330f);

            // Cadre en bois, ardoise, puis un liseré de bois foncé autour de la fenêtre
            MakeImage(canvasRt, "Cadre", Wood, Vector2.zero, canvasRt.sizeDelta + new Vector2(30f, 30f));
            MakeImage(canvasRt, "Ardoise", Slate, Vector2.zero, canvasRt.sizeDelta);
            MakeImage(canvasRt, "Bord de la fenetre", DarkWood, new Vector2(0f, 50f), new Vector2(viewportWidth + 16f, itemHeight + 36f));

            // Fenêtre qui masque ce qui dépasse de la bande.
            var viewport = MakeImage(canvasRt, "Fenetre", DeepSlate,
                new Vector2(0f, 50f), new Vector2(viewportWidth, itemHeight + 20f));
            viewport.gameObject.AddComponent<RectMask2D>();
            viewportRt = viewport.rectTransform;

            // La bande : ses cases sont posées à gauche, on la déplace en X pour faire défiler.
            var stripGo = new GameObject("Bande", typeof(RectTransform));
            strip = (RectTransform)stripGo.transform;
            strip.SetParent(viewport.rectTransform, false);
            strip.anchorMin = strip.anchorMax = new Vector2(0f, 0.5f);
            strip.pivot = new Vector2(0f, 0.5f);
            strip.sizeDelta = new Vector2(itemCount * Pitch, itemHeight);

            rainbow = MakeRainbowSprite();
            items = new Image[itemCount];
            pieces = new GameObject[itemCount];
            monkeys = new SAE.Monkey[itemCount];
            for (int i = 0; i < itemCount; i++)
            {
                var item = MakeImage(strip, "Case " + i, Color.gray, Vector2.zero, new Vector2(itemWidth, itemHeight));
                var rt = item.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(i * Pitch, 0f);
                items[i] = item;
            }

            // Barre centrale (au-dessus de la bande).
            bar = MakeImage(canvasRt, "Barre", Gold, new Vector2(0f, 50f), new Vector2(8f, itemHeight + 50f));

            message = MakeText(canvasRt, "Message", 56, new Vector2(0f, -100f), new Vector2(1060f, 80f));
            message.text = "";

            Fill(Rarity.Gris, new[] { 1f, 0, 0, 0, 0, 0, 0 });
            SetStripX(CenteredX(startIndex, 0f));

            canvasRoot = canvasGo;
            canvasRoot.SetActive(false); // invisible tant qu'on n'ouvre pas de coffre
        }

        // ---------- API ----------

        public void SetVisible(bool visible) => canvasRoot.SetActive(visible);

        public void ShowMessage(string text, Color color)
        {
            message.text = text;
            message.color = color;
        }

        // Lance la roulette : 'winner' est déjà tiré, la bande est remplie autour.
        public IEnumerator Spin(Rarity winner, float[] odds, float duration, int winnerType = 0, System.Func<int> rollType = null)
        {
            ShowMessage("", Color.white);
            bar.enabled = true;
            Fill(winner, odds, winnerType, rollType);

            float from = CenteredX(startIndex, 0f);
            // Arrêt à un endroit aléatoire à l'intérieur de la case gagnante (pas toujours pile au milieu).
            float to = CenteredX(winnerIndex, Random.Range(-0.4f, 0.4f) * itemWidth);

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                float eased = 1f - Mathf.Pow(1f - k, 4f); // démarre vite, ralentit à la fin
                SetStripX(Mathf.Lerp(from, to, eased));
                yield return null;
            }
            SetStripX(to);
            bar.enabled = false;   // le singe est choisi : la barre s'en va, il va sortir du coffre
        }

        // ---------- Interne ----------

        // Position X de la bande pour que la case 'index' (décalée de 'offset') soit sous la barre.
        float CenteredX(int index, float offset) =>
            viewportWidth * 0.5f - (index * Pitch + itemWidth * 0.5f + offset);

        void SetStripX(float x)
        {
            strip.anchoredPosition = new Vector2(x, 0f);
            ShowVisiblePieces();
        }

        // Seuls les singes dans la fenêtre existent : on les crée quand leur case y entre et on les détruit quand
        // elle en sort (le masque de l'interface ne cache pas les objets 3D, et 100 singes avec leur aura,
        // ce serait trop lourd pour le casque).
        void ShowVisiblePieces()
        {
            if (pieces == null || monkeys == null) return;
            float half = viewportWidth * 0.5f;
            for (int i = 0; i < pieces.Length; i++)
            {
                float center = strip.anchoredPosition.x + i * Pitch + itemWidth * 0.5f - half;   // position dans la fenêtre
                bool visible = Mathf.Abs(center) < half - itemWidth * 0.4f;
                if (visible && !pieces[i]) PlacePiece(i);
                else if (!visible && pieces[i]) { Destroy(pieces[i]); pieces[i] = null; }
            }
        }

        // Remplit les cases au hasard selon les probabilités, sauf la case gagnante (le vrai singe tiré).
        void Fill(Rarity winner, float[] odds, int winnerType = 0, System.Func<int> rollType = null)
        {
            for (int i = 0; i < items.Length; i++)
            {
                bool win = i == winnerIndex;
                var r = win ? winner : ChestOdds.Roll(odds);
                // Les autres cases montrent TOUS les types de singes (pour le spectacle) ; seule la case gagnante
                // est le vrai tirage (rollType), qui suit les types débloqués par les vagues
                int type = win ? winnerType : Random.Range(0, SAE.MonkeyData.TypeCount);
                Paint(items[i], r);
                monkeys[i] = new SAE.Monkey((SAE.MonkeyType)type, (SAE.Rarity)(int)r);   // mêmes raretés de Gris à LGBT/Arc-en-ciel
                if (pieces[i]) { Destroy(pieces[i]); pieces[i] = null; }
            }
            ShowVisiblePieces();
        }

        // Fond de case : la couleur de la rareté mêlée à l'ardoise, pour que le singe et son aura ressortent.
        void Paint(Image img, Rarity r)
        {
            if (r == Rarity.LGBT) { img.sprite = rainbow; img.color = new Color(0.55f, 0.55f, 0.55f); }
            else { img.sprite = null; img.color = Color.Lerp(RarityInfo.ColorOf(r), DeepSlate, 0.55f); }
        }

        // Le singe 3D de la case, devant le fond (côté joueur : -Z), en unités de canvas (pixels).
        void PlacePiece(int i)
        {
            float size = itemHeight * 0.7f;
            pieces[i] = SAE.Visuals.MonkeyPiece(monkeys[i], items[i].rectTransform, new Vector3(itemWidth * 0.5f, 0f, -size * 0.6f), size, withLabel: false);
        }

        static Image MakeImage(RectTransform parent, string name, Color color, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static Text MakeText(RectTransform parent, string name, int size, Vector2 pos, Vector2 boxSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchoredPosition = pos;
            rt.sizeDelta = boxSize;
            var t = go.GetComponent<Text>();
            t.font = SAE.Visuals.TitleFont;   // Bangers, comme les titres du hub
            t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            return t;
        }

        // Dégradé arc-en-ciel horizontal pour la rareté LGBT.
        static Sprite MakeRainbowSprite()
        {
            const int w = 128;
            var tex = new Texture2D(w, 2, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < w; x++)
            {
                var c = Color.HSVToRGB(x / (float)(w - 1), 0.85f, 1f);
                tex.SetPixel(x, 0, c);
                tex.SetPixel(x, 1, c);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, 2), new Vector2(0.5f, 0.5f));
        }
    }
}
