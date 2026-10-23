using System;
using Unity.Collections;
using Unity.Netcode;

namespace SAE
{
    // Le tour de table : les mises, la donne (2 cartes chacun), les joueurs, le croupier, puis les résultats
    public enum BlackjackPhase : byte { Betting, Dealing, Playing, Dealer, Results }

    // Où en est une place : Empty = pas de mise dans cette donne. Les quatre derniers sont les résultats.
    public enum SeatStatus : byte { Empty, Playing, Stood, Bust, Blackjack, Won, Lost, Push }

    // Ce qu'on peut faire à sa place : miser (pendant les mises), tirer une carte ou rester (pendant son tour),
    // et lancer la donne (DISTRIBUER, pour toute la table).
    public enum BlackjackOp : byte { AddTen, AddHundred, Clear, Hit, Stand, Deal }

    // Une place à la table : sa mise, ses cartes (une carte = un octet, voir BlackjackRules) et où elle en est
    public struct BlackjackSeat : IEquatable<BlackjackSeat>
    {
        public int bet;
        public SeatStatus status;
        public FixedList32Bytes<byte> cards;

        public bool Equals(BlackjackSeat o) => bet == o.bet && status == o.status && cards.Equals(o.cards);
    }

    // Toute la table de blackjack en un seul bloc : la phase, les cartes du croupier (la 2e cachée tant qu'il n'a pas joué)
    // et les 4 places (une par joueur du coop : place n = joueur n). BlackjackTable le fait évoluer, BlackjackView le dessine.
    // En coop, l'hôte l'envoie tel quel dans une NetworkVariable (CoopGame) : INetworkSerializeByMemcpy, car il n'y a que
    // des nombres dedans (FixedList : une liste de taille fixe, sans allocation).
    public struct BlackjackState : INetworkSerializeByMemcpy, IEquatable<BlackjackState>
    {
        public const int Seats = 4;

        public BlackjackPhase phase;
        public FixedList32Bytes<byte> dealer;
        public bool dealerHidden;
        public BlackjackSeat seat0, seat1, seat2, seat3;

        public BlackjackSeat this[int i]
        {
            get => i switch { 0 => seat0, 1 => seat1, 2 => seat2, _ => seat3 };
            set
            {
                switch (i)
                {
                    case 0: seat0 = value; break;
                    case 1: seat1 = value; break;
                    case 2: seat2 = value; break;
                    default: seat3 = value; break;
                }
            }
        }

        public bool Equals(BlackjackState o) =>
            phase == o.phase && dealerHidden == o.dealerHidden && dealer.Equals(o.dealer)
            && seat0.Equals(o.seat0) && seat1.Equals(o.seat1) && seat2.Equals(o.seat2) && seat3.Equals(o.seat3);
    }
}
