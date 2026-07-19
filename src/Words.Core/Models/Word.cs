namespace Words.Core.Models;

/// <summary>
/// Represents a word entry in the word bank.
/// </summary>
public sealed record Word
{
    public string Text { get; }
    public WordCategory Category { get; }
    public GameDifficulty Difficulty { get; }
    public string Hint { get; }

    public Word(string text, WordCategory category, GameDifficulty difficulty, string hint)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Word text cannot be empty.", nameof(text));
        text = text.Trim();
        if (!text.All(char.IsLetter))
            throw new ArgumentException("Word text must contain letters only.", nameof(text));
        if (string.IsNullOrWhiteSpace(hint))
            throw new ArgumentException("Hint cannot be empty.", nameof(hint));

        Text = text.ToUpperInvariant();
        Category = category;
        Difficulty = difficulty;
        Hint = hint.Trim();
    }
}
