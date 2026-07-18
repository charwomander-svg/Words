namespace Words.Core.Models;

/// <summary>
/// Configuration that controls how a game session is set up.
/// </summary>
public class GameConfig
{
    /// <summary>Rule set used for the game round.</summary>
    public GameMode Mode { get; init; } = GameMode.Classic;

    /// <summary>Difficulty level for the game.</summary>
    public GameDifficulty Difficulty { get; init; } = GameDifficulty.Medium;

    /// <summary>Category to draw the word from.</summary>
    public WordCategory Category { get; init; } = WordCategory.General;

    /// <summary>Maximum number of incorrect guesses allowed before the game ends.</summary>
    public int MaxIncorrectGuesses { get; init; } = 6;

    /// <summary>Points awarded for correctly guessing the word.</summary>
    public int BasePoints { get; init; } = 100;

    /// <summary>Bonus multiplier applied for each remaining guess at win time.</summary>
    public int BonusPerRemainingGuess { get; init; } = 10;

    /// <summary>Maximum incorrect guesses after applying mode rules.</summary>
    public int EffectiveMaxIncorrectGuesses => Mode switch
    {
        GameMode.Relaxed => MaxIncorrectGuesses + 3,
        GameMode.Challenge => Math.Max(1, MaxIncorrectGuesses - 2),
        _ => MaxIncorrectGuesses
    };

    /// <summary>Base win points after applying mode rules.</summary>
    public int EffectiveBasePoints => Mode switch
    {
        GameMode.Relaxed => Math.Max(0, BasePoints / 2),
        GameMode.Challenge => BasePoints + 50,
        _ => BasePoints
    };

    /// <summary>Remaining-guess bonus after applying mode rules.</summary>
    public int EffectiveBonusPerRemainingGuess => Mode switch
    {
        GameMode.Relaxed => Math.Max(0, BonusPerRemainingGuess / 2),
        GameMode.Challenge => BonusPerRemainingGuess * 2,
        _ => BonusPerRemainingGuess
    };
}
