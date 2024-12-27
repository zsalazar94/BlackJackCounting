using BlackJackCounting.Data;
using BlackJackCounting.Enums;

namespace BlackJackCounting.Models
{
    /// <summary>
    /// Represents a collection of cards in a player's or dealer's hand.
    /// </summary>
    public class Hand
    {
        public List<Card> Cards { get; private set; }

        public Hand()
        {
            Cards = new List<Card>();
        }

        public void AddCard(Card card)
        {
            Cards.Add(card);
        }

        /// <summary>
        /// Calculates the total value of the hand, adjusting Aces dynamically.
        /// </summary>
        /// <returns>The best possible hand value without exceeding 21.</returns>
        public int CalculateValue()
        {
            int total = 0;
            int aceCount = 0;

            foreach (var card in Cards)
            {
                total += card.PrimaryValue;
                if (card.Rank == Rank.Ace)
                {
                    aceCount++;
                }
            }

            // Adjust for Aces to avoid busting
            while (total > 21 && aceCount > 0)
            {
                total -= 10;
                aceCount--;
            }

            return total;
        }

        public bool IsSoft()
        {
            int total = 0;
            int aceCount = 0;

            foreach (var card in Cards)
            {
                total += card.PrimaryValue;
                if (card.Rank == Rank.Ace)
                {
                    aceCount++;
                }
            }

            // If there's an Ace being counted as 11, it's a soft hand
            return total <= 21 && aceCount > 0;
        }

        public bool IsBust => CalculateValue() > 21;
        public bool HasBlackjack => Cards.Count == 2 && CalculateValue() == 21;
    }
}
