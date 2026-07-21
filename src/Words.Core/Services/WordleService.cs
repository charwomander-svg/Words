using Words.Core.Interfaces;
using Words.Core.Models;

namespace Words.Core.Services;

public sealed class WordleService
{
    private readonly IWordService _wordService;

    public WordleService(IWordService wordService)
    {
        _wordService = wordService ?? throw new ArgumentNullException(nameof(wordService));
    }

    public WordleSession StartSession(int wordLength = 5, int maxAttempts = 6)
        => StartSession(WordleGameMode.WildDictionary, wordLength, maxAttempts);

    public WordleSession StartSession(WordleGameMode mode, int wordLength = 5, int maxAttempts = 6)
    {
        var answer = mode switch
        {
            WordleGameMode.Hard => GetClassicAnswer(wordLength),
            WordleGameMode.Daily => GetDailyAnswer(DateOnly.FromDateTime(DateTime.Now), wordLength),
            WordleGameMode.Alphabet => GetClassicAnswer(wordLength),
            WordleGameMode.Timed => GetClassicAnswer(wordLength),
            WordleGameMode.Ghost => GetClassicAnswer(wordLength),
            WordleGameMode.VsCpu => GetClassicAnswer(wordLength),
            WordleGameMode.WordsRoyale => GetClassicAnswer(wordLength),
            WordleGameMode.MegaBoard => GetClassicAnswer(wordLength),
            WordleGameMode.Survival => GetClassicAnswer(wordLength),
            WordleGameMode.RareHunt => GetRareAnswer(wordLength),
            WordleGameMode.Discovery => GetClassicAnswer(wordLength),
            WordleGameMode.Classic => GetClassicAnswer(wordLength),
            _ => _wordService.GetRandomWord(wordLength)
        };

        return new WordleSession(answer, maxAttempts);
    }

    public WordleSession StartAlphabetSession(char requiredFirstLetter, int wordLength = 5, int maxAttempts = 5)
        => new(GetAlphabetAnswer(requiredFirstLetter, wordLength), maxAttempts);

    public MegaBoardRun StartMegaBoardRun(int boardCount = 4, int wordLength = 5, int maxAttempts = 8)
    {
        if (boardCount < 2)
            throw new ArgumentOutOfRangeException(nameof(boardCount), "Mega Board needs at least two boards.");

        var candidates = GetClassicCandidates(wordLength);
        if (candidates.Length == 0)
            candidates = _wordService.GetWordsByLength(wordLength).ToArray();
        if (candidates.Length == 0)
            throw new InvalidOperationException($"No Mega Board answers found with length '{wordLength}'.");

        var uniqueAnswers = candidates
            .OrderBy(_ => Random.Shared.Next())
            .Take(Math.Min(boardCount, candidates.Length))
            .ToList();
        while (uniqueAnswers.Count < boardCount)
            uniqueAnswers.Add(candidates[Random.Shared.Next(candidates.Length)]);

        var boards = uniqueAnswers
            .Select(answer => new WordleSession(answer, maxAttempts))
            .ToList();

        return new MegaBoardRun(boards);
    }

    public MegaBoardRun StartMegaBoardRunFromAnswers(IEnumerable<string> answers, int maxAttempts = 8)
    {
        var boards = answers
            .Select(answer => new WordleSession(answer, maxAttempts))
            .ToList();
        return new MegaBoardRun(boards);
    }

    public WordleGuessResult SubmitGuess(WordleSession session, string guess)
        => SubmitGuess(session, guess, WordleGameMode.WildDictionary);

    public WordleGuessResult SubmitGuess(WordleSession session, string guess, WordleGameMode mode)
    {
        ArgumentNullException.ThrowIfNull(session);

        var normalized = guess.Trim().ToUpperInvariant();
        ValidateGuess(normalized, session.WordLength, nameof(guess));
        if (mode == WordleGameMode.Hard)
            ValidateHardModeGuess(session, normalized);

        return session.AddGuess(normalized);
    }

    public IReadOnlyList<WordleGuessResult> SubmitGuess(MegaBoardRun run, string guess)
    {
        ArgumentNullException.ThrowIfNull(run);

        var normalized = guess.Trim().ToUpperInvariant();
        ValidateGuess(normalized, run.FocusBoard.WordLength, nameof(guess));
        return run.SubmitGuess(normalized);
    }

    public WordMetadata GetWordMetadata(string word) => _wordService.GetWordMetadata(word);

    public IReadOnlyList<string> GetWordsByLength(int wordLength) => _wordService.GetWordsByLength(wordLength);

    public WordMetadataCoverage GetMetadataCoverage(int wordLength)
    {
        var words = _wordService.GetWordsByLength(wordLength);
        var tagged = words.Count(word => _wordService.GetWordMetadata(word).Commonality != WordCommonality.WildDictionary);
        return new WordMetadataCoverage(wordLength, words.Count, tagged, words.Count - tagged);
    }

