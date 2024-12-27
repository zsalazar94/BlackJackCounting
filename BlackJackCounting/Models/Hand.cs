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
            int totalValue = Cards.Sum(card => card.PrimaryValue);
            int aceCount = Cards.Count(card => card.Rank == Rank.Ace);

            while (totalValue > 21 && aceCount > 0)
            {
                totalValue -= 10; // Adjust one Ace from 11 to 1
                aceCount--;
            }

            return totalValue;
        }

        public bool IsBust => CalculateValue() > 21;
        public bool HasBlackjack => Cards.Count == 2 && CalculateValue() == 21;
    }
}
