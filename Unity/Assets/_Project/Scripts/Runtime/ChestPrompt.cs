using UnityEngine;
using UnityEngine.UI;

namespace Sae501.Coffres
{
    // Indication au-dessus du coffre : "[E] Ouvrir le coffre", visible quand le joueur est assez près.
    // Sert aussi à afficher l'erreur "pas assez de money" pendant un court instant.
    // Canvas en World Space + Billboard : le texte se tourne vers le joueur.
    public class ChestPrompt : MonoBehaviour
    {
        public ChestController chest;
        public Transform player;
        public string keyLabel = "E";
        public float errorDuration = 2f;

        GameObject canvasRoot;
        Text label;
        float errorUntil;

        void Awake() => Build();

        void Build()
        {
            canvasRoot = new GameObject("PromptCanvas", typeof(RectTransform), typeof(Canvas));
            canvasRoot.transform.SetParent(transform, false);
            canvasRoot.transform.localScale = Vector3.one * 0.002f; // 600 px -> 1,2 m
            canvasRoot.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)canvasRoot.transform;
            rt.sizeDelta = new Vector2(600f, 110f);

            var bgGo = new GameObject("Fond", typeof(RectTransform), typeof(Image));
            var bgRt = (RectTransform)bgGo.transform;
            bgRt.SetParent(rt, false);
            bgRt.sizeDelta = rt.sizeDelta;
            var bg = bgGo.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.65f);
            bg.raycastTarget = false;

            var textGo = new GameObject("Texte", typeof(RectTransform), typeof(Text));
            var textRt = (RectTransform)textGo.transform;
            textRt.SetParent(rt, false);
            textRt.sizeDelta = rt.sizeDelta;
            label = textGo.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 52;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;

            canvasRoot.SetActive(false);
        }

        public void ShowError(string message)
        {
            label.text = message;
            label.color = new Color(1f, 0.35f, 0.35f);
            errorUntil = Time.time + errorDuration;
        }

        void Update()
        {
            bool showError = Time.time < errorUntil;
            bool showHint = !showError && !chest.IsBusy && chest.IsInRange(player.position);

            if (showHint)
            {
                label.text = $"[{keyLabel}]  Ouvrir le coffre";
                label.color = Color.white;
            }
            canvasRoot.SetActive(showError || showHint);
        }
    }
}
