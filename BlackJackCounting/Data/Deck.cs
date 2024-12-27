using BlackJackCounting.Enums;

namespace BlackJackCounting.Data
{
    public class Deck
    {
        private Stack<Card> cards;
        public Deck()
        {
            cards = new Stack<Card>(GenerateDeck());
            Shuffle();
        }

        private IEnumerable<Card> GenerateDeck()
        {
            for (int i = 0; i < 6; i++)
            { 
                foreach (Suit suit in Enum.GetValues(typeof(Suit)))
                {
                    foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                    {
                        yield return new Card(suit, rank);
                    }
                }
            }
        }

        public void Shuffle()
        {
            var random = new Random();
            cards = new Stack<Card>(cards.OrderBy(_ => random.Next()));
        }

        public Card Deal()
        {
            if (cards.Any())
            {
                return cards.Pop();
            }
            throw new InvalidOperationException("No more cards in the deck!");
        }

        public int CardsRemaining => cards.Count;
    }
}
