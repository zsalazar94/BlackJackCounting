﻿using BlackJackCounting.Data;
using BlackJackCounting.Enums;
using BlackJackCounting.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Runtime.CompilerServices;

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
        public double PlayerCash { get; private set; } = 10000.00; // Starting cash
        public double CurrentBet { get; private set; }
        public List<double> Bets { get; private set; } // Bets for each hand
        public int RunningCount { get; private set; } = 0; // Running count for card counting
        public int TotalDecks { get; private set; } = 6; // Number of decks used in the game


        public GameService()
        {
            StartNewGame();
        }

        public void StartNewGame()
        {
            LogMethodStart();
            ReplenishDeckIfNecessary(true); // Check and replenish for a new game
            PlayerHands = new List<Hand> { new Hand() };
            DealerHand = new Hand();
            Bets = new List<double> { 0 };
            CurrentHandIndex = 0;
            IsGameOver = false;
            GameResult = string.Empty;
            CurrentBet = 0;
        }

        public void LogMethodStart([CallerMemberName] string methodName = "")
        {
            //Console.WriteLine($"Starting {methodName}");
        }

        public void StartGameAfterBet()
        {
            LogMethodStart();

            if (CurrentBet <= 0)
            {
                throw new InvalidOperationException("You must place a bet before starting the game.");
            }

            DealInitialCards();
        }

        private void DealInitialCards()
        {
            LogMethodStart();

            ReplenishDeckIfNecessary(false); // Ensure the deck is not empty before dealing

            PlayerHands[0].AddCard(Deck.Deal());
            UpdateRunningCount(PlayerHands[0].Cards[^1]); // Update count

            DealerHand.AddCard(Deck.Deal());
            UpdateRunningCount(DealerHand.Cards[^1]); // Update count

            PlayerHands[0].AddCard(Deck.Deal());
            UpdateRunningCount(PlayerHands[0].Cards[^1]); // Update count

            DealerHand.AddCard(Deck.Deal());
            UpdateRunningCount(DealerHand.Cards[^1]); // Update count

            if (PlayerHands[0].HasBlackjack)
            {
                GameResult = "Blackjack! You Win!";
                ResolveBets();
                IsGameOver = true;
            }
        }

        public bool CanSplit()
        {
            LogMethodStart();

            if (CurrentHandIndex >= PlayerHands.Count) return false;
            var hand = PlayerHands[CurrentHandIndex];
            return hand.Cards.Count == 2 && hand.Cards[0].Rank == hand.Cards[1].Rank && PlayerHands.Count < 4;
        }

        public void Split()
        {
            LogMethodStart();

            if (!CanSplit()) return;

            var currentHand = PlayerHands[CurrentHandIndex];
            var newHand = new Hand();

            // Move the second card from the current hand to the new hand
            newHand.AddCard(currentHand.Cards[1]);
            UpdateRunningCount(currentHand.Cards[1]); // Update count for the moved card
            currentHand.Cards.RemoveAt(1);

            // Deal a card to the current hand
            var dealtCard = Deck.Deal();
            currentHand.AddCard(dealtCard);
            UpdateRunningCount(dealtCard); // Update count for the dealt card

            // Add the new hand to the list of player hands
            PlayerHands.Add(newHand);
            Bets.Add(Bets[CurrentHandIndex]); // Duplicate the bet for the new hand
            PlayerCash -= Bets[CurrentHandIndex]; // Deduct the additional bet
        }

        public void DoubleDown()
        {
            LogMethodStart();

            if (IsGameOver || CurrentHandIndex >= PlayerHands.Count) return;

            var currentHand = PlayerHands[CurrentHandIndex];

            // Double the bet for the current hand
            PlayerCash -= Bets[CurrentHandIndex];
            Bets[CurrentHandIndex] *= 2;

            // Add one card to the current hand
            var dealtCard = Deck.Deal();
            currentHand.AddCard(dealtCard);
            UpdateRunningCount(dealtCard); // Update the running count for the dealt card

            // Automatically stand after doubling down
            if (currentHand.IsBust || CurrentHandIndex == PlayerHands.Count - 1)
            {
                DealerPlays(); // If it's the last hand, dealer plays
            }
            else
            {
                CurrentHandIndex++; // Move to the next hand
                if (PlayerHands[CurrentHandIndex].Cards.Count == 1)
                {
                    dealtCard = Deck.Deal();
                    PlayerHands[CurrentHandIndex].AddCard(dealtCard);
                    UpdateRunningCount(dealtCard);
                }
            }
        }

        public void PlayerHits()
        {
            LogMethodStart();

            if (IsGameOver) return;

            ReplenishDeckIfNecessary(false); // Check and replenish mid-game if necessary

            var currentHand = PlayerHands[CurrentHandIndex];
            var dealtCard = Deck.Deal();
            currentHand.AddCard(dealtCard);

            UpdateRunningCount(dealtCard); // Update the running count for the dealt card

            if (currentHand.CalculateValue() == 21)
            {
                // Automatically stand on 21
                if (CurrentHandIndex < PlayerHands.Count - 1)
                {
                    CurrentHandIndex++;
                    if (PlayerHands[CurrentHandIndex].Cards.Count == 1)
                    {
                        dealtCard = Deck.Deal();
                        PlayerHands[CurrentHandIndex].AddCard(dealtCard);
                        UpdateRunningCount(dealtCard);
                    }
                }
                else
                {
                    DealerPlays(); // Dealer plays if it's the last hand
                }
            }
            else if (currentHand.IsBust)
            {
                // Transition to the next hand if the current hand busts
                if (CurrentHandIndex < PlayerHands.Count - 1)
                {
                    CurrentHandIndex++;
                    if (PlayerHands[CurrentHandIndex].Cards.Count == 1)
                    {
                        dealtCard = Deck.Deal();
                        PlayerHands[CurrentHandIndex].AddCard(dealtCard);
                        UpdateRunningCount(dealtCard);
                    }
                }
                else
                {
                    DealerPlays(); // If it's the last hand, let the dealer play
                }
            }
        }

        public void PlayerStands()
        {
            LogMethodStart();

            if (IsGameOver) return;

            if (CurrentHandIndex < PlayerHands.Count - 1)
            {
                // Move to the next hand
                CurrentHandIndex++;

                // Ensure the next hand has two cards
                if (PlayerHands[CurrentHandIndex].Cards.Count == 1)
                {
                    var dealtCard = Deck.Deal();
                    PlayerHands[CurrentHandIndex].AddCard(dealtCard);
                    UpdateRunningCount(dealtCard);
                }
            }
            else
            {
                // Dealer plays when all player hands are finished
                DealerPlays();
            }
        }

        public async Task PlayerStandsAsync(Func<Task> updateUI)
        {
            LogMethodStart();

            if (IsGameOver) return;

            if (CurrentHandIndex < PlayerHands.Count - 1)
            {
                // Move to the next hand
                CurrentHandIndex++;

                // If the next hand has only one card, deal the second card
                if (PlayerHands[CurrentHandIndex].Cards.Count == 1)
                {
                    PlayerHands[CurrentHandIndex].AddCard(Deck.Deal());
                }
            }
            else
            {
                // Trigger asynchronous dealer play
                await DealerPlaysAsync(updateUI);
            }

            await updateUI(); // Ensure the UI reflects the changes
        }


        public async Task DealerPlaysAsync(Func<Task> updateUI)
        {
            LogMethodStart();

            // Flip the dealer's face-down card
            await updateUI(); // Update UI to show the flipped card
            await Task.Delay(2000); // Add a 2-second delay

            // Dealer draws cards until the total is at least 17
            while (ShouldDealerHit())
            {
                var dealtCard = Deck.Deal();
                DealerHand.AddCard(dealtCard);

                // Update running count for the dealt card
                UpdateRunningCount(dealtCard);

                // Delay between card draws
                await updateUI();
                await Task.Delay(2000);
            }

            // Final UI update
            await updateUI();
            DetermineWinner();
            IsGameOver = true;
        }

        private void DealerPlays()
        {
            LogMethodStart();

            while (ShouldDealerHit())
            {
                var dealtCard = Deck.Deal();
                DealerHand.AddCard(dealtCard);

                UpdateRunningCount(dealtCard); // Update the running count for the dealt card
            }

            DetermineWinner();
            IsGameOver = true;
        }

        private bool ShouldDealerHit()
        {
            LogMethodStart();

            int dealerValue = DealerHand.CalculateValue();
            return dealerValue < 17 || (dealerValue == 17 && DealerHand.IsSoft());
        }

        private void DetermineWinner()
        {
            LogMethodStart();

            foreach (var hand in PlayerHands)
            {
                int playerTotal = hand.CalculateValue();
                int dealerTotal = DealerHand.CalculateValue();
                /*
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
                }*/
            }
            ResolveBets();
            IsGameOver = true;
        }

        public void PlaceBet(double amount)
        {
            LogMethodStart();

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
            LogMethodStart();

            for (int i = 0; i < PlayerHands.Count; i++)
            {
                var hand = PlayerHands[i];
                int playerTotal = hand.CalculateValue();
                int dealerTotal = DealerHand.CalculateValue();
                double winnings = 0;

                if (hand.HasBlackjack && hand.Cards.Count == 2) // Blackjack condition
                {
                    if (dealerTotal == 21 && DealerHand.Cards.Count == 2)
                    {
                        // Both player and dealer have blackjack (tie)
                        PlayerCash += Bets[i]; // Refund the bet
                        GameResult += $"Hand {i + 1}: Tie! Bet refunded.\n";
                    }
                    else
                    {
                        // Player has blackjack and wins 3:2
                        winnings = Bets[i] * 1.5;
                        PlayerCash += Bets[i] + winnings;
                        GameResult += $"Hand {i + 1}: Blackjack! Total winnings: ${Bets[i] + winnings:F2}.\n";
                    }
                }
                else if (hand.IsBust)
                {
                    // Player loses the bet on this hand
                    GameResult += $"Hand {i + 1}: Bust! Lost ${Bets[i]:F2}.\n";
                }
                else if (DealerHand.IsBust || playerTotal > dealerTotal)
                {
                    // Player wins
                    winnings = Bets[i];
                    PlayerCash += Bets[i] + winnings;
                    GameResult += $"Hand {i + 1}: You Win! Total winnings: ${Bets[i] + winnings:F2}.\n";
                }
                else if (playerTotal == dealerTotal)
                {
                    // Tie
                    PlayerCash += Bets[i];
                    GameResult += $"Hand {i + 1}: Tie! Bet refunded.\n";
                }
                else
                {
                    // Player loses
                    GameResult += $"Hand {i + 1}: Dealer Wins! Lost ${Bets[i]:F2}.\n";
                }
            }

            CurrentBet = 0; // Reset the bet after resolution
        }


        private void ReplenishDeckIfNecessary(bool isNewGame)
        {
            LogMethodStart();

            if (Deck == null || (isNewGame && Deck.CardsRemaining < 120))
            {
                Deck = new Deck(); // Replenish the deck for a new game
                RunningCount = 0;  // Reset running count on new shuffle
            }
            else if (!isNewGame && Deck.CardsRemaining == 0)
            {
                Deck = new Deck(); // Replenish the deck during the game only if it's empty
                RunningCount = 0;  // Reset running count on new shuffle
            }
        }
        
        public double TrueCount
        {
            get
            {
                double remainingDecks = Deck.CardsRemaining / 52.0;
                return remainingDecks > 0 ? Math.Round(RunningCount / remainingDecks, 2) : 0;
            }
        }

        // Call this method whenever a card is dealt
        public void UpdateRunningCount(Card card)
        {
            LogMethodStart();

            // Assign values for card counting (High-Low system)
            if (card.PrimaryValue >= 2 && card.PrimaryValue <= 6)
            {
                RunningCount++; // Low cards increase count
            }
            else if (card.PrimaryValue >= 10 || card.Rank == Rank.Ace)
            {
                RunningCount--; // High cards decrease count
            }
        }
    }
}
