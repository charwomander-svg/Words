using System.Text.Json;
using Words.Core.Interfaces;
using Words.Core.Models;

namespace Words.Core.Services;

/// <summary>
/// Loads words from the embedded JSON word bank and serves random words.
/// </summary>
public class WordService : IWordService
{
    private readonly IReadOnlyList<Word> _words;
    private readonly Random _random;

    public WordService(IReadOnlyList<Word> words, Random? random = null)
    {
        _words = words ?? throw new ArgumentNullException(nameof(words));
        _random = random ?? Random.Shared;
    }

    /// <summary>
    /// Creates a <see cref="WordService"/> by loading words from a JSON file.
    /// </summary>
    public static WordService FromJsonFile(string path, Random? random = null) =>
        FromJson(File.ReadAllText(path), random);

    /// <summary>
    /// Creates a <see cref="WordService"/> from the embedded word-bank resource.
    /// </summary>
    public static WordService FromEmbeddedResource(Random? random = null)
    {
        const string resourceName = "Words.Core.Data.words.json";
        using var stream = typeof(WordService).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        return FromJson(reader.ReadToEnd(), random);
    }

    private static WordService FromJson(string json, Random? random)
    {
        var words = JsonSerializer.Deserialize<List<WordEntry>>(json)
                    ?? throw new InvalidDataException("Word bank file is empty or malformed.");

        var parsed = words.Select(e => new Word(
            e.Text.ToUpperInvariant(),
            Enum.Parse<WordCategory>(e.Category, ignoreCase: true),
            Enum.Parse<GameDifficulty>(e.Difficulty, ignoreCase: true),
            e.Hint
        )).ToList();

        return new WordService(parsed, random);
    }

    /// <inheritdoc/>
    public Word GetRandomWord(WordCategory category, GameDifficulty difficulty)
    {
        var pool = _words
            .Where(w => w.Category == category && w.Difficulty == difficulty)
            .ToList();

        if (pool.Count == 0)
            throw new InvalidOperationException(
                $"No words found for category '{category}' and difficulty '{difficulty}'.");

        return pool[_random.Next(pool.Count)];
    }

    /// <inheritdoc/>
    public IReadOnlyList<Word> GetAllWords() => _words;

    // DTO used only for JSON deserialization
    private sealed record WordEntry(string Text, string Category, string Difficulty, string Hint);
}
