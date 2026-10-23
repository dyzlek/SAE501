using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // La table de roulette de la bananeraie : le pari, la mise, le tirage et les gains. Trois étapes, écrites sur l'ardoise
    // (celle à faire est en doré) :
    //   1. choisir sa couleur sur le tapis : trois grandes zones ROUGE ×2, NOIR ×2, VERT ×36 (RouletteBetZone).
    //      La zone choisie s'entoure d'un cadre doré, et les cases de cette couleur s'allument sur la roue ;
    //   2. miser avec les jetons du pupitre : +1, +10, +100, TOUT, RETIRER (remet à 0). La mise s'écrit au-dessus de la zone,
    //      avec ce qu'elle peut rapporter (« mise 20 · gain 40 »), et des bananes viennent s'y poser (une par tranche de 25, 6 au plus) ;
    //   3. faire tourner la roue À LA MAIN (RouletteWheel, WheelGrab) : au lâcher, la mise est payée et le numéro tiré.
    // Après un lancer, la couleur et la mise restent (dans la limite de ce qu'on a) : on peut rejouer tout de suite.
    // Coop (Coop) : une seule table pour tous, et la bourse est commune : on joue ensemble contre le casino.
    // C'est l'hôte qui la fait tourner : chez un invité, chaque appui (couleur, jeton, lancer) est demandé à l'hôte,
    // la couleur et la mise reviennent par CoopGame (ApplyFromHost), et le lancer tiré par l'hôte est rejoué chez tout le
    // monde (SpinFromHost) : la même roue, le même numéro. Seul l'hôte paie la mise et les gains.
    // La borne de blackjack,à côté, se construit au lancement (BlackjackTable.CreateNear).
    public class RouletteTable : MonoBehaviour
    {
        public static RouletteTable Instance { get; private set; }

        public RouletteWheel wheel;
        public TextMesh board;              // l'ardoise au-dessus du pupitre
        public GameObject[] stakeBananas;   // les bananes misées, posées sur la zone choisie (6)
        public TextMesh stakeLabel;         // la mise et le gain possible, au-dessus de la zone choisie

        public const int BananasPerPile = 25;   // 1 à 25 misées : 1 banane sur le tapis, 26 à 50 : 2, etc.
        const int HistorySize = 8;          // dernières couleurs sorties, affichées sur l'ardoise
        static readonly Color GlowColor = new Color(1f, 0.85f, 0.2f);
        const string Gold = "#FFD45A", Done = "#8CE08C", Waiting = "#7A847A";

        RoulettePocket[] pockets;           // dans l'ordre de la roue : pockets[i] = la case Case_i (RouletteRules.WheelOrder[i])
        RoulettePocket flashing;            // la case gagnante qui clignote (null : aucune)
        RouletteBetZone[] zones;
        RouletteColor? selected;            // la couleur choisie (null = aucune)
        int stake;
        int played;                         // la mise en jeu pendant que la roue tourne
        readonly List<RouletteColor> history = new List<RouletteColor>();
        string lastResult = "";

        public bool Spinning => wheel.Spinning;
        public int Stake => Mathf.Min(stake, Economy.Money);
        public bool CanRaise => !Spinning && stake < Economy.Money;
        public bool CanClear => !Spinning && stake > 0;

        // Coop : ce que l'hôte envoie aux invités (CoopGame) : la couleur choisie (-1 : aucune) et la mise
        public int SelectedIndex => selected.HasValue ? (int)selected.Value : -1;
        public int RawStake => stake;

        void Awake()
        {
            Instance = this;
            pockets = GetComponentsInChildren<RoulettePocket>();
            System.Array.Sort(pockets, (a, b) => string.CompareOrdinal(a.name, b.name));   // Case_00, Case_01... : l'ordre de la roue
            zones = GetComponentsInChildren<RouletteBetZone>();
        }

        // Le compte change (bananes déposées, achat...) : le tapis et l'ardoise suivent
        void OnEnable() => Economy.MoneyChanged += OnMoneyChanged;
        void OnDisable() => Economy.MoneyChanged -= OnMoneyChanged;
        void OnMoneyChanged(int money, Vector3? where) => Refresh();

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            ShowGlow();
            Refresh();
            BlackjackTable.CreateNear(transform);
        }

        // Les lueurs allumées (cadre de la zone, cases de la roue) pulsent doucement pour attirer l'œil
        void Update()
        {
            if (!selected.HasValue) return;
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 6f);
            foreach (var p in pockets)
                if (p.color == selected.Value && p != flashing) p.Highlight(GlowColor, 0.35f + 0.3f * pulse);   // la case garde un peu de sa couleur
            foreach (var z in zones) if (z.color == selected.Value) z.glow.Set(GlowColor * pulse);
        }

        // --- Le tapis (RouletteBetZone) et les jetons (RouletteButton) ---

        // Choisir une couleur : sa zone et ses cases s'allument et remplacent l'ancien choix. Pas pendant que la roue tourne.
        public void Select(RouletteColor color)
        {
            if (Spinning) return;
            if (Coop.IsGuest) { Coop.SendRoulette(RouletteOp.Select, color, 0f); return; }
            selected = color;
            ShowGlow();
            Refresh();
        }

        public bool Add(int amount)
        {
            if (!CanRaise) return false;
            if (Coop.IsGuest) return Ask(amount == 1 ? RouletteOp.AddOne : amount == 10 ? RouletteOp.AddTen : RouletteOp.AddHundred);
            stake = Mathf.Min(stake + amount, Economy.Money);
            Refresh();
            return true;
        }

        public bool AllIn()
        {
            if (!CanRaise) return false;   // déjà tout misé
            if (Coop.IsGuest) return Ask(RouletteOp.AllIn);
            stake = Economy.Money;
            Refresh();
            return true;
        }

        public bool Clear()
        {
            if (!CanClear) return false;
            if (Coop.IsGuest) return Ask(RouletteOp.Clear);
            stake = 0;
            Refresh();
            return true;
        }

        // --- Le lancer (au lâcher de la roue) ---

        // La roue vient d'être lancée à la main à 'speed' °/s. Pari valide : on paie la mise, on tire le numéro et la roue
        // le montre. Sinon (pas de couleur, pas de mise) : la roue tourne pour rien et l'ardoise dit ce qui manque.
        public bool Launch(float speed)
        {
            if (Spinning) return false;
            int bet = Stake;
            if (!selected.HasValue || bet <= 0)
            {
                lastResult = !selected.HasValue ? "<color=#FF7A6A>Choisis d'abord une couleur sur le tapis !</color>"
                                                : "<color=#FF7A6A>Mise d'abord des bananes avec les jetons !</color>";
                Sounds.Play(Sounds.AchatRefuse, wheel.transform.position);
                Refresh();
                return false;
            }
            // Coop, invité : c'est l'hôte qui paie et tire le numéro ; son lancer nous reviendra (SpinFromHost)
            if (Coop.IsGuest) return Ask(RouletteOp.Spin, speed);
            if (!Economy.TrySpend(bet, wheel.transform.position)) return false;
            var color = selected.Value;
            int result = Random.Range(0, RouletteRules.Numbers);
            Coop.ShareRouletteSpin(result, speed, color, bet);   // hôte : les invités voient le même lancer
            Play(result, speed, color, bet, pay: true);
            return true;
        }

        // Coop, invité : l'hôte vient de lancer la roue (lui ou un autre joueur) : on rejoue le même lancer, sans payer
        public void SpinFromHost(int result, float speed, RouletteColor color, int bet)
        {
            selected = color;
            Play(result, speed, color, bet, pay: false);
        }

        void Play(int result, float speed, RouletteColor color, int bet, bool pay)
        {
            played = bet;
            lastResult = $"La bille tourne... (misé {bet} sur {RouletteRules.Colored(color)})";
            wheel.Spin(result, speed, () => { Resolve(color, bet, result, pay); StartCoroutine(FlashWinner(result)); });
            Refresh();
        }

        // Coop, invité : la couleur et la mise de la table, vues par l'hôte (CoopGame)
        public void ApplyFromHost(int colorIndex, int hostStake)
        {
            var color = colorIndex < 0 ? (RouletteColor?)null : (RouletteColor)colorIndex;
            if (color == selected && hostStake == stake) return;
            if (color != selected && !Spinning) { selected = color; ShowGlow(); }
            stake = hostStake;
            Refresh();
        }

        // Coop, invité : la demande part chez l'hôte ; le bouton s'enfonce sans flash rouge
        static bool Ask(RouletteOp op, float speed = 0f)
        {
            Coop.SendRoulette(op, default, speed);
            return true;
        }

        void Resolve(RouletteColor color, int bet, int result, bool pay)
        {
            var landed = RouletteRules.ColorOf(result);
            int won = landed == color ? bet * (RouletteRules.Payout(color) + 1) : 0;   // gain + mise rendue
            // Seul l'hôte (ou le joueur seul) touche les gains : la bourse est commune, elle revient aux invités par CoopGame.
            // Les trophées ne comptent que le gain, pas la mise rendue.
            if (pay) Economy.Earn(won, wheel.transform.position + Vector3.up * 0.3f, stakeBack: won > 0 ? bet : 0);
            // Gagné : le son du singe obtenu, ou le jingle de victoire pour le vert (×35) ; perdu : le son de vie perdue, bien audible
            var at = wheel.transform.position;
            if (won == 0) Sounds.Play(Sounds.ViePerdue, at, 1f, 0.9f);
            else Sounds.Play(color == RouletteColor.Vert ? Sounds.VagueGagnee : Sounds.SingeObtenu, at);

            history.Insert(0, landed);
            if (history.Count > HistorySize) history.RemoveAt(HistorySize);
            lastResult = won > 0
                ? $"{RouletteRules.Colored(landed)} : <color={Done}>GAGNÉ, +{won} bananes !</color>"
                : $"{RouletteRules.Colored(landed)} : perdu...";
            Refresh();
        }

        // --- Affichage ---

        // La case gagnante clignote en blanc pendant 2,5 s, sous la bille ; puis les lueurs de la couleur choisie reviennent
        IEnumerator FlashWinner(int result)
        {
            int index = System.Array.IndexOf(RouletteRules.WheelOrder, result);
            if (index < 0 || index >= pockets.Length) yield break;
            flashing = pockets[index];   // les cases sont rangées dans l'ordre de la roue (RouletteRules.WheelOrder)
            for (float t = 0f; t < 2.5f && !Spinning; t += Time.deltaTime)
            {
                flashing.Highlight(Color.white, Mathf.Repeat(t * 4f, 1f) < 0.6f ? 1f : 0f);
                yield return null;
            }
            flashing = null;
            ShowGlow();
        }

        // Au repos, chaque case reprend sa couleur ; la zone choisie montre son cadre doré (les cases s'allument dans Update)
        void ShowGlow()
        {
            foreach (var p in pockets) p.Rest();
            foreach (var z in zones) z.glow.gameObject.SetActive(selected.HasValue && z.color == selected.Value);
        }

        RouletteBetZone SelectedZone()
        {
            if (selected.HasValue) foreach (var z in zones) if (z.color == selected.Value) return z;
            return null;
        }

        // L'ardoise en 3 étapes, les bananes misées et la mise écrite au-dessus de la zone choisie
        void Refresh()
        {
            int shown = Spinning ? played : Stake;   // on ne montre jamais plus que ce qu'on a
            PlaceBananas(shown);

            var zone = SelectedZone();
            int gain = selected.HasValue ? shown * (RouletteRules.Payout(selected.Value) + 1) : 0;
            stakeLabel.text = shown > 0 && zone ? $"mise <color={Gold}>{shown}</color> · gain {gain}" : "";
            if (zone) stakeLabel.transform.position = zone.transform.position + Vector3.up * 0.32f;   // assez haut pour ne cacher aucune inscription

            bool colorDone = selected.HasValue, stakeDone = shown > 0;
            string colorText = colorDone ? $"{RouletteRules.Colored(selected.Value)} (×{RouletteRules.Payout(selected.Value) + 1})" : "touche une zone du tapis";
            string stakeText = stakeDone ? $"{shown} {(shown > 1 ? "bananes" : "banane")}" + (colorDone ? $", gain possible : {gain}" : "") : "les jetons du pupitre";

            var text = new System.Text.StringBuilder();
            text.Append(Step(1, "Couleur : " + colorText, colorDone, !colorDone)).Append('\n');
            text.Append(Step(2, "Mise : " + stakeText, stakeDone, colorDone && !stakeDone)).Append('\n');
            text.Append(Step(3, Spinning ? "La roue tourne !" : "Attrape la roue et fais-la tourner !", false, colorDone && stakeDone && !Spinning)).Append('\n');
            if (lastResult.Length > 0) text.Append(lastResult).Append('\n');
            var last = new List<string>();
            foreach (var h in history) last.Add(RouletteRules.Colored(h));
            text.Append($"<size=32>derniers : {(last.Count > 0 ? string.Join(" ", last) : "-")}</size>");
            board.text = text.ToString();
        }

        // Une étape de l'ardoise : dorée et marquée d'une flèche si c'est la prochaine à faire, verte si elle est faite
        static string Step(int number, string text, bool done, bool current)
        {
            string color = current ? Gold : done ? Done : Waiting;
            return $"<color={color}>{(current ? "> " : "")}{number}. {text}</color>";
        }

        // Les bananes misées : sur la zone choisie, en deux rangées de trois (aucune tant qu'on n'a pas choisi de couleur)
        void PlaceBananas(int amount)
        {
            int piles = Mathf.Clamp((amount + BananasPerPile - 1) / BananasPerPile, 0, stakeBananas.Length);   // arrondi au-dessus
            var zone = SelectedZone();
            for (int i = 0; i < stakeBananas.Length; i++)
            {
                stakeBananas[i].SetActive(zone && i < piles);
                if (!zone) continue;
                // Devant l'inscription de la zone (côté joueur, -Z), pour ne pas la cacher
                var offset = new Vector3((i % 3 - 1) * 0.12f, 0.025f, -0.1f + (i / 3) * 0.06f);   // une rangée au bord avant, la 2e juste derrière
                stakeBananas[i].transform.position = zone.transform.position + transform.rotation * offset;
            }
        }
    }
}
