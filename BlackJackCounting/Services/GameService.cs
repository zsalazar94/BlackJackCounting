using BlackJackCounting.Data;
using BlackJackCounting.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BlackJackCounting.Services
{
    public class GameService
    {
        public Deck Deck { get; private set; }
        public List<Hand> PlayerHands { get; private set; }
        public Hand DealerHand { get; private set; }
        public int CurrentHandIndex { get; private set; }
        public bool IsGameOver { get; private set; }
        public string GameResult { get; private set; }

        // Betting System
        public double PlayerCash { get; private set; } = 1000.00; // Starting cash
        public double CurrentBet { get; private set; }
        public List<double> Bets { get; private set; } // Bets for each hand


        public GameService()
        {
            StartNewGame();
        }

        public void StartNewGame()
        {
            ReplenishDeckIfNecessary(true); // Check and replenish for a new game
            PlayerHands = new List<Hand> { new Hand() };
            DealerHand = new Hand();
            Bets = new List<double> { 0 };
            CurrentHandIndex = 0;
            IsGameOver = false;
            GameResult = string.Empty;
            CurrentBet = 0;
        }

        public void StartGameAfterBet()
        {
            if (CurrentBet <= 0)
            {
                throw new InvalidOperationException("You must place a bet before starting the game.");
            }

            DealInitialCards();
        }

        private void DealInitialCards()
        {
            ReplenishDeckIfNecessary(false); // Ensure the deck is not empty before dealing

            PlayerHands[0].AddCard(Deck.Deal());
            DealerHand.AddCard(Deck.Deal());
            PlayerHands[0].AddCard(Deck.Deal());
            DealerHand.AddCard(Deck.Deal());

            if (PlayerHands[0].HasBlackjack)
            {
                GameResult = "Blackjack! You Win!";
                ResolveBets();
                IsGameOver = true;
            }
        }

        public bool CanSplit()
        {
            if (CurrentHandIndex >= PlayerHands.Count) return false;
            var hand = PlayerHands[CurrentHandIndex];
            return hand.Cards.Count == 2 && hand.Cards[0].Rank == hand.Cards[1].Rank && PlayerHands.Count < 4;
        }

        public void Split()
        {
            if (!CanSplit()) return;

            ReplenishDeckIfNecessary(false); // Check and replenish mid-game if necessary

            var currentHand = PlayerHands[CurrentHandIndex];
            var newHand = new Hand();

            newHand.AddCard(currentHand.Cards[1]);
            currentHand.Cards.RemoveAt(1);

            currentHand.AddCard(Deck.Deal());
            newHand.AddCard(Deck.Deal());

            PlayerHands.Add(newHand);
            Bets.Add(Bets[CurrentHandIndex]); // Duplicate the bet
            PlayerCash -= Bets[CurrentHandIndex]; // Deduct the additional bet
        }

        public void DoubleDown()
        {
            if (IsGameOver || CurrentHandIndex >= PlayerHands.Count) return;

            var currentHand = PlayerHands[CurrentHandIndex];

            // Double the bet for the current hand
            PlayerCash -= Bets[CurrentHandIndex];
            Bets[CurrentHandIndex] *= 2;

            // Add one card to the current hand
            currentHand.AddCard(Deck.Deal());

            // Automatically stand after doubling down
            if (CurrentHandIndex < PlayerHands.Count - 1)
            {
                CurrentHandIndex++; // Move to the next hand if there are more
            }
            else
            {
                DealerPlays(); // Dealer plays after all hands are done
            }
        }

        public void PlayerHits()
        {
            if (IsGameOver) return;

            ReplenishDeckIfNecessary(false); // Check and replenish mid-game if necessary

            var currentHand = PlayerHands[CurrentHandIndex];
            currentHand.AddCard(Deck.Deal());

            if (currentHand.IsBust && CurrentHandIndex < PlayerHands.Count - 1)
            {
                CurrentHandIndex++; // Move to the next hand
            }
            else if (currentHand.IsBust && CurrentHandIndex == PlayerHands.Count - 1)
            {
                DealerPlays(); // Dealer plays if it’s the last hand
            }
        }

        public void PlayerStands()
        {
            if (IsGameOver) return;

            if (CurrentHandIndex < PlayerHands.Count - 1)
            {
                // Move to the next hand
                CurrentHandIndex++;
            }
            else
            {
                // Dealer plays when all player hands are finished
                DealerPlays();
            }
        }

        private void DealerPlays()
        {
            while (DealerHand.CalculateValue() < 17)
            {
                DealerHand.AddCard(Deck.Deal());
            }

            DetermineWinner();
            IsGameOver = true;
        }

        private void DetermineWinner()
        {
            foreach (var hand in PlayerHands)
            {
                int playerTotal = hand.CalculateValue();
                int dealerTotal = DealerHand.CalculateValue();

                if (hand.IsBust)
                {
                    GameResult += $"Hand {PlayerHands.IndexOf(hand) + 1}: Bust!\n";
                }
                else if (DealerHand.IsBust || playerTotal > dealerTotal)
                {
                    GameResult += $"Hand {PlayerHands.IndexOf(hand) + 1}: You Win!\n";
                }
                else if (playerTotal < dealerTotal)
                {
                    GameResult += $"Hand {PlayerHands.IndexOf(hand) + 1}: Dealer Wins!\n";
                }
                else
                {
                    GameResult += $"Hand {PlayerHands.IndexOf(hand) + 1}: Tie!\n";
                }
            }
            ResolveBets();
            IsGameOver = true;
        }

        public void PlaceBet(double amount)
        {
            if (amount <= 0 || amount > PlayerCash)
            {
                throw new InvalidOperationException("Invalid bet amount.");
            }

            CurrentBet = Math.Round(amount, 2); // Round to 2 decimal places
            PlayerCash = Math.Round(PlayerCash - amount, 2); // Deduct bet and round to 2 decimals
            Bets[0] = CurrentBet; // Set the bet for the main hand
            StartGameAfterBet();
        }

        public void ResolveBets()
        {
            for (int i = 0; i < PlayerHands.Count; i++)
            {
                var hand = PlayerHands[i];
                int playerTotal = hand.CalculateValue();
                int dealerTotal = DealerHand.CalculateValue();

                if (hand.HasBlackjack && hand.Cards.Count == 2) // Blackjack condition
                {
                    if (dealerTotal == 21 && DealerHand.Cards.Count == 2)
                    {
                        // Both player and dealer have blackjack (tie)
                        PlayerCash = Math.Round(PlayerCash + Bets[i], 2); // Refund the bet
                    }
                    else
                    {
                        // Player has blackjack and wins 3:2
                        PlayerCash = Math.Round(PlayerCash + Bets[i] * 2.5, 2); // Bet + 1.5x bet
                    }
                }
                else if (hand.IsBust)
                {
                    // Player loses the bet on this hand
                }
                else if (DealerHand.IsBust || playerTotal > dealerTotal)
                {
                    // Player wins 2x the bet
                    PlayerCash = Math.Round(PlayerCash + Bets[i] * 2, 2);
                }
                else if (playerTotal == dealerTotal)
                {
                    // Tie: Refund the bet
                    PlayerCash = Math.Round(PlayerCash + Bets[i], 2);
                }
                else
                {
                    // Player loses the bet
                }
            }

            CurrentBet = 0; // Reset the bet after resolution
        }

        private void ReplenishDeckIfNecessary(bool isNewGame)
        {
            if (Deck == null || (isNewGame && Deck.CardsRemaining < 15))
            {
                Deck = new Deck(); // Replenish the deck for a new game
            }
            else if (!isNewGame && Deck.CardsRemaining == 0)
            {
                Deck = new Deck(); // Replenish the deck during the game only if it's empty
            }
        }
    }
}
