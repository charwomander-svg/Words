namespace Words.Core.Models;

/// <summary>
/// Configuration that controls how a game session is set up.
/// </summary>
public class GameConfig
{
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
}
