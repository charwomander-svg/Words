using Words.Core.Models;
using Words.Core.Services;

namespace Words.Xbox;

/// <summary>
/// Xbox game host: drives a console-based "Guess That Word" game loop.
/// On a real Xbox title this class would be wired into the game engine's
/// update/render cycle rather than a console read loop.
/// </summary>
public class XboxGameHost
{
    private readonly GameService _gameService;

    public XboxGameHost(GameService gameService)
    {
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
    }

    /// <summary>
    /// Runs the game loop until the player decides to quit.
    /// </summary>
    public void Run()
    {
        Console.WriteLine("=== Guess That Word – Xbox Edition ===");
        Console.WriteLine();

        var player = CreatePlayer();

        bool keepPlaying = true;
        while (keepPlaying)
        {
            var config = SelectConfig();
            PlayRound(player, config);

            Console.Write("\nPlay again? (Y/N): ");
            keepPlaying = Console.ReadLine()?.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        Console.WriteLine($"\nThanks for playing, {player.GamerTag}!");
        Console.WriteLine($"Final score: {player.Score}  |  Won {player.GamesWon}/{player.GamesPlayed} games");
    }

    // -------------------------------------------------------------------------

    private static Player CreatePlayer()
    {
        Console.Write("Enter your GamerTag: ");
        var tag = Console.ReadLine()?.Trim();
        while (string.IsNullOrWhiteSpace(tag))
        {
            Console.Write("GamerTag cannot be empty. Try again: ");
            tag = Console.ReadLine()?.Trim();
        }
        return new Player(tag);
    }

    private static GameConfig SelectConfig()
    {
        var difficulty = PromptEnum<GameDifficulty>("Select difficulty (Easy / Medium / Hard): ");
        var category   = PromptEnum<WordCategory>("Select category (General / Animals / Food / Sports / Science / Geography / Entertainment / Technology): ");
        return new GameConfig { Difficulty = difficulty, Category = category };
    }

    private void PlayRound(Player player, GameConfig config)
    {
        GameSession session;
        try
        {
            session = _gameService.StartGame(player, config);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"[Error] {ex.Message}");
            return;
        }

        Console.WriteLine($"\nHint: {session.Hint}");
        Console.WriteLine($"Word: {session.MaskedWord}  |  Guesses left: {session.RemainingGuesses}");
        Console.WriteLine($"Guessed: (none)");

        while (session.Status == GameStatus.InProgress)
        {
            Console.Write("\nGuess a letter: ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input) || input.Length != 1 || !char.IsLetter(input[0]))
            {
                Console.WriteLine("Please enter a single letter.");
                continue;
            }

            var result = _gameService.SubmitGuess(session.Id, input[0]);

            switch (result.Outcome)
            {
                case GuessOutcome.AlreadyGuessed:
                    Console.WriteLine($"  You already guessed '{char.ToUpperInvariant(result.Letter)}'.");
                    break;
                case GuessOutcome.Correct:
                    Console.WriteLine($"  ✓ '{char.ToUpperInvariant(result.Letter)}' is in the word!");
                    break;
                case GuessOutcome.Incorrect:
                    Console.WriteLine($"  ✗ '{char.ToUpperInvariant(result.Letter)}' is not in the word.");
                    break;
            }

            Console.WriteLine($"  Word: {session.MaskedWord}  |  Guesses left: {session.RemainingGuesses}");
            Console.WriteLine($"  Guessed: {string.Join(" ", session.GuessedLetters.Order())}");
        }

        if (session.Status == GameStatus.Won)
        {
            int score = session.CalculateScore();
            Console.WriteLine($"\n🎉 You guessed the word! +{score} points");
        }
        else
        {
            Console.WriteLine($"\n💀 Out of guesses! Better luck next time.");
        }

        // EndGame is called automatically by SubmitGuess once the session is
        // no longer InProgress, so no explicit call is needed here.
    }

    private static T PromptEnum<T>(string prompt) where T : struct, Enum
    {
        while (true)
        {
            Console.Write(prompt);
            var input = Console.ReadLine()?.Trim() ?? string.Empty;
            if (Enum.TryParse<T>(input, ignoreCase: true, out var value))
                return value;
            Console.WriteLine($"  Invalid choice. Valid options: {string.Join(", ", Enum.GetNames<T>())}");
        }
    }
}
