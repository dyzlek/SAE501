using UnityEngine;

namespace SAE
{
    // Petit texte « +5 » (vert) ou « -25 » (rouge) qui monte et s'efface là où l'argent a bougé.
    // C'est le retour visuel de l'économie, posé dans le décor plutôt qu'à l'écran.
    public class MoneyPopup : MonoBehaviour
    {
        const float Duration = 1.3f;
        const float Rise = 0.6f;

        TextMesh text;
        Color color;
        Vector3 start;
        float t;

        public static void Spawn(Vector3 position, int delta)
        {
            var go = new GameObject("Argent " + delta);
            go.transform.position = position;
            var popup = go.AddComponent<MoneyPopup>();
            popup.color = delta >= 0 ? new Color(0.35f, 1f, 0.35f) : new Color(1f, 0.3f, 0.3f);
            popup.text = Visuals.Label(go.transform, (delta >= 0 ? "+" : "") + delta, Vector3.zero, 0.14f, popup.color);
            popup.start = position;
        }

        void Update()
        {
            t += Time.deltaTime / Duration;
            if (t >= 1f) { Destroy(gameObject); return; }
            transform.position = start + Vector3.up * (Rise * (1f - (1f - t) * (1f - t)));   // monte vite puis ralentit
            float scale = t < 0.15f ? Mathf.Lerp(0.3f, 1.2f, t / 0.15f) : Mathf.Lerp(1.2f, 1f, (t - 0.15f) * 4f);
            transform.localScale = Vector3.one * scale;
            text.color = new Color(color.r, color.g, color.b, 1f - Mathf.Clamp01((t - 0.6f) / 0.4f));
        }
    }
}
