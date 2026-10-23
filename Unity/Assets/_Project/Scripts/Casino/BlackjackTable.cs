using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // La borne de blackjack de la bananeraie, façon « Gamble With Your Friends » : une partie en solo contre un singe
    // croupier. Le tour :
    //   1. MISE : on tape le montant sur le terminal (chiffres, 000, C) ou on prend MIN, 1/4, 1/2, MAX de la bourse ;
    //   2. JOUER : la mise est payée, on reçoit 2 cartes, le croupier aussi (la 2e face cachée) ;
    //   3. CARTE (tirer), RESTER, ou DOUBLER (avec 2 cartes : la mise double, une seule carte de plus) ;
    //      au-dessus de 21, la main est perdue (« sautée ») ;
    //   4. le croupier retourne sa carte et tire jusqu'à 16 (il reste à 17) ;
    //   5. gagné = mise × 2, blackjack = mise × 2,5, égalité = mise rendue. La mise tapée reste pour rejouer.
    // En coop, chacun joue à sa borne, chez lui (rien ne passe par le réseau) : la bourse est commune, donc les mises
    // et les gains passent par Economy, qui les envoie à l'hôte comme le reste.
    // Ce script fait les règles ; BlackjackView construit la borne et dessine les cartes.
    public class BlackjackTable : MonoBehaviour
    {
        public static BlackjackTable Instance { get; private set; }

        const float DealDelay = 0.35f;     // s entre deux cartes de la donne
        const float DealerDelay = 0.7f;    // s entre deux cartes du croupier : on a le temps de suivre
        const float ResultsTime = 2.5f;    // s pour voir le résultat avant de pouvoir rejouer
        const int ReshuffleBelow = 15;     // cartes : en dessous, on reprend un paquet neuf mélangé
        const int MaxDigits = 7;           // le terminal affiche au plus 9 999 999

        // Où la borne est posée par rapport à la roulette (dans le repère de la bananeraie) : devant elle, contre la barrière
        // de droite, le croupier côté barrière. Tournée de 90° : le joueur regarde vers la barrière.
        static readonly Vector3 OffsetFromRoulette = new Vector3(1.4f, 0f, -3.8f);

        public BlackjackPhase Phase { get; private set; }
        public BlackjackResult Result { get; private set; }
        public List<byte> Dealer { get; } = new List<byte>();
        public List<byte> Hand { get; } = new List<byte>();
        public bool DealerHidden { get; private set; }
        public int Bet { get; private set; } = 10;       // le montant tapé sur le terminal
        public int Played { get; private set; }          // la mise en jeu pendant la main (doublée par DOUBLER)
        public string Message { get; private set; } = "";

        public bool CanType => Phase == BlackjackPhase.Betting || Phase == BlackjackPhase.Results;
        public bool CanPlay => CanType && Bet >= BlackjackRules.MinBet && Bet <= Economy.Money;
        public bool CanAct => Phase == BlackjackPhase.Playing;
        public bool CanDouble => CanAct && Hand.Count == 2 && Economy.Money >= Played;

        List<byte> deck = new List<byte>();
        BlackjackView view;

        // Appelé par la roulette au lancement : la borne se construit à côté d'elle (pas besoin de régénérer la scène)
        public static void CreateNear(Transform roulette)
        {
            if (Instance) return;
            var root = new GameObject("Blackjack").transform;
            root.SetParent(roulette.parent, false);
            root.localPosition = roulette.localPosition + roulette.localRotation * OffsetFromRoulette;
            root.localRotation = roulette.localRotation * Quaternion.Euler(0f, 90f, 0f);   // +Z local = vers le croupier
            root.gameObject.AddComponent<BlackjackTable>();
        }

        void Awake()
        {
            Instance = this;
            view = BlackjackView.Build(this);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        // --- Le terminal de mise ---

        public bool TypeDigits(string digits)
        {
            if (!CanType) return false;
            string typed = Bet == 0 ? digits.TrimStart('0') : Bet + digits;
            if (typed.Length > MaxDigits) return false;
            Bet = typed.Length == 0 ? 0 : int.Parse(typed);
            Message = "";
            return true;
        }

        public bool ClearBet()
        {
            if (!CanType) return false;
            Bet = 0;
            Message = "";
            return true;
        }

        // MIN, 1/4, 1/2, MAX : une part de la bourse (0 = la mise minimum)
        public bool QuickBet(float share)
        {
            if (!CanType) return false;
            Bet = share <= 0f ? BlackjackRules.MinBet : Mathf.Max(BlackjackRules.MinBet, Mathf.FloorToInt(Economy.Money * share));
            Message = "";
            return true;
        }

        // --- Les gros boutons ---

        public bool Play()
        {
            if (!CanType) return false;
            if (!CanPlay)
            {
                Message = Bet < BlackjackRules.MinBet ? $"Mise trop petite\n(min {BlackjackRules.MinBet})" : "Pas assez\nde bananes !";
                return false;
            }
            if (!Economy.TrySpend(Bet, view.PlayerPosition)) return false;
            StartCoroutine(Deal());
            return true;
        }

        public bool Hit()
        {
            if (!CanAct) return false;
            Hand.Add(Draw());
            int total = BlackjackRules.Total(Hand);
            if (total > BlackjackRules.Target) StartCoroutine(Finish(BlackjackResult.Bust));
            else if (total == BlackjackRules.Target) StartCoroutine(DealerTurn());   // 21 : on ne peut pas faire mieux
            return true;
        }

        public bool Stand()
        {
            if (!CanAct) return false;
            StartCoroutine(DealerTurn());
            return true;
        }

        // DOUBLER : la mise double (payée maintenant), une seule carte de plus, puis c'est au croupier
        public bool Double()
        {
            if (!CanDouble || !Economy.TrySpend(Played, view.PlayerPosition)) return false;
            Played *= 2;
            Hand.Add(Draw());
            if (BlackjackRules.Total(Hand) > BlackjackRules.Target) StartCoroutine(Finish(BlackjackResult.Bust));
            else StartCoroutine(DealerTurn());
            return true;
        }

        // --- La main ---

        IEnumerator Deal()
        {
            Phase = BlackjackPhase.Dealing;
            Result = BlackjackResult.None;
            Message = "";
            Played = Bet;
            Hand.Clear();
            Dealer.Clear();
            DealerHidden = true;
            if (deck.Count < ReshuffleBelow) deck = BlackjackRules.NewDeck();

            // Une carte au joueur, une au croupier, deux fois (la 2e du croupier face cachée)
            for (int round = 0; round < 2; round++)
            {
                Hand.Add(Draw());
                yield return new WaitForSeconds(DealDelay);
                Dealer.Add(Draw());
                yield return new WaitForSeconds(DealDelay);
            }

            // Un blackjack d'entrée (chez l'un ou l'autre) se joue tout de suite
            bool player = BlackjackRules.IsBlackjack(Hand), dealer = BlackjackRules.IsBlackjack(Dealer);
            if (player || dealer)
            {
                DealerHidden = false;
                yield return Finish(player && dealer ? BlackjackResult.Push : player ? BlackjackResult.Blackjack : BlackjackResult.Lost);
                yield break;
            }
            Phase = BlackjackPhase.Playing;
        }

        IEnumerator DealerTurn()
        {
            Phase = BlackjackPhase.Dealer;
            DealerHidden = false;
            yield return new WaitForSeconds(DealerDelay);
            while (BlackjackRules.Total(Dealer) < BlackjackRules.DealerStands)
            {
                Dealer.Add(Draw());
                yield return new WaitForSeconds(DealerDelay);
            }
            int me = BlackjackRules.Total(Hand), dealer = BlackjackRules.Total(Dealer);
            var result = dealer > BlackjackRules.Target || me > dealer ? BlackjackResult.Won
                       : me == dealer ? BlackjackResult.Push : BlackjackResult.Lost;
            yield return Finish(result);
        }

        // Le résultat, et les gains (qui reviennent à la bourse commune, comme une banane vendue)
        IEnumerator Finish(BlackjackResult result)
        {
            Phase = BlackjackPhase.Results;
            Result = result;
            DealerHidden = false;
            int back = BlackjackRules.Payout(result, Played);
            if (back > 0) Economy.Earn(back, view.PlayerPosition + Vector3.up * 0.3f, stakeBack: Mathf.Min(back, Played));
            Message = result switch
            {
                BlackjackResult.Blackjack => $"BLACKJACK ! +{back}",
                BlackjackResult.Won => $"GAGNÉ ! +{back}",
                BlackjackResult.Push => "Égalité :\nmise rendue",
                BlackjackResult.Bust => "Sauté !\nPlus de 21",
                _ => "Perdu...",
            };
            yield return new WaitForSeconds(ResultsTime);
            if (Phase == BlackjackPhase.Results) Phase = BlackjackPhase.Betting;   // on peut rejouer (déjà possible pendant le résultat)
        }

        byte Draw()
        {
            if (deck.Count == 0) deck = BlackjackRules.NewDeck();
            byte card = deck[deck.Count - 1];
            deck.RemoveAt(deck.Count - 1);
            return card;
        }
    }
}
