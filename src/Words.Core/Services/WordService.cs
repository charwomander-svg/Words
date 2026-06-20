using System.Text.Json;
using System.IO.Compression;
using Words.Core.Interfaces;
using Words.Core.Models;

namespace Words.Core.Services;

/// <summary>
/// Loads words from the embedded JSON word bank and serves random words.
/// </summary>
public class WordService : IWordService
{
    private readonly IReadOnlyList<Word> _words;
    private readonly IReadOnlyDictionary<int, string[]> _wordsByLength;
    private readonly Random _random;

    public WordService(IReadOnlyList<Word> words, Random? random = null)
        : this(words, new Dictionary<int, string[]>(), random)
    {
    }

    private WordService(
        IReadOnlyList<Word> words,
        IReadOnlyDictionary<int, string[]> wordsByLength,
        Random? random = null)
    {
        _words = words ?? throw new ArgumentNullException(nameof(words));
        _wordsByLength = wordsByLength ?? throw new ArgumentNullException(nameof(wordsByLength));
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

        return new WordService(parsed, LoadDictionaryWords(), random);
    }

    private static IReadOnlyDictionary<int, string[]> LoadDictionaryWords()
    {
        const string resourceName = "Words.Core.Data.english-words-4to20.txt.gz";
        using var stream = typeof(WordService).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
        using var gzip = new GZipStream(stream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);

        var byLength = new Dictionary<int, List<string>>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length == 0)
                continue;

            if (!byLength.TryGetValue(line.Length, out var words))
            {
                words = [];
                byLength[line.Length] = words;
            }

            words.Add(line);
        }

        return byLength.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
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

    /// <inheritdoc/>
    public string GetRandomWord(int length)
    {
        if (length < 4 || length > 20)
            throw new ArgumentOutOfRangeException(nameof(length), "Word length must be between 4 and 20 letters.");

        var pool = GetWordsByLength(length);
        if (pool.Count == 0)
            throw new InvalidOperationException($"No words found with length '{length}'.");

        return pool[_random.Next(pool.Count)];
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetWordsByLength(int length)
    {
        if (length < 4 || length > 20)
            throw new ArgumentOutOfRangeException(nameof(length), "Word length must be between 4 and 20 letters.");

        return _wordsByLength.TryGetValue(length, out var words)
            ? words
            : Array.Empty<string>();
    }

    // DTO used only for JSON deserialization
    private sealed record WordEntry(string Text, string Category, string Difficulty, string Hint);
}
