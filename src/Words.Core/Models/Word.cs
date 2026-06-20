namespace Words.Core.Models;

/// <summary>
/// Represents a word entry in the word bank.
/// </summary>
public record Word(
    string Text,
    WordCategory Category,
    GameDifficulty Difficulty,
    string Hint
);
