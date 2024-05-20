using System;
using System.Collections.Generic;

public enum Color { Red, Green, Blue, Yellow, Wild }
public enum SpecialRound { None, DoubleTrouble, ExtraDraw, SpeedRound }

public class Card
{
    public Color Color { get; }
    public string Value { get; }
    public Card(Color color, string value)
    {
        Color = color;
        Value = value;
    }

    public override string ToString()
    {
        return $"{Color} {Value}";
    }
}

public class Player
{
    public List<Card> Hand { get; } = new List<Card>();
    public string Name { get; }

    public Player(string name)
    {
        Name = name;
    }

    public void DrawCard(Card card)
    {
        Hand.Add(card);
    }

    public void PlayCard(Card card)
    {
        Hand.Remove(card);
    }

    public bool HasPlayableCard(Card topCard)
    {
        foreach (var card in Hand)
        {
            if (card.Color == topCard.Color || card.Value == topCard.Value || card.Color == Color.Wild)
                return true;
        }
        return false;
    }

    public override string ToString()
    {
        return Name;
    }
}

public class Game
{
    private List<Player> players = new List<Player>();
    private List<Card> deck = new List<Card>();
    private Stack<Card> discardPile = new Stack<Card>();
    private Random rand = new Random();
    private int currentPlayerIndex = 0;
    private int direction = 1;
    private SpecialRound specialRound = SpecialRound.None;
    private DateTime speedRoundEndTime;

    public Game(string[] playerNames)
    {
        foreach (var name in playerNames)
            players.Add(new Player(name));
    }

    private void InitializeDeck()
    {
        foreach (Color color in Enum.GetValues(typeof(Color)))
        {
            if (color == Color.Wild)
                continue;

            for (int i = 0; i <= 9; i++)
            {
                deck.Add(new Card(color, i.ToString()));
                if (i != 0) deck.Add(new Card(color, i.ToString())); // Each number appears twice except 0
            }

            string[] actions = { "Switch", "Skip", "Draw Two" };
            foreach (var action in actions)
            {
                deck.Add(new Card(color, action));
                deck.Add(new Card(color, action));
            }
        }

        for (int i = 0; i < 4; i++)
        {
            deck.Add(new Card(Color.Wild, "Wild"));
            deck.Add(new Card(Color.Wild, "Wild Draw Four"));
            deck.Add(new Card(Color.Wild, "Swap Hands"));
            deck.Add(new Card(Color.Wild, "Play Again"));
            deck.Add(new Card(Color.Wild, "Shield Block"));
        }

        ShuffleDeck();
    }

