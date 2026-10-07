using UnityEngine;

namespace SAE
{
    // La caisse du hub : un panneau dans le décor qui affiche l'argent total.
    // Le chiffre défile jusqu'à la nouvelle valeur, le panneau « saute » et clignote vert (gain) ou rouge (dépense).
    // C'est aussi lui qui fait apparaître les « +5 / -25 » flottants pour tout le jeu.
    public class MoneyBoard : MonoBehaviour
    {
        public TextMesh amount;
        public Transform panel;              // la partie qui saute
        public ColorTint frame;              // le cadre qui clignote

        static readonly Color Gold = new Color(1f, 0.82f, 0.2f);
        static readonly Color FrameColor = new Color(0.45f, 0.30f, 0.18f);

        float shown;
        float punch;
        Color flash;
        Vector3 panelScale;

        void OnEnable() => Economy.MoneyChanged += OnMoneyChanged;
        void OnDisable() => Economy.MoneyChanged -= OnMoneyChanged;

        void Start()
        {
            shown = Economy.Money;
            if (panel) panelScale = panel.localScale;
        }

        void OnMoneyChanged(int delta, Vector3? where)
        {
            if (delta == 0) return;   // achat gratuit : pas de « +0 » qui flotte, la caisse ne bouge pas
            punch = 1f;
            flash = delta >= 0 ? new Color(0.3f, 0.9f, 0.3f) : new Color(0.95f, 0.25f, 0.25f);
            MoneyPopup.Spawn((where ?? transform.position + Vector3.up * 0.3f) + Vector3.up * 0.4f, delta);
        }

        void Update()
        {
            // Le chiffre rattrape la vraie valeur (vite si l'écart est grand)
            float target = Economy.Money;
            shown = Mathf.MoveTowards(shown, target, Mathf.Max(20f, Mathf.Abs(target - shown) * 4f) * Time.deltaTime);
            if (amount)
            {
                amount.text = Mathf.RoundToInt(shown).ToString();
                amount.color = Color.Lerp(Gold, Color.white, punch);
            }

            punch = Mathf.MoveTowards(punch, 0f, Time.deltaTime * 2.5f);
            if (panel) panel.localScale = panelScale * (1f + 0.12f * Mathf.Sin(punch * Mathf.PI));
            if (frame) frame.Set(Color.Lerp(FrameColor, flash, punch));
        }
    }
}
