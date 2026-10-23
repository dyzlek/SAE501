using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // La table de blackjack de la bananeraie, contre un singe croupier. Quatre places, une par joueur du coop
    // (place n = joueur n ; seul, on joue à la place 1). Le tour de table :
    //   1. MISES : à sa place, +10 / +100 / RETIRER ; puis DISTRIBUER (n'importe quel joueur) ;
    //   2. DONNE : chaque place qui a misé paie sa mise et reçoit 2 cartes ; le croupier en a 2, la 2e face cachée ;
    //   3. JOUEURS : chacun à sa place, CARTE (tirer) ou RESTER. Au-dessus de 21, la main est perdue (« sautée »).
    //      Tout le monde joue en même temps ; après 30 s, les mains encore en jeu restent ;
    //   4. CROUPIER : il retourne sa carte et tire jusqu'à 16 (il reste à 17) ;
    //   5. RÉSULTATS : gagné = mise × 2, blackjack = mise × 2,5, égalité = mise rendue. Puis on remise (les mises restent).
    // En coop, la bourse est commune : on joue ensemble contre le croupier. C'est l'hôte qui fait tourner la table :
    // un invité lui demande d'agir (Coop.SendBlackjack) et reçoit la table entière (State) par CoopGame.
    // Ce script ne fait que les règles du jeu ; BlackjackView construit la table et dessine les cartes.
    public class BlackjackTable : MonoBehaviour
    {
        public static BlackjackTable Instance { get; private set; }

        const float DealDelay = 0.35f;     // s entre deux cartes de la donne
        const float DealerDelay = 0.7f;    // s entre deux cartes du croupier : on a le temps de suivre
        const float ResultsTime = 4f;      // s pour lire les résultats avant les mises suivantes
        const float TurnTimeout = 30f;     // s : après, les mains encore en jeu restent (un joueur parti ne bloque pas la table)
        const int ReshuffleBelow = 15;     // cartes : en dessous, on reprend un paquet neuf mélangé

        // Où la table est posée par rapport à la roulette (dans le repère de la bananeraie) : contre la barrière de droite,
        // devant la roulette, le croupier côté barrière. Tournée de 90° : les joueurs regardent vers la barrière.
        static readonly Vector3 OffsetFromRoulette = new Vector3(1.3f, 0f, -4.2f);

        public BlackjackState State => state;
        BlackjackState state;
        List<byte> deck = new List<byte>();
        float turnStart;
        BlackjackView view;

        // Appelé par la roulette au lancement : la table se construit à côté d'elle (pas besoin de régénérer la scène)
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

        // La place du joueur de ce casque : la sienne en coop, la 1re seul
        public static int LocalSeat => Coop.Role == CoopRole.Solo ? 0 : Coop.LocalId;

        // --- Les boutons (BlackjackView) ---

        // Un bouton de la place 'seat' vient d'être enfoncé. On ne joue qu'à sa place (DISTRIBUER est pour tout le monde).
        public void Press(int seat, BlackjackOp op)
        {
            if (op != BlackjackOp.Deal && seat != LocalSeat)
            {
                Sounds.Play(Sounds.AchatRefuse, view.SeatPosition(seat), 0.6f);   // ce n'est pas notre place
                return;
            }
            if (Coop.IsGuest) Coop.SendBlackjack(op);   // l'hôte joue pour nous ; la table revient par CoopGame
            else Do(seat, op);
        }

        // --- Les règles (seul, ou chez l'hôte) ---

        // Hôte : aussi appelé par CoopPlayer.BlackjackRpc, pour la place de l'invité qui a appuyé
        public void Do(int seat, BlackjackOp op)
        {
            var s = state[seat];
            switch (op)
            {
                case BlackjackOp.AddTen:
                case BlackjackOp.AddHundred:
                    if (state.phase != BlackjackPhase.Betting) return;
                    s.bet = Mathf.Min(s.bet + (op == BlackjackOp.AddTen ? 10 : 100), Economy.Money);
                    break;
                case BlackjackOp.Clear:
                    if (state.phase != BlackjackPhase.Betting) return;
                    s.bet = 0;
                    break;
                case BlackjackOp.Hit:
                    if (state.phase != BlackjackPhase.Playing || s.status != SeatStatus.Playing) return;
                    s.cards.Add(Draw());
                    int total = BlackjackRules.Total(s.cards);
                    if (total > BlackjackRules.Target) s.status = SeatStatus.Bust;
                    else if (total == BlackjackRules.Target) s.status = SeatStatus.Stood;   // 21 : on ne peut pas faire mieux
                    break;
                case BlackjackOp.Stand:
                    if (state.phase != BlackjackPhase.Playing || s.status != SeatStatus.Playing) return;
                    s.status = SeatStatus.Stood;
                    break;
                case BlackjackOp.Deal:
                    if (state.phase == BlackjackPhase.Betting) StartCoroutine(DealRound());
                    return;
            }
            state[seat] = s;
        }

        // Coop, invité : la table vue par l'hôte remplace la nôtre
        public void ApplyFromHost(BlackjackState host) => state = host;

        void Update()
        {
            if (Coop.IsGuest || state.phase != BlackjackPhase.Playing) return;
            bool timeout = Time.time - turnStart > TurnTimeout;
            for (int i = 0; i < BlackjackState.Seats; i++)
            {
                var s = state[i];
                if (s.status != SeatStatus.Playing) continue;
                if (!timeout) return;   // quelqu'un joue encore
                s.status = SeatStatus.Stood;
                state[i] = s;
            }
            StartCoroutine(DealerTurn());   // tout le monde a fini : au croupier
        }

        IEnumerator DealRound()
        {
            // Les mises sont payées maintenant, place par place, avec ce qu'il reste dans la bourse
            bool anyone = false;
            for (int i = 0; i < BlackjackState.Seats; i++)
            {
                var s = state[i];
                s.cards.Clear();
                s.bet = Mathf.Min(s.bet, Economy.Money);
                bool plays = s.bet > 0 && Economy.TrySpend(s.bet, view.SeatPosition(i));
                s.status = plays ? SeatStatus.Playing : SeatStatus.Empty;
                anyone |= plays;
                state[i] = s;
            }
            if (!anyone)
            {
                Sounds.Play(Sounds.AchatRefuse, transform.position);   // personne n'a misé
                yield break;
            }

            state.phase = BlackjackPhase.Dealing;
            state.dealer.Clear();
            state.dealerHidden = true;
            if (deck.Count < ReshuffleBelow) deck = BlackjackRules.NewDeck();

            // Deux tours : une carte à chaque place qui joue, puis une au croupier (la 2e face cachée)
            for (int round = 0; round < 2; round++)
            {
                for (int i = 0; i < BlackjackState.Seats; i++)
                {
                    var s = state[i];
                    if (s.status != SeatStatus.Playing) continue;
                    s.cards.Add(Draw());
                    state[i] = s;
                    yield return new WaitForSeconds(DealDelay);
                }
                state.dealer.Add(Draw());
                yield return new WaitForSeconds(DealDelay);
            }

            // Un blackjack d'entrée ne joue plus ; si le croupier en a un, il le montre tout de suite et la donne est finie
            for (int i = 0; i < BlackjackState.Seats; i++)
            {
                var s = state[i];
                if (s.status == SeatStatus.Playing && BlackjackRules.IsBlackjack(s.cards)) s.status = SeatStatus.Blackjack;
                state[i] = s;
            }
            state.phase = BlackjackPhase.Playing;
            turnStart = Time.time;
            if (BlackjackRules.IsBlackjack(state.dealer))
            {
                for (int i = 0; i < BlackjackState.Seats; i++)
                {
                    var s = state[i];
                    if (s.status == SeatStatus.Playing) s.status = SeatStatus.Stood;
                    state[i] = s;
                }
            }
        }

        IEnumerator DealerTurn()
        {
            state.phase = BlackjackPhase.Dealer;
            state.dealerHidden = false;
            yield return new WaitForSeconds(DealerDelay);

            // Il ne tire que si une main peut encore gagner (toutes sautées : inutile)
            bool someoneStands = false;
            for (int i = 0; i < BlackjackState.Seats; i++)
                someoneStands |= state[i].status == SeatStatus.Stood;
            while (someoneStands && BlackjackRules.Total(state.dealer) < BlackjackRules.DealerStands)
            {
                state.dealer.Add(Draw());
                yield return new WaitForSeconds(DealerDelay);
            }

            // Les résultats, et les gains (la bourse est commune : seul l'hôte, ou le joueur seul, les touche)
            int dealer = BlackjackRules.Total(state.dealer);
            bool dealerBlackjack = BlackjackRules.IsBlackjack(state.dealer);
            for (int i = 0; i < BlackjackState.Seats; i++)
            {
                var s = state[i];
                if (s.status == SeatStatus.Empty) continue;
                s.status = Result(s, dealer, dealerBlackjack);
                int back = BlackjackRules.Payout(s.status, s.bet);
                if (back > 0) Economy.Earn(back, view.SeatPosition(i) + Vector3.up * 0.3f, stakeBack: Mathf.Min(back, s.bet));
                state[i] = s;
            }
            state.phase = BlackjackPhase.Results;
            yield return new WaitForSeconds(ResultsTime);

            // On range les cartes ; les mises restent pour rejouer tout de suite
            state.dealer.Clear();
            for (int i = 0; i < BlackjackState.Seats; i++)
            {
                var s = state[i];
                s.cards.Clear();
                s.status = SeatStatus.Empty;
                state[i] = s;
            }
            state.phase = BlackjackPhase.Betting;
        }

        static SeatStatus Result(BlackjackSeat s, int dealer, bool dealerBlackjack)
        {
            if (s.status == SeatStatus.Bust) return SeatStatus.Lost;
            if (s.status == SeatStatus.Blackjack) return dealerBlackjack ? SeatStatus.Push : SeatStatus.Blackjack;
            if (dealerBlackjack) return SeatStatus.Lost;
            int total = BlackjackRules.Total(s.cards);
            if (dealer > BlackjackRules.Target || total > dealer) return SeatStatus.Won;
            return total == dealer ? SeatStatus.Push : SeatStatus.Lost;
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
