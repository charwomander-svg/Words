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
    private readonly IReadOnlyDictionary<string, Word> _curatedWordsByText;
    private readonly IReadOnlyDictionary<string, WordMetadata> _metadataByText;
    private readonly HashSet<string> _dictionaryWords;
    private readonly Random _random;

    public WordService(IReadOnlyList<Word> words, Random? random = null)
        : this(words, BuildWordsByLength(words), new Dictionary<string, WordMetadata>(), random)
    {
    }

    private WordService(
        IReadOnlyList<Word> words,
        IReadOnlyDictionary<int, string[]> wordsByLength,
        IReadOnlyDictionary<string, WordMetadata> metadataByText,
        Random? random = null)
    {
        _words = words ?? throw new ArgumentNullException(nameof(words));
        _wordsByLength = wordsByLength ?? throw new ArgumentNullException(nameof(wordsByLength));
        _metadataByText = metadataByText ?? throw new ArgumentNullException(nameof(metadataByText));
        _curatedWordsByText = _words
            .GroupBy(word => word.Text, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        _dictionaryWords = _wordsByLength.Values
            .SelectMany(wordsForLength => wordsForLength)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
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

        return new WordService(parsed, LoadDictionaryWords(), LoadWordMetadata(), random);
    }

    private static IReadOnlyDictionary<int, string[]> BuildWordsByLength(IEnumerable<Word> words) =>
        words
            .Select(word => word.Text)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .GroupBy(text => text.Length)
            .ToDictionary(group => group.Key, group => group.ToArray());

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
            var word = line.Trim().ToUpperInvariant();
            if (word.Length is < 4 or > 20 || word.Any(ch => ch is < 'A' or > 'Z'))
                continue;

            if (!byLength.TryGetValue(word.Length, out var words))
            {
                words = [];
                byLength[word.Length] = words;
            }

            words.Add(word);
        }

        return byLength.ToDictionary(pair => pair.Key, pair => pair.Value.Distinct().ToArray());
    }

    private static IReadOnlyDictionary<string, WordMetadata> LoadWordMetadata()
    {
        const string resourceName = "Words.Core.Data.word-metadata.tsv";
        using var stream = typeof(WordService).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);

        var metadata = new Dictionary<string, WordMetadata>(StringComparer.OrdinalIgnoreCase);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split('\t', 3);
            if (parts.Length != 3)
                throw new InvalidDataException($"Word metadata row is malformed: '{line}'.");

            var word = parts[0].Trim().ToUpperInvariant();
            if (word.Length == 0 || word.Any(ch => ch is < 'A' or > 'Z'))
                throw new InvalidDataException($"Word metadata key is invalid: '{parts[0]}'.");

            var commonality = Enum.Parse<WordCommonality>(parts[1].Trim(), ignoreCase: true);
            metadata[word] = new WordMetadata(word, commonality, ToLabel(commonality), parts[2].Trim());
        }

        return metadata;
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
    public string GetRandomWord(int length) => GetRandomWords(length, 1)[0];

    /// <inheritdoc/>
    public IReadOnlyList<string> GetRandomWords(int length, int count)
    {
        if (length is < 4 or > 20)
            throw new ArgumentOutOfRangeException(nameof(length), "Word length must be between 4 and 20 letters.");
        if (count is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(count), "Word count must be between 1 and 100.");

        var pool = GetWordsByLength(length);
        if (pool.Count == 0)
            throw new InvalidOperationException($"No words found with length '{length}'.");
        if (count > pool.Count)
            throw new InvalidOperationException($"Requested {count} words of length {length}, but only {pool.Count} are available.");

        return pool
            .OrderBy(_ => _random.Next())
            .Take(count)
            .ToArray();
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetWordsByLength(int length)
    {
        if (length is < 4 or > 20)
            throw new ArgumentOutOfRangeException(nameof(length), "Word length must be between 4 and 20 letters.");

        return _wordsByLength.TryGetValue(length, out var words)
            ? words
            : Array.Empty<string>();
    }

    /// <inheritdoc/>
    public bool IsValidDictionaryWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return false;

        return _dictionaryWords.Contains(word.Trim().ToUpperInvariant());
    }

    /// <inheritdoc/>
    public WordMetadata GetWordMetadata(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            throw new ArgumentException("Word cannot be empty.", nameof(word));

        var normalized = word.Trim().ToUpperInvariant();
        if (_metadataByText.TryGetValue(normalized, out var metadata))
            return metadata;

        if (_curatedWordsByText.TryGetValue(normalized, out var curated))
        {
            var commonality = curated.Difficulty switch
            {
                GameDifficulty.Easy => WordCommonality.Common,
                GameDifficulty.Medium => WordCommonality.Uncommon,
                _ => WordCommonality.Rare
            };

            return new WordMetadata(
                normalized,
                commonality,
                ToLabel(commonality),
                curated.Hint);
        }

        if (!_dictionaryWords.Contains(normalized))
            throw new InvalidOperationException($"'{normalized}' is not in the dictionary.");

        return new WordMetadata(
            normalized,
            WordCommonality.WildDictionary,
            ToLabel(WordCommonality.WildDictionary),
            "Deep dictionary word. Expect obscure, technical, archaic, or specialized vocabulary.");
    }

    private static string ToLabel(WordCommonality commonality) => commonality switch
    {
        WordCommonality.Common => "Common",
        WordCommonality.Uncommon => "Uncommon",
        WordCommonality.Rare => "Rare",
        WordCommonality.Obscure => "Obscure",
        _ => "Wild Dictionary"
    };

    // DTO used only for JSON deserialization
    private sealed record WordEntry(string Text, string Category, string Difficulty, string Hint);
}
