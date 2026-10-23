using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace SAE
{
    public enum CardSuit { Pique, Coeur, Carreau, Trefle }

    // Les règles du blackjack, toutes au même endroit (comme RouletteRules) :
    //   - une carte tient dans un octet : couleur × 13 + rang (rang 1 = As, 11 = Valet, 12 = Dame, 13 = Roi) ;
    //   - les figures valent 10, l'As vaut 11, ou 1 si on dépasserait 21 (une main « souple ») ;
    //   - blackjack = 21 avec les 2 premières cartes : payé 3 pour 2 ; une main gagnée est payée 1 pour 1 ;
    //   - le croupier tire jusqu'à 16 et reste à 17.
    public static class BlackjackRules
    {
        public const int Target = 21;
        public const int DealerStands = 17;

        public static byte Card(CardSuit suit, int rank) => (byte)((int)suit * 13 + rank - 1);
        public static CardSuit SuitOf(byte card) => (CardSuit)(card / 13);
        public static int RankOf(byte card) => card % 13 + 1;
        public static bool IsRed(byte card) => SuitOf(card) == CardSuit.Coeur || SuitOf(card) == CardSuit.Carreau;

        // Ce qui est écrit dans le coin de la carte (V, D, R : valet, dame, roi, à la française)
        public static string RankName(byte card) => RankOf(card) switch
        {
            1 => "A",
            11 => "V",
            12 => "D",
            13 => "R",
            int n => n.ToString(),
        };

        public static int Total(FixedList32Bytes<byte> hand)
        {
            int total = 0, aces = 0;
            foreach (var card in hand)
            {
                int rank = RankOf(card);
                if (rank == 1) { aces++; total += 11; }
                else total += Mathf.Min(rank, 10);
            }
            while (total > Target && aces > 0) { total -= 10; aces--; }   // un As passe de 11 à 1
            return total;
        }

        public static bool IsBlackjack(FixedList32Bytes<byte> hand) => hand.Length == 2 && Total(hand) == Target;

        // Ce que rend une mise selon le résultat, mise comprise (0 : perdue)
        public static int Payout(SeatStatus result, int bet) => result switch
        {
            SeatStatus.Blackjack => bet + bet * 3 / 2,
            SeatStatus.Won => bet * 2,
            SeatStatus.Push => bet,
            _ => 0,
        };

        // Un paquet de 52 cartes mélangé (mélange de Fisher-Yates)
        public static List<byte> NewDeck()
        {
            var deck = new List<byte>(52);
            for (int suit = 0; suit < 4; suit++)
                for (int rank = 1; rank <= 13; rank++) deck.Add(Card((CardSuit)suit, rank));
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (deck[i], deck[j]) = (deck[j], deck[i]);
            }
            return deck;
        }
    }
}
