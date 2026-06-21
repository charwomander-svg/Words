namespace Words.Core.Models;

/// <summary>
/// Result returned when the player requests a hint.
/// </summary>
public record HintResult(
    bool IsAvailable,
    char Letter,
    string Message
);