    private void ShuffleDeck()
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = rand.Next(i + 1);
            var temp = deck[i];
            deck[i] = deck[j];
            deck[j] = temp;
        }
    }

    private Card DrawCard()
    {
        if (deck.Count == 0)
        {
            var temp = new List<Card>(discardPile);
            discardPile.Clear();
            deck.AddRange(temp);
            ShuffleDeck();
        }

        var card = deck[0];
        deck.RemoveAt(0);
        return card;
    }

    private void DealCards()
    {
        for (int i = 0; i < 7; i++)
        {
            foreach (var player in players)
            {
                player.DrawCard(DrawCard());
            }
        }
    }

    private void StartGame()
    {
        InitializeDeck();
        DealCards();
        discardPile.Push(DrawCard());

        Console.WriteLine($"Starting card: {discardPile.Peek()}");

        DetermineSpecialRound();
        if (specialRound != SpecialRound.None)
            Console.WriteLine($"Special Round Activated: {specialRound}");

        while (true)
        {
            PlayTurn();
            if (CheckForWin())
                break;
        }
    }

    private void DetermineSpecialRound()
    {
        int roll = rand.Next(3);
        if (roll == 0)
        {
            specialRound = (SpecialRound)rand.Next(1, 4);
            if (specialRound == SpecialRound.SpeedRound)
                speedRoundEndTime = DateTime.Now.AddSeconds(5); // Adjust time for speed round
        }
    }

    private void PlayTurn()
    {
        var currentPlayer = players[currentPlayerIndex];
        Console.WriteLine($"\n{currentPlayer.Name}'s turn. Current top card: {discardPile.Peek()}");

        if (specialRound == SpecialRound.ExtraDraw)
            currentPlayer.DrawCard(DrawCard());

        DisplayHand(currentPlayer);

        if (specialRound == SpecialRound.SpeedRound && DateTime.Now > speedRoundEndTime)
        {
            Console.WriteLine("Speed Round: Time's up! Drawing a card.");
            currentPlayer.DrawCard(DrawCard());
            NextPlayer();
            return;
        }

        Card chosenCard = null;
        while (true)
        {
            Console.Write("Choose a card to play or draw a card (enter the number or 'd' to draw): ");
            var input = Console.ReadLine();

            if (input.ToLower() == "d")
            {
                chosenCard = DrawCard();
                currentPlayer.DrawCard(chosenCard);
                Console.WriteLine($"Drew card: {chosenCard}");
                break;
            }

            if (int.TryParse(input, out int cardIndex) && cardIndex >= 0 && cardIndex < currentPlayer.Hand.Count)
            {
                chosenCard = currentPlayer.Hand[cardIndex];
                if (chosenCard.Color == discardPile.Peek().Color || chosenCard.Value == discardPile.Peek().Value || chosenCard.Color == Color.Wild)
                {
                    currentPlayer.PlayCard(chosenCard);
                    discardPile.Push(chosenCard);
                    Console.WriteLine($"Played card: {chosenCard}");
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid card choice. Please choose a valid card.");
                }
            }
            else
            {
                Console.WriteLine("Invalid input. Please try again.");
            }
        }

        ApplyCardEffect(chosenCard);
        if (currentPlayer.Hand.Count == 0)
        {
            Console.WriteLine($"{currentPlayer.Name} has won the game!");
            Environment.Exit(0);
        }

        NextPlayer();
    }

    private void DisplayHand(Player player)
    {
        Console.WriteLine("Your hand:");
        for (int i = 0; i < player.Hand.Count; i++)
        {
            Console.WriteLine($"{i}: {player.Hand[i]}");
        }
    }

    private void ApplyCardEffect(Card card)
    {
        if (card.Value == "Switch")
        {
            direction *= -1;
            Console.WriteLine("Direction switched!");
        }
        else if (card.Value == "Skip")
        {
            NextPlayer();
            Console.WriteLine("Next player skipped!");
        }
        else if (card.Value == "Draw Two")
        {
            NextPlayer();
            players[currentPlayerIndex].DrawCard(DrawCard());
            players[currentPlayerIndex].DrawCard(DrawCard());
            Console.WriteLine($"{players[currentPlayerIndex].Name} draws two cards!");
        }
        else if (card.Value == "Wild")
        {
            ChangeColor();
        }
        else if (card.Value == "Wild Draw Four")
        {
            ChangeColor();
            NextPlayer();
            players[currentPlayerIndex].DrawCard(DrawCard());
            players[currentPlayerIndex].DrawCard(DrawCard());
            players[currentPlayerIndex].DrawCard(DrawCard());
            players[currentPlayerIndex].DrawCard(DrawCard());
            Console.WriteLine($"{players[currentPlayerIndex].Name} draws four cards!");
        }
        else if (card.Value == "Swap Hands")
        {
            SwapHands();
        }
        else if (card.Value == "Play Again")
        {
            Console.WriteLine($"{players[currentPlayerIndex].Name} gets to play again!");
        }
        else if (card.Value == "Shield Block")
        {
            Console.WriteLine("Shield Block activated! Next action card will be blocked.");
        }
    }

    private void ChangeColor()
    {
        Console.Write("Choose a new color (Red, Green, Blue, Yellow): ");
        var newColor = Console.ReadLine();
        if (Enum.TryParse(newColor, true, out Color color) && color != Color.Wild)
        {
            discardPile.Push(new Card(color, "Wild"));
            Console.WriteLine($"Color changed to {color}");
        }
        else
        {
            Console.WriteLine("Invalid color. Try again.");
            ChangeColor();
        }
    }

    private void SwapHands()
    {
        Console.WriteLine("Choose a player to swap hands with:");
        for (int i = 0; i < players.Count; i++)
        {
            if (i != currentPlayerIndex)
                Console.WriteLine($"{i}: {players[i].Name}");
        }

        if (int.TryParse(Console.ReadLine(), out int playerIndex) && playerIndex >= 0 && playerIndex < players.Count && playerIndex != currentPlayerIndex)
        {
            var currentPlayerHand = new List<Card>(players[currentPlayerIndex].Hand);
            var targetPlayerHand = new List<Card>(players[playerIndex].Hand);

            players[currentPlayerIndex].Hand.Clear();
            players[playerIndex].Hand.Clear();

            players[currentPlayerIndex].Hand.AddRange(targetPlayerHand);
            players[playerIndex].Hand.AddRange(currentPlayerHand);

            Console.WriteLine($"{players[currentPlayerIndex].Name} swapped hands with {players[playerIndex].Name}.");
        }
        else
        {
            Console.WriteLine("Invalid choice. Try again.");
            SwapHands();
        }
    }


    private void NextPlayer()
    {
        currentPlayerIndex = (currentPlayerIndex + direction + players.Count) % players.Count;
    }

    private bool CheckForWin()
    {
        foreach (var player in players)
        {
            if (player.Hand.Count == 0)
            {
                Console.WriteLine($"{player.Name} has won the game!");
                return true;
            }
        }
        return false;
    }

    public static void Main(string[] args)
    {
        Console.WriteLine("Welcome to Color Clash!");
        Console.Write("Enter player names (comma separated): ");
        var playerNames = Console.ReadLine().Split(',');
        var game = new Game(playerNames);
        game.StartGame();
    }
}
