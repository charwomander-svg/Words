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
    private static readonly char[] QwertyOrder = "QWERTYUIOPASDFGHJKLZXCVBNM".ToCharArray();
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
        PromptCredits(player);

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
        if (player.UnlockedAchievements.Count > 0)
            Console.WriteLine($"Achievements: {string.Join(", ", player.UnlockedAchievements)}");
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

    private static void PromptCredits(Player player)
    {
        Console.Write("View credits now? (Y/N): ");
        var input = Console.ReadLine()?.Trim();
        if (!string.Equals(input, "Y", StringComparison.OrdinalIgnoreCase))
            return;

        Console.WriteLine();
        Console.WriteLine("=== Credits ===");
        Console.WriteLine("Words: Xbox edition");
        Console.WriteLine("Concept, code, and chaos: the Words team");
        Console.WriteLine("================");
        player.RecordCreditsViewed();
    }

    private static GameConfig SelectConfig()
    {
        var length = PromptInt("Select word length (4-20): ", 4, 20);
        var concurrentWords = PromptInt("How many concurrent words? (1-100): ", 1, 100);
        return new GameConfig { WordLength = length, ConcurrentWords = concurrentWords };
    }

    private void PlayRound(Player player, GameConfig config)
    {
        var achievementsBeforeRound = player.UnlockedAchievements.ToHashSet(StringComparer.OrdinalIgnoreCase);
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
        Console.WriteLine($"Word(s): {string.Join(" | ", session.MaskedWords)}  |  Guesses left: {session.RemainingGuesses}");
        Console.WriteLine($"Guessed: (none)");
        Console.WriteLine(XboxInputScheme.Describe());
        Console.WriteLine("Layout: QWERTYUIOP / ASDFGHJKL / ZXCVBNM");

        var selectedIndex = 0;
        var pendingGuess = string.Empty;

        while (session.Status == GameStatus.InProgress)
        {
            var selectedLetter = QwertyOrder[selectedIndex];
            Console.Write($"\nInput [{selectedLetter}] Word [{pendingGuess}]: ");
            var command = XboxInputScheme.ParseRoundCommand(Console.ReadLine());

            switch (command.Action)
            {
                case XboxRoundAction.Invalid:
                    Console.WriteLine("Use A/B/X/Y, LEFT/RIGHT, ? or Q.");
                    continue;
                case XboxRoundAction.ShowHelp:
                    Console.WriteLine(XboxInputScheme.Describe());
                    continue;
                case XboxRoundAction.QuitRound:
                    Console.WriteLine("Round ended early.");
                    return;
                case XboxRoundAction.MovePrevious:
                    selectedIndex = CycleQwerty(selectedIndex, -1);
                    Console.WriteLine($"  Selected letter: {QwertyOrder[selectedIndex]}");
                    continue;
                case XboxRoundAction.MoveNext:
                    selectedIndex = CycleQwerty(selectedIndex, 1);
                    Console.WriteLine($"  Selected letter: {QwertyOrder[selectedIndex]}");
                    continue;
                case XboxRoundAction.ConfirmLetter:
                    pendingGuess += selectedLetter;
                    Console.WriteLine($"  Added '{selectedLetter}'");
                    continue;
                case XboxRoundAction.DeleteLetter:
                    if (pendingGuess.Length > 0)
                    {
                        pendingGuess = pendingGuess[..^1];
                        Console.WriteLine($"  Deleted last letter. Word is now [{pendingGuess}]");
                    }
                    else
                    {
                        Console.WriteLine("  Nothing to delete.");
                    }
                    continue;
                case XboxRoundAction.RequestHint:
                    var hint = session.RequestHint();
                    Console.WriteLine(hint.Message);
                    continue;
                case XboxRoundAction.SubmitWord:
                    if (pendingGuess.Length == 0)
                    {
                        Console.WriteLine("  Add at least one letter before submitting.");
                        continue;
                    }

                    bool correct = pendingGuess.Length == 1
                        ? session.Guess(pendingGuess[0]).Outcome == GuessOutcome.Correct
                        : session.GuessWord(pendingGuess);

                    if (correct)
                    {
                        Console.WriteLine($"  ✓ Submitted '{pendingGuess}'.");
                    }
                    else
                    {
                        Console.WriteLine($"  ✗ '{pendingGuess}' is not in the word.");
                    }

                    pendingGuess = string.Empty;
                    break;
            }

            if (session.Status != GameStatus.InProgress)
            {
                _gameService.EndGame(session.Id);
                break;
            }

            Console.WriteLine($"  Word(s): {string.Join(" | ", session.MaskedWords)}  |  Guesses left: {session.RemainingGuesses}");
            Console.WriteLine($"  Guessed: {string.Join(" ", session.GuessedLetters.Order())}");
            Console.WriteLine($"  Hints left: {(session.CanUseHints ? session.RemainingHints : 0)}");
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

        var newAchievements = player.UnlockedAchievements
            .Where(achievement => !achievementsBeforeRound.Contains(achievement))
            .ToList();
        if (newAchievements.Count > 0)
        {
            Console.WriteLine("🏆 Achievement unlocked:");
            foreach (var achievement in newAchievements)
                Console.WriteLine($"  - {achievement}");
        }

        // GameService.EndGame is called when the round finishes.
    }

    private static int CycleQwerty(int index, int offset)
    {
        var next = (index + offset + QwertyOrder.Length) % QwertyOrder.Length;
        return next;
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

    private static int PromptInt(string prompt, int min, int max)
    {
        while (true)
        {
            Console.Write(prompt);
            var input = Console.ReadLine()?.Trim();
            if (int.TryParse(input, out var value) && value >= min && value <= max)
                return value;
            Console.WriteLine($"  Invalid choice. Enter a number between {min} and {max}.");
        }
    }
}
