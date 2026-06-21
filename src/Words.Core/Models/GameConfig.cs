namespace Words.Core.Models;

/// <summary>
/// Configuration that controls how a game session is set up.
/// </summary>
public class GameConfig
{
    /// <summary>Length of each target word.</summary>
    public int WordLength { get; init; } = 5;

    /// <summary>Number of words to solve at the same time.</summary>
    public int ConcurrentWords { get; init; } = 1;

    /// <summary>Maximum number of incorrect guesses allowed before the game ends.</summary>
    public int MaxIncorrectGuesses { get; init; } = 6;

    /// <summary>Points awarded for correctly guessing the word.</summary>
    public int BasePoints { get; init; } = 100;

    /// <summary>Bonus multiplier applied for each remaining guess at win time.</summary>
    public int BonusPerRemainingGuess { get; init; } = 10;

    /// <summary>Whether hint requests are enabled for this session.</summary>
    public bool HintsEnabled { get; init; } = true;

    /// <summary>Maximum hint requests allowed per word.</summary>
    public int MaxHintsPerWord { get; init; } = 3;

    /// <summary>Point cost per hint request.</summary>
    public int HintPointCost { get; init; } = 25;

    /// <summary>
    /// The effective guess budget after accounting for concurrent words.
    /// </summary>
    public int EffectiveMaxIncorrectGuesses => MaxIncorrectGuesses + Math.Max(0, ConcurrentWords - 1);

    /// <summary>
    /// Validates the configured word length and concurrency settings.
    /// </summary>
    public void Validate()
    {
        if (WordLength is < 4 or > 20)
            throw new ArgumentOutOfRangeException(nameof(WordLength), "Word length must be between 4 and 20 letters.");

        if (ConcurrentWords is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(ConcurrentWords), "Concurrent words must be between 1 and 100.");

        if (MaxIncorrectGuesses < 1)
            throw new ArgumentOutOfRangeException(nameof(MaxIncorrectGuesses), "Maximum incorrect guesses must be positive.");

        if (MaxHintsPerWord < 0)
            throw new ArgumentOutOfRangeException(nameof(MaxHintsPerWord), "Max hints per word cannot be negative.");

        if (HintPointCost < 0)
            throw new ArgumentOutOfRangeException(nameof(HintPointCost), "Hint point cost cannot be negative.");
    }
}
