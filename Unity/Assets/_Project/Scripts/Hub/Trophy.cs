using System.Globalization;
using UnityEngine;

namespace SAE
{
    // Ce que compte un trophée : les vagues vaincues, ou les bananes gagnées depuis le début (panier, vagues, roulette).
    public enum TrophyGoal { Vagues, Bananes }

    // Un trophée de l'armoire du hub (sous la fenêtre, derrière à droite). Tant qu'il n'est pas gagné, il est sombre,
    // comme une silhouette, et sa plaque montre où on en est (« 37 / 50 »). Une fois l'objectif atteint, il prend
    // sa couleur (bronze, argent, or, puis diamant arc-en-ciel), tourne doucement sur lui-même, et « saute » une fois
    // si on vient de le gagner. Plus il est prestigieux, plus il est grand (c'est le générateur qui le construit).
    // Pas de sauvegarde dans le jeu : les trophées repartent de zéro à chaque partie, comme l'argent.
    public class Trophy : MonoBehaviour
    {
        // Les paliers, du moins au plus prestigieux (bronze, argent, or, diamant)
        public static readonly int[] WaveGoals = { 10, 50, 100, 200 };
        public static readonly int[] BananaGoals = { 1000, 5000, 15000, 50000 };
        public static readonly Color[] TierColors =
        {
            new Color(0.80f, 0.50f, 0.25f),   // bronze
            new Color(0.85f, 0.87f, 0.92f),   // argent
            new Color(1.00f, 0.80f, 0.20f),   // or
            new Color(0.70f, 0.95f, 1.00f),   // diamant (arc-en-ciel une fois gagné)
        };
        public const int TierCount = 4;

        public TrophyGoal goal;
        public int target;                   // le nombre de vagues ou de bananes à atteindre
        public int tier;                     // 0 = bronze … 3 = diamant
        public ColorTint[] metal;            // les morceaux qui prennent la couleur du trophée
        public TextMesh plaque;              // la plaque gravée au bord de l'étagère (elle ne tourne pas avec le trophée)

        static readonly Color Unpolished = new Color(0.16f, 0.16f, 0.18f);   // pas encore gagné : une silhouette sombre
        static readonly Color PlaqueGold = new Color(1f, 0.83f, 0.35f);
        static readonly Color PlaqueDim = new Color(0.62f, 0.60f, 0.55f);
        const float SpinSpeed = 30f;         // degrés par seconde : lent, juste pour faire briller
        const float PunchSize = 0.35f;       // le trophée grossit de 35 % au moment où on le gagne

        bool won;
        float punch;                         // 1 = vient d'être gagné, redescend à 0
        Vector3 baseScale;

        bool IsTopTier => tier == TierCount - 1;
        int Progress => goal == TrophyGoal.Vagues ? GameState.WavesWon : GameState.BananasEarned;
        string Unit => goal == TrophyGoal.Vagues ? "VAGUES" : "BANANES";

        void Awake() => baseScale = transform.localScale;

        // Une vague gagnée ou une banane déposée font toujours bouger l'argent : on se met à jour à ce moment-là
        void OnEnable()
        {
            Economy.MoneyChanged += OnMoneyChanged;
            Refresh(false);
        }

        void OnDisable() => Economy.MoneyChanged -= OnMoneyChanged;

        void OnMoneyChanged(int delta, Vector3? where) => Refresh(true);

        void Refresh(bool celebrate)
        {
            bool nowWon = Progress >= target;
            if (nowWon && !won && celebrate) punch = 1f;
            won = nowWon;

            var color = won ? TierColors[tier] : Unpolished;
            foreach (var part in metal)
                if (part) part.Set(color, won && IsTopTier);
            if (plaque)
            {
                plaque.text = won ? $"{Format(target)} {Unit}" : $"{Format(target)} {Unit}\n{Format(Mathf.Min(Progress, target))} / {Format(target)}";
                plaque.color = won ? PlaqueGold : PlaqueDim;
            }
        }

        void Update()
        {
            if (!won) return;
            transform.Rotate(0f, SpinSpeed * Time.deltaTime, 0f, Space.Self);
            if (punch <= 0f) return;
            punch = Mathf.MoveTowards(punch, 0f, Time.deltaTime * 1.5f);
            transform.localScale = baseScale * (1f + PunchSize * Mathf.Sin(punch * Mathf.PI));
        }

        // 15000 -> « 15 000 » (espace des milliers, la même sur tous les casques quelle que soit leur langue)
        static string Format(int n) => n.ToString("#,0", CultureInfo.InvariantCulture).Replace(',', ' ');
    }
}
