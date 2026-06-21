using Words.Core.Models;
using Words.Core.Services;
using Words.Xbox.Audio;
using Words.Xbox.Presentation;

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
    private readonly GameAudioService _audio;

    public XboxGameHost(GameService gameService, GameAudioService audio)
    {
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
        _audio = audio ?? throw new ArgumentNullException(nameof(audio));
    }

    /// <summary>
    /// Runs the game loop until the player decides to quit.
    /// </summary>
    public void Run()
    {
        var player = CreatePlayer();
        var reducedMotion = IsReducedMotionEnabled();
        var titleFrame = 0;
        bool keepPlaying = true;
        while (keepPlaying)
        {
            _audio.EnterTitleScreen();
            Console.WriteLine();
            Console.WriteLine(XboxPresentation.ComposeTitleScreen(player, titleFrame++, reducedMotion));
            Console.WriteLine();

            var choice = PromptEnum<XboxTitleMenuChoice>("Choose a menu option: ");
            switch (choice)
            {
                case XboxTitleMenuChoice.Play:
                    var config = SelectConfig();
                    PlayRound(player, config);
                    break;
                case XboxTitleMenuChoice.Tutorial:
                    ShowInfoPage(player, _audio.EnterTitleScreen, XboxPresentation.ComposeTutorialPage());
                    break;
                case XboxTitleMenuChoice.Credits:
                    ShowCredits(player);
                    break;
                case XboxTitleMenuChoice.Privacy:
                    ShowInfoPage(player, _audio.EnterTitleScreen, XboxPresentation.ComposePrivacyPage());
                    break;
                case XboxTitleMenuChoice.Legal:
                    ShowInfoPage(player, _audio.EnterTitleScreen, XboxPresentation.ComposeLegalPage());
                    break;
                case XboxTitleMenuChoice.Quit:
                    keepPlaying = false;
                    break;
            }
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

    private void ShowCredits(Player player)
    {
        _audio.EnterCredits();
        Console.WriteLine();
        Console.WriteLine(XboxPresentation.ComposeCreditsPage(player));
        Console.WriteLine();
        player.RecordCreditsViewed();
        WaitForContinue();
        _audio.EnterTitleScreen();
    }

    private void ShowInfoPage(Player player, Action enterMusic, string page)
    {
        enterMusic();
        Console.WriteLine();
        Console.WriteLine(page);
        Console.WriteLine();
        Console.WriteLine($"Player: {player.GamerTag}");
        WaitForContinue();
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

        _audio.EnterRound();
        _audio.PlayRoundStart();
        Console.WriteLine();
        Console.WriteLine(XboxPresentation.ComposeRoundScreen(session, pendingGuess: string.Empty, selectedIndex: 0));
        Console.WriteLine(XboxInputScheme.Describe());

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
                    _audio.PlayNavigate();
                    Console.WriteLine($"  Selected letter: {QwertyOrder[selectedIndex]}");
                    continue;
                case XboxRoundAction.MoveNext:
                    selectedIndex = CycleQwerty(selectedIndex, 1);
                    _audio.PlayNavigate();
                    Console.WriteLine($"  Selected letter: {QwertyOrder[selectedIndex]}");
                    continue;
                case XboxRoundAction.ConfirmLetter:
                    pendingGuess += selectedLetter;
                    _audio.PlayConfirm();
                    Console.WriteLine($"  Added '{selectedLetter}'");
                    continue;
                case XboxRoundAction.DeleteLetter:
                    if (pendingGuess.Length > 0)
                    {
                        pendingGuess = pendingGuess[..^1];
                        _audio.PlayDelete();
                        Console.WriteLine($"  Deleted last letter. Word is now [{pendingGuess}]");
                    }
                    else
                    {
                        Console.WriteLine("  Nothing to delete.");
                    }
                    continue;
                case XboxRoundAction.RequestHint:
                    var hint = session.RequestHint();
                    if (hint.IsAvailable)
                        _audio.PlayHint();
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
                        _audio.PlayCorrectGuess();
                        Console.WriteLine($"  ✓ Submitted '{pendingGuess}'.");
                    }
                    else
                    {
                        _audio.PlayIncorrectGuess();
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

            Console.WriteLine();
            Console.WriteLine(XboxPresentation.ComposeRoundScreen(session, pendingGuess, selectedIndex));
        }

        if (session.Status == GameStatus.Won)
        {
            int score = session.CalculateScore();
            _audio.PlayRoundEnd();
            Console.WriteLine($"\n🎉 You guessed the word! +{score} points");
        }
        else
        {
            _audio.PlayRoundEnd();
            Console.WriteLine($"\n💀 Out of guesses! Better luck next time.");
        }

        var newAchievements = player.UnlockedAchievements
            .Where(achievement => !achievementsBeforeRound.Contains(achievement))
            .ToList();
        if (newAchievements.Count > 0)
        {
            _audio.PlayAchievement();
            Console.WriteLine("🏆 Achievement unlocked:");
            foreach (var achievement in newAchievements)
                Console.WriteLine($"  - {achievement}");
        }

        // GameService.EndGame is called when the round finishes.
        WaitForContinue();
        _audio.EnterTitleScreen();
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

    private static bool IsReducedMotionEnabled() =>
        string.Equals(Environment.GetEnvironmentVariable("WORDS_REDUCED_MOTION"), "1", StringComparison.OrdinalIgnoreCase) ||
        Console.IsOutputRedirected;

    private static void WaitForContinue()
    {
        Console.Write("Press Enter to continue...");
        Console.ReadLine();
    }
}
