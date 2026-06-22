namespace Words.Core.Models;

/// <summary>
/// The current status of a game session.
/// </summary>
public enum GameStatus
{
    InProgress,
    Won,
    Lost
}

/// <summary>
/// Holds the full state of an active game session.
/// </summary>
public class GameSession
{
    private readonly Word _word;
    private readonly HashSet<char> _guessedLetters = new();

    public Guid Id { get; } = Guid.NewGuid();
    public Player Player { get; }
    public GameConfig Config { get; }
    public GameStatus Status { get; private set; } = GameStatus.InProgress;
    public int IncorrectGuesses { get; private set; }
    public IReadOnlySet<char> GuessedLetters => _guessedLetters;
    public string Answer => _word.Text;
    public string Hint => _word.Hint;

    /// <summary>
    /// The word with unguessed letters replaced by underscores.
    /// </summary>
    public string MaskedWord =>
        new(_word.Text.Select(c => _guessedLetters.Contains(c) ? c : '_').ToArray());

    public int RemainingGuesses => Config.MaxIncorrectGuesses - IncorrectGuesses;

    public GameSession(Player player, Word word, GameConfig config)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        _word = word ?? throw new ArgumentNullException(nameof(word));
        Config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Processes a guessed letter and returns the result.
    /// </summary>
    public GuessResult Guess(char letter)
    {
        if (Status != GameStatus.InProgress)
            return new GuessResult(letter, GuessOutcome.GameOver, MaskedWord, false);

        letter = char.ToUpperInvariant(letter);

        if (_guessedLetters.Contains(letter))
            return new GuessResult(letter, GuessOutcome.AlreadyGuessed, MaskedWord, false);

        _guessedLetters.Add(letter);

        bool isInWord = _word.Text.Contains(letter, StringComparison.OrdinalIgnoreCase);

        if (!isInWord)
            IncorrectGuesses++;

        string masked = MaskedWord;
        bool solved = !masked.Contains('_');

        if (solved)
            Status = GameStatus.Won;
        else if (IncorrectGuesses >= Config.MaxIncorrectGuesses)
            Status = GameStatus.Lost;

        return new GuessResult(
            letter,
            isInWord ? GuessOutcome.Correct : GuessOutcome.Incorrect,
            masked,
            solved
        );
    }

    /// <summary>
    /// Calculates the score for a won game (returns 0 if not won).
    /// </summary>
    public int CalculateScore() =>
        Status == GameStatus.Won
            ? Config.BasePoints + RemainingGuesses * Config.BonusPerRemainingGuess
            : 0;
}
