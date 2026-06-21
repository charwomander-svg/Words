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
    private readonly IReadOnlyList<Word> _words;
    private readonly HashSet<char> _guessedLetters = new();
    private int _hintsUsed;

    public Guid Id { get; } = Guid.NewGuid();
    public Player Player { get; }
    public GameConfig Config { get; }
    public GameStatus Status { get; private set; } = GameStatus.InProgress;
    public int IncorrectGuesses { get; private set; }
    public IReadOnlySet<char> GuessedLetters => _guessedLetters;
    public IReadOnlyList<Word> Words => _words;
    public Word PrimaryWord => _words[0];
    public string Hint => _words.Count == 1 ? _words[0].Hint : $"{_words.Count} words in play";
    public IReadOnlyList<string> Hints => _words.Select(word => word.Hint).ToArray();
    public int HintsUsed => _hintsUsed;
    public int RemainingHints => Config.HintsEnabled ? Math.Max(0, Config.MaxHintsPerWord - _hintsUsed) : 0;
    public bool CanUseHints => Config.HintsEnabled && RemainingHints > 0 && Status == GameStatus.InProgress;

    /// <summary>
    /// The word with unguessed letters replaced by underscores.
    /// </summary>
    public string MaskedWord => MaskedWords.Count == 1 ? MaskedWords[0] : string.Join(" ", MaskedWords);

    /// <summary>
    /// The masked versions of all concurrent words.
    /// </summary>
    public IReadOnlyList<string> MaskedWords =>
        _words.Select(word => new string(word.Text.Select(c => _guessedLetters.Contains(char.ToUpperInvariant(c)) ? c : '_').ToArray()))
              .ToArray();

    public int RemainingGuesses => GetGuessBudget() - IncorrectGuesses;

    public GameSession(Player player, Word word, GameConfig config)
        : this(player, new[] { word }, config)
    {
    }

    public GameSession(Player player, IReadOnlyList<Word> words, GameConfig config)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        _words = words is { Count: > 0 } ? words : throw new ArgumentException("At least one word is required.", nameof(words));
        Config = config ?? throw new ArgumentNullException(nameof(config));
        Config.Validate();
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

        bool isInWord = _words.Any(word => word.Text.Contains(letter, StringComparison.OrdinalIgnoreCase));

        if (!isInWord)
            IncorrectGuesses++;

        bool solved = MaskedWords.All(mask => !mask.Contains('_'));

        if (solved)
            Status = GameStatus.Won;
        else if (IncorrectGuesses >= GetGuessBudget())
            Status = GameStatus.Lost;

        return new GuessResult(
            letter,
            isInWord ? GuessOutcome.Correct : GuessOutcome.Incorrect,
            masked,
            solved
        );
    }

    /// <summary>
    /// Processes a full-word guess and returns whether it matched any target word.
    /// </summary>
    public bool GuessWord(string guess)
    {
        if (Status != GameStatus.InProgress)
            return false;

        guess = guess?.Trim() ?? string.Empty;
        if (guess.Length == 0)
            return false;

        var matchedWord = _words.FirstOrDefault(word => word.Text.Equals(guess, StringComparison.OrdinalIgnoreCase));
        bool isCorrect = matchedWord is not null;

        if (isCorrect)
        {
            foreach (char letter in matchedWord!.Text)
                _guessedLetters.Add(char.ToUpperInvariant(letter));
        }
        else
        {
            IncorrectGuesses++;
        }

        string masked = MaskedWord;
        bool solved = MaskedWords.All(mask => !mask.Contains('_'));

        if (solved)
            Status = GameStatus.Won;
        else if (IncorrectGuesses >= GetGuessBudget())
            Status = GameStatus.Lost;

        return isCorrect;
    }

    /// <summary>
    /// Returns a limited hint showing a correct letter that has not yet been guessed.
    /// </summary>
    public HintResult RequestHint()
    {
        if (!Config.HintsEnabled)
            return new HintResult(false, '\0', "Hints are disabled in this mode.");

        if (Status != GameStatus.InProgress)
            return new HintResult(false, '\0', "Hints are only available during active rounds.");

        if (_hintsUsed >= Config.MaxHintsPerWord)
            return new HintResult(false, '\0', "No hints left for this word.");

        var hintLetter = _words
            .SelectMany(word => word.Text)
            .Select(char.ToUpperInvariant)
            .FirstOrDefault(letter => !_guessedLetters.Contains(letter));

        if (hintLetter == default)
            return new HintResult(false, '\0', "No unrevealed letters remain.");

        _hintsUsed++;
        return new HintResult(true, hintLetter, $"Hint: focus on the letter '{hintLetter}'.");
    }

    /// <summary>
    /// Calculates the score for a won game (returns 0 if not won).
    /// </summary>
    public int CalculateScore() =>
        Status == GameStatus.Won
            ? Math.Max(0, Config.BasePoints + RemainingGuesses * Config.BonusPerRemainingGuess - (HintsUsed * Config.HintPointCost))
            : 0;

    private int GetGuessBudget() => Config.MaxIncorrectGuesses + Math.Max(0, _words.Count - 1);
}
