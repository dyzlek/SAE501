using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Sae501.Coffres
{
    // Roulette façon caisses CS : une bande de rectangles colorés qui défile derrière
    // une barre centrale, et ralentit jusqu'à s'arrêter sur la couleur tirée.
    // Le canvas est en World Space (dans le décor) : utilisable à l'écran comme en VR.
    public class RouletteView : MonoBehaviour
    {
        [Header("Taille (en pixels de canvas, 1 px = 2 mm)")]
        public float viewportWidth = 1060f;
        public float itemWidth = 150f;
        public float itemHeight = 170f;
        public float gap = 10f;

        [Header("Bande")]
        public int itemCount = 48;
        public int winnerIndex = 40;   // case sur laquelle la roulette s'arrête
        public int startIndex = 3;     // case centrée au départ

        GameObject canvasRoot;
        RectTransform strip;
        Image[] items;
        Text message;
        Sprite rainbow;

        float Pitch => itemWidth + gap;

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

            MakeImage(canvasRt, "Fond", new Color(0f, 0f, 0f, 0.65f), Vector2.zero, canvasRt.sizeDelta);

            // Fenêtre qui masque ce qui dépasse de la bande.
            var viewport = MakeImage(canvasRt, "Fenetre", new Color(0.08f, 0.08f, 0.1f, 1f),
                new Vector2(0f, 50f), new Vector2(viewportWidth, itemHeight + 20f));
            viewport.gameObject.AddComponent<RectMask2D>();

            // La bande : ses cases sont posées à gauche, on la déplace en X pour faire défiler.
            var stripGo = new GameObject("Bande", typeof(RectTransform));
            strip = (RectTransform)stripGo.transform;
            strip.SetParent(viewport.rectTransform, false);
            strip.anchorMin = strip.anchorMax = new Vector2(0f, 0.5f);
            strip.pivot = new Vector2(0f, 0.5f);
            strip.sizeDelta = new Vector2(itemCount * Pitch, itemHeight);

            rainbow = MakeRainbowSprite();
            items = new Image[itemCount];
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
            MakeImage(canvasRt, "Barre", new Color(1f, 0.95f, 0.3f, 1f), new Vector2(0f, 50f), new Vector2(8f, itemHeight + 50f));

            message = MakeText(canvasRt, "Message", 44, new Vector2(0f, -100f), new Vector2(1060f, 70f));
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
        public IEnumerator Spin(Rarity winner, float[] odds, float duration)
        {
            ShowMessage("", Color.white);
            Fill(winner, odds);

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
        }

        // ---------- Interne ----------

        // Position X de la bande pour que la case 'index' (décalée de 'offset') soit sous la barre.
        float CenteredX(int index, float offset) =>
            viewportWidth * 0.5f - (index * Pitch + itemWidth * 0.5f + offset);

        void SetStripX(float x) => strip.anchoredPosition = new Vector2(x, 0f);

        // Remplit les cases au hasard selon les probabilités, sauf la case gagnante.
        void Fill(Rarity winner, float[] odds)
        {
            for (int i = 0; i < items.Length; i++)
            {
                var r = i == winnerIndex ? winner : ChestOdds.Roll(odds);
                Paint(items[i], r);
            }
        }

        void Paint(Image img, Rarity r)
        {
            if (r == Rarity.LGBT) { img.sprite = rainbow; img.color = Color.white; }
            else { img.sprite = null; img.color = RarityInfo.ColorOf(r); }
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
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