    public WordleModeCoverage GetModeCoverage(WordleGameMode mode, int wordLength)
    {
        var totalWords = _wordService.GetWordsByLength(wordLength).Count;
        var candidateCount = mode switch
        {
            WordleGameMode.RareHunt => GetRareCandidates(wordLength).Length,
            WordleGameMode.WildDictionary => totalWords,
            _ => GetClassicCandidates(wordLength).Length
        };

        return new WordleModeCoverage(mode, wordLength, candidateCount, totalWords);
    }

    public WordleSession StartDailySession(DateOnly date, int wordLength = 5, int maxAttempts = 6)
        => new(GetDailyAnswer(date, wordLength), maxAttempts);

    private string GetClassicAnswer(int wordLength)
    {
        var candidates = GetClassicCandidates(wordLength);

        if (candidates.Length == 0)
            return _wordService.GetRandomWord(wordLength);

        return candidates[Random.Shared.Next(candidates.Length)];
    }

    private string GetDailyAnswer(DateOnly date, int wordLength)
    {
        var candidates = GetClassicCandidates(wordLength);
        if (candidates.Length == 0)
            candidates = _wordService.GetWordsByLength(wordLength).ToArray();
        if (candidates.Length == 0)
            throw new InvalidOperationException($"No daily answer words found with length '{wordLength}'.");

        var seed = $"{date:yyyyMMdd}:{wordLength}";
        var hash = seed.Aggregate(17, (value, ch) => unchecked((value * 31) + ch));
        var index = (int)((uint)hash % candidates.Length);
        return candidates[index];
    }

    private string GetRareAnswer(int wordLength)
    {
        var candidates = GetRareCandidates(wordLength);

        if (candidates.Length == 0)
            return _wordService.GetRandomWord(wordLength);

        return candidates[Random.Shared.Next(candidates.Length)];
    }

    private string GetAlphabetAnswer(char requiredFirstLetter, int wordLength)
    {
        var letter = char.ToUpperInvariant(requiredFirstLetter);
        if (letter is < 'A' or > 'Z')
            throw new ArgumentOutOfRangeException(nameof(requiredFirstLetter), "Required first letter must be A-Z.");

        var candidates = GetClassicCandidates(wordLength)
            .Where(word => word[0] == letter)
            .ToArray();
        if (candidates.Length == 0)
            candidates = _wordService.GetWordsByLength(wordLength)
                .Where(word => word[0] == letter)
                .ToArray();
        if (candidates.Length == 0)
            throw new InvalidOperationException($"No alphabet answers found with length {wordLength} starting with '{letter}'.");

        return candidates[Random.Shared.Next(candidates.Length)];
    }

    private string[] GetRareCandidates(int wordLength) =>
        _wordService.GetWordsByLength(wordLength)
            .Where(word =>
            {
                var metadata = _wordService.GetWordMetadata(word);
                return metadata.Commonality >= WordCommonality.Rare;
            })
            .ToArray();

    private string[] GetClassicCandidates(int wordLength) =>
        _wordService.GetWordsByLength(wordLength)
            .Where(word =>
            {
                var metadata = _wordService.GetWordMetadata(word);
                return metadata.Commonality <= WordCommonality.Rare;
            })
            .ToArray();

    private static void ValidateHardModeGuess(WordleSession session, string guess)
    {
        foreach (var previousGuess in session.Guesses)
        {
            var requiredLetters = new Dictionary<char, int>();
            for (var index = 0; index < previousGuess.Guess.Length; index++)
            {
                var letter = previousGuess.Guess[index];
                var tile = previousGuess.Tiles[index];
                if (tile == WordleTileState.Correct)
                {
                    if (guess[index] != letter)
                        throw new InvalidOperationException($"Hard Mode: position {index + 1} must be '{letter}'.");
                    requiredLetters[letter] = requiredLetters.GetValueOrDefault(letter) + 1;
                }
                else if (tile == WordleTileState.Present)
                {
                    requiredLetters[letter] = requiredLetters.GetValueOrDefault(letter) + 1;
                }
            }

            foreach (var (letter, count) in requiredLetters)
            {
                if (guess.Count(ch => ch == letter) < count)
                    throw new InvalidOperationException($"Hard Mode: guess must include '{letter}'.");
            }
        }
    }

    private void ValidateGuess(string normalized, int wordLength, string parameterName)
    {
        if (normalized.Length != wordLength)
            throw new ArgumentException($"Guess must be exactly {wordLength} letters.", parameterName);
        if (!_wordService.IsValidDictionaryWord(normalized))
            throw new InvalidOperationException($"'{normalized}' is not in the dictionary.");
    }
}
