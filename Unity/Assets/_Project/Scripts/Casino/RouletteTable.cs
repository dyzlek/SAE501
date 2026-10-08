using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // La table de roulette (bêta-test, sur l'estrade de la carte) : le pari, la mise, le tirage et les gains.
    // On choisit une couleur en cliquant une case de la roue : toutes les cases de cette couleur s'entourent d'une lueur
    // (cliquer une case d'une autre couleur remplace le choix). La lueur reste jusqu'au lancer.
    // La mise part à 1 banane ; MISE - / MISE + l'ajustent de 10 (jamais sous 1), TOUT mise toutes ses bananes.
    // Elle n'est payée qu'au LANCER, puis elle revient à 1 quel que soit le résultat : après un gros « tout » gagné ou perdu,
    // on n'a pas à redescendre de 10 en 10 pour faire une petite mise.
    // À côté de la roue, des bananes posées sur le tapis montrent la mise (comme dans How to Fish) : une par tranche
    // de 25 bananes misées, 6 au plus pour ne pas encombrer le tapis. Pendant que la roue tourne, elles montrent la mise en jeu.
    // Bouton « 100 % » (bêta-test) : la bille tombe forcément sur la couleur choisie, pour vérifier que les gains tombent bien.
    public class RouletteTable : MonoBehaviour
    {
        public RouletteWheel wheel;
        public TextMesh board;              // l'ardoise au-dessus du pupitre
        public GameObject[] stakeBananas;   // les bananes du tapis des mises, de la première à la dernière (6)
        public TextMesh stakeLabel;         // la mise écrite au-dessus du tapis

        public const int Step = 10;         // bananes ajoutées ou retirées à chaque appui
        public const int BananasPerPile = 25;   // 1 à 25 misées : 1 banane sur le tapis, 26 à 50 : 2, etc.
        const int HistorySize = 8;          // dernières couleurs sorties, affichées sur l'ardoise
        static readonly Color GlowColor = new Color(1f, 0.85f, 0.2f);

        RoulettePocket[] pockets;
        RouletteColor? selected;            // la couleur choisie (null = aucune)
        int stake = 1;
        int played;                         // la mise en jeu pendant que la roue tourne
        readonly List<RouletteColor> history = new List<RouletteColor>();
        string lastResult = "Clique une couleur sur la roue, puis LANCER !";

        public bool Guaranteed { get; private set; }
        public bool Spinning => wheel.Spinning;
        public int Stake => stake;
        public bool CanSpin => !Spinning && selected.HasValue && Economy.CanAfford(stake);
        public bool CanRaise => stake < Economy.Money;

        void Awake() => pockets = GetComponentsInChildren<RoulettePocket>();

        void Start()
        {
            ShowGlow();
            Refresh();
        }

        // Les lueurs allumées pulsent doucement pour attirer l'œil
        void Update()
        {
            if (!selected.HasValue) return;
            var c = GlowColor * (0.75f + 0.25f * Mathf.Sin(Time.time * 6f));
            foreach (var p in pockets)
                if (p.color == selected.Value) p.glow.Set(c);
        }

        // --- Les cases (RoulettePocket) et les boutons (RouletteButton) ---

        // Choisir une couleur : ses cases s'allument et remplacent l'ancien choix. Pas pendant que la roue tourne.
        public void Select(RouletteColor color)
        {
            if (Spinning) return;
            selected = color;
            ShowGlow();
            Refresh();
        }

        public bool Raise()
        {
            if (!CanRaise) return false;
            stake = Mathf.Min(stake + Step, Economy.Money);
            Refresh();
            return true;
        }

        public bool Lower()
        {
            if (stake <= 1) return false;
            stake = Mathf.Max(1, stake - Step);
            Refresh();
            return true;
        }

        public bool AllIn()
        {
            if (!CanRaise) return false;   // déjà tout misé
            stake = Economy.Money;
            Refresh();
            return true;
        }

        public void ToggleGuarantee()
        {
            Guaranteed = !Guaranteed;
            Refresh();
        }

        // Lance la roue : on paie la mise, la lueur s'éteint, et la mise du lancer suivant repart de 1.
        public bool Spin()
        {
            if (!CanSpin || !Economy.TrySpend(stake, wheel.transform.position)) return false;
            var color = selected.Value;
            int bet = played = stake;
            int result = Draw(color);
            stake = 1;   // la mise suivante repart de 1
            selected = null;
            ShowGlow();
            lastResult = $"Pari : {RouletteRules.Colored(color)}, misé {bet}. La bille tourne...";
            wheel.Spin(result, () => Resolve(color, bet, result));
            Refresh();
            return true;
        }

        // Le numéro tiré : au hasard, ou en « 100 % » un numéro au hasard parmi ceux de la couleur choisie
        int Draw(RouletteColor color)
        {
            if (!Guaranteed) return Random.Range(0, RouletteRules.Numbers);
            var winners = new List<int>();
            for (int n = 0; n < RouletteRules.Numbers; n++)
                if (RouletteRules.ColorOf(n) == color) winners.Add(n);
            return winners[Random.Range(0, winners.Count)];
        }

        void Resolve(RouletteColor color, int bet, int result)
        {
            var landed = RouletteRules.ColorOf(result);
            int won = landed == color ? bet * (RouletteRules.Payout(color) + 1) : 0;   // gain + mise rendue
            Economy.Earn(won, wheel.transform.position + Vector3.up * 0.3f);

            history.Insert(0, landed);
            if (history.Count > HistorySize) history.RemoveAt(HistorySize);
            lastResult = won > 0
                ? $"{RouletteRules.Colored(landed)} : <color=#8CE08C>gagné, +{won} bananes !</color>"
                : $"{RouletteRules.Colored(landed)} : perdu ({bet} sur {RouletteRules.Colored(color)})";
            Refresh();
        }

        // --- Affichage ---

        void ShowGlow()
        {
            foreach (var p in pockets)
                p.glow.gameObject.SetActive(selected.HasValue && p.color == selected.Value);
        }

        // L'ardoise, et les bananes du tapis des mises
        void Refresh()
        {
            int shown = Spinning ? played : stake;
            int piles = Mathf.Clamp((shown + BananasPerPile - 1) / BananasPerPile, 1, stakeBananas.Length);   // arrondi au-dessus
            for (int i = 0; i < stakeBananas.Length; i++) stakeBananas[i].SetActive(i < piles);
            stakeLabel.text = $"<color=#FFD45A>{shown}</color> bananes";

            string pick = selected.HasValue
                ? $"{RouletteRules.Colored(selected.Value)} (paie {RouletteRules.Payout(selected.Value)} contre 1)"
                : "<color=#9AA89A>aucune</color>";
            var text = new System.Text.StringBuilder();
            text.Append($"couleur : {pick}\n");
            text.Append($"mise : <color=#FFD45A>{stake}</color>\n");
            text.Append(lastResult).Append('\n');
            var last = new List<string>();
            foreach (var h in history) last.Add(RouletteRules.Colored(h));
            text.Append($"<size=32>derniers : {(last.Count > 0 ? string.Join(" ", last) : "-")}</size>");
            if (Guaranteed) text.Append("\n<color=#FFD45A>100 % ACTIF (bêta-test)</color>");
            board.text = text.ToString();
        }
    }
}
