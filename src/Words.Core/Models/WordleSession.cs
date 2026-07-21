namespace Words.Core.Models;

public sealed class WordleSession
{
    private readonly List<WordleGuessResult> _guesses = [];

    public WordleSession(string answer, int maxAttempts = 6)
    {
        if (string.IsNullOrWhiteSpace(answer))
            throw new ArgumentException("Answer cannot be empty.", nameof(answer));
        if (maxAttempts < 1)
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Max attempts must be at least 1.");

        Answer = answer.Trim().ToUpperInvariant();
        WordLength = Answer.Length;
        MaxAttempts = maxAttempts;
    }

    public string Answer { get; }
    public int WordLength { get; }
    public int MaxAttempts { get; }
    public IReadOnlyList<WordleGuessResult> Guesses => _guesses;
    public GameStatus Status { get; private set; } = GameStatus.InProgress;
    public int RemainingAttempts => MaxAttempts - _guesses.Count;

    public WordleGuessResult AddGuess(string guess)
    {
        if (Status != GameStatus.InProgress)
            throw new InvalidOperationException("Wordle session is already complete.");

        guess = NormalizeGuess(guess);
        if (guess.Length != WordLength)
            throw new ArgumentException($"Guess must be exactly {WordLength} letters.", nameof(guess));

        var result = ScoreGuess(guess, Answer);
        _guesses.Add(result);

        if (result.IsCorrect)
            Status = GameStatus.Won;
        else if (_guesses.Count >= MaxAttempts)
            Status = GameStatus.Lost;

        return result;
    }

    public void Expire()
    {
        if (Status == GameStatus.InProgress)
            Status = GameStatus.Lost;
    }

    public static WordleGuessResult ScoreGuess(string guess, string answer)
    {
        guess = NormalizeGuess(guess);
        answer = NormalizeGuess(answer);

        if (guess.Length != answer.Length)
            throw new ArgumentException("Guess and answer must be the same length.");

        var tiles = Enumerable.Repeat(WordleTileState.Absent, guess.Length).ToArray();
        var remaining = new Dictionary<char, int>();

        for (var index = 0; index < answer.Length; index++)
        {
            if (guess[index] == answer[index])
            {
                tiles[index] = WordleTileState.Correct;
                continue;
            }

            remaining[answer[index]] = remaining.GetValueOrDefault(answer[index]) + 1;
        }

        for (var index = 0; index < guess.Length; index++)
        {
            if (tiles[index] == WordleTileState.Correct)
                continue;

            var letter = guess[index];
            if (!remaining.TryGetValue(letter, out var count) || count == 0)
                continue;

            tiles[index] = WordleTileState.Present;
            remaining[letter] = count - 1;
        }

        return new WordleGuessResult(guess, tiles, guess == answer);
    }

    private static string NormalizeGuess(string value) =>
        value.Trim().ToUpperInvariant();
}
