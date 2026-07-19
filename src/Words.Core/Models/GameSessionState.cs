namespace Words.Core.Models;

/// <summary>
/// Immutable session snapshot for UI layers.
/// </summary>
public sealed record GameSessionState(
    Guid Id,
    string GamerTag,
    WordCategory Category,
    GameDifficulty Difficulty,
    GameStatus Status,
    string MaskedWord,
    int RemainingGuesses,
    int IncorrectGuesses,
    IReadOnlyList<char> GuessedLetters,
    string Hint
);
