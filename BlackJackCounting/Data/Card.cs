using BlackJackCounting.Enums;
namespace BlackJackCounting.Data
{
    /// <summary>
    /// Represents a single playing card with a suit and rank.
    /// </summary>
    public class Card
    {
        public Suit Suit { get; set; }
        public Rank Rank { get; set; }

        /// <summary>
        /// Initializes a new instance of the Card class with a specific suit and rank.
        /// </summary>
        public Card(Suit suit, Rank rank)
        {
            Suit = suit;
            Rank = rank;
        }

        /// <summary>
        /// Gets the primary value of the card (1 or 11 for Aces, 10 for face cards, and the numeric value for others).
        /// </summary>
        public int PrimaryValue => Rank == Rank.Ace ? 11 : (int)Rank;

        /// <summary>
        /// Gets the secondary value of the card (1 for Aces; this is used for flexibility in hand calculation).
        /// </summary>
        public int SecondaryValue => Rank == Rank.Ace ? 1 : PrimaryValue;

        public override string ToString()
        {
            return $"{Rank} of {Suit}";
        }
    }
}
