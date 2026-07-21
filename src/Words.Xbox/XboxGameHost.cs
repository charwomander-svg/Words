using Words.Core.Models;
using Words.Core.Services;
using Words.Core.Interfaces;

namespace Words.Xbox;

/// <summary>
/// Xbox game host: drives the legacy console word-guessing loop.
/// On a real Xbox title this class would be wired into the game engine's
/// update/render cycle rather than a console read loop.
/// </summary>
public class XboxGameHost
{
    private readonly GameService _gameService;
    private readonly IScoreService _scoreService;
    private readonly TextReader _input;
    private readonly TextWriter _output;

    public XboxGameHost(GameService gameService, IScoreService scoreService, TextReader? input = null, TextWriter? output = null)
    {
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
        _scoreService = scoreService ?? throw new ArgumentNullException(nameof(scoreService));
        _input = input ?? Console.In;
        _output = output ?? Console.Out;
    }

    /// <summary>
    /// Runs the game loop until the player decides to quit.
    /// </summary>
    public void Run()
    {
        WriteBanner();

        var player = CreatePlayer();

        bool quit = false;
        while (!quit)
        {
            switch (PromptEnum<MainMenuChoice>($"\nCurrent player: {player.GamerTag}  |  Score: {player.Score}\n" +
                                               "1) Play a round\n" +
                                               "2) View leaderboard\n" +
                                               "3) Quit\n" +
                                               "Choose an option: "))
            {
                case MainMenuChoice.Play:
                    var config = SelectConfig();
                    PlayRound(player, config);
                    break;
                case MainMenuChoice.Leaderboard:
                    ShowLeaderboard();
                    break;
                case MainMenuChoice.Quit:
                    quit = true;
                    break;
            }
        }

        _output.WriteLine($"\nThanks for playing, {player.GamerTag}!");
        _output.WriteLine($"Final score: {player.Score}  |  Won {player.GamesWon}/{player.GamesPlayed} games");
    }

    // -------------------------------------------------------------------------

    private void WriteBanner()
    {
        _output.WriteLine("=== Words Legacy Xbox Host ===");
        _output.WriteLine();
        _output.WriteLine("Legacy letter-guessing mode for platform plumbing.");
        _output.WriteLine("You can choose a difficulty and category before each round.");
    }

    private Player CreatePlayer()
    {
        _output.Write("Enter your GamerTag: ");
        var tag = _input.ReadLine()?.Trim();
        while (string.IsNullOrWhiteSpace(tag))
        {
            _output.Write("GamerTag cannot be empty. Try again: ");
            tag = _input.ReadLine()?.Trim();
        }
        return new Player(tag);
    }

    private GameConfig SelectConfig()
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
            _output.WriteLine($"[Error] {ex.Message}");
            return;
        }

        _output.WriteLine($"\nHint: {session.Hint}");
        _output.WriteLine($"Word: {session.MaskedWord}  |  Guesses left: {session.RemainingGuesses}");
        _output.WriteLine($"Guessed: (none)");

        while (session.Status == GameStatus.InProgress)
        {
            _output.Write("\nGuess a letter: ");
            var input = _input.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input) || input.Length != 1 || !char.IsLetter(input[0]))
            {
                _output.WriteLine("Please enter a single letter.");
                continue;
            }

            var result = _gameService.SubmitGuess(session.Id, input[0]);

            switch (result.Outcome)
            {
                case GuessOutcome.AlreadyGuessed:
                    _output.WriteLine($"  You already guessed '{char.ToUpperInvariant(result.Letter)}'.");
                    break;
                case GuessOutcome.Correct:
                    _output.WriteLine($"  ✓ '{char.ToUpperInvariant(result.Letter)}' is in the word!");
                    break;
                case GuessOutcome.Incorrect:
                    _output.WriteLine($"  ✗ '{char.ToUpperInvariant(result.Letter)}' is not in the word.");
                    break;
            }

            _output.WriteLine($"  Word: {session.MaskedWord}  |  Guesses left: {session.RemainingGuesses}");
            _output.WriteLine($"  Guessed: {string.Join(" ", session.GuessedLetters.Order())}");
        }

        if (session.Status == GameStatus.Won)
        {
            int score = session.CalculateScore();
            _output.WriteLine($"\n🎉 You guessed '{session.Answer}'! +{score} points");
        }
        else
        {
            _output.WriteLine($"\n💀 Out of guesses! The word was '{session.Answer}'.");
        }

        _output.WriteLine($"  Total score: {player.Score}  |  Games won: {player.GamesWon}/{player.GamesPlayed}");

        // EndGame is called automatically by SubmitGuess once the session is
        // no longer InProgress, so no explicit call is needed here.
    }

    private void ShowLeaderboard()
    {
        var leaderboard = _scoreService.GetLeaderboard();
        _output.WriteLine("\n=== Leaderboard ===");

        if (leaderboard.Count == 0)
        {
            _output.WriteLine("No scores yet.");
            return;
        }

        for (int index = 0; index < Math.Min(leaderboard.Count, 5); index++)
        {
            var player = leaderboard[index];
            _output.WriteLine($"{index + 1}. {player.GamerTag,-16} {player.Score,4}");
        }
    }

    private T PromptEnum<T>(string prompt) where T : struct, Enum
    {
        while (true)
        {
            _output.Write(prompt);
            var input = _input.ReadLine()?.Trim() ?? string.Empty;
            if (Enum.TryParse<T>(input, ignoreCase: true, out var value))
                return value;
            _output.WriteLine($"  Invalid choice. Valid options: {string.Join(", ", Enum.GetNames<T>())}");
        }
    }

    private enum MainMenuChoice
    {
        Play = 1,
        Leaderboard = 2,
        Quit = 3
    }
}
