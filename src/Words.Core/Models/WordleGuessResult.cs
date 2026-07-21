namespace Words.Core.Models;

public sealed record WordleGuessResult(
    string Guess,
    IReadOnlyList<WordleTileState> Tiles,
    bool IsCorrect
);
