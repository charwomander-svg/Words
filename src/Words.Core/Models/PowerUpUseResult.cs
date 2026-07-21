namespace Words.Core.Models;

public sealed record PowerUpUseResult(
    PowerUpType PowerUp,
    string Message,
    IReadOnlyList<char> Letters,
    int? Position = null,
    char? Letter = null,
    int TimeDeltaSeconds = 0
);
