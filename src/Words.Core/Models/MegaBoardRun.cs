namespace Words.Core.Models;

public sealed class MegaBoardRun
{
    private readonly List<WordleSession> _boards;

    public MegaBoardRun(IEnumerable<WordleSession> boards)
    {
        _boards = boards?.ToList() ?? throw new ArgumentNullException(nameof(boards));
        if (_boards.Count < 2)
            throw new ArgumentException("Mega Board needs at least two boards.", nameof(boards));
        if (_boards.Select(board => board.WordLength).Distinct().Count() != 1)
            throw new ArgumentException("All Mega Board answers must use the same word length.", nameof(boards));
        if (_boards.Select(board => board.MaxAttempts).Distinct().Count() != 1)
            throw new ArgumentException("All Mega Board boards must use the same attempt count.", nameof(boards));
    }

    public IReadOnlyList<WordleSession> Boards => _boards;
    public int BoardCount => _boards.Count;
    public int SolvedCount => _boards.Count(board => board.Status == GameStatus.Won);
    public int LostCount => _boards.Count(board => board.Status == GameStatus.Lost);
    public int DangerCount => _boards.Count(board => board.Status == GameStatus.InProgress && board.RemainingAttempts <= 2);
    public bool IsComplete => _boards.All(board => board.Status != GameStatus.InProgress);
    public bool IsPerfectClear => IsComplete && LostCount == 0;

    public WordleSession FocusBoard =>
        GetSmartFocusBoard()
        ?? _boards.FirstOrDefault(board => board.Status == GameStatus.Won)
        ?? _boards[0];

    public int CalculateScore()
    {
        var clearBonus = SolvedCount * _boards[0].WordLength * 100;
        var scaleBonus = BoardCount switch
        {
            >= 100 => 25000,
            >= 50 => 10000,
            >= 25 => 5000,
            >= 16 => 2500,
            _ => 1000
        };
        var perfectBonus = IsPerfectClear ? BoardCount * _boards[0].WordLength * 50 : 0;
        return clearBonus + (SolvedCount == BoardCount ? scaleBonus : 0) + perfectBonus;
    }

    public string BuildKnownPattern(WordleSession board)
    {
        ArgumentNullException.ThrowIfNull(board);
        var pattern = Enumerable.Repeat('_', board.WordLength).ToArray();
        foreach (var guess in board.Guesses)
        {
            for (var index = 0; index < guess.Tiles.Count; index++)
            {
                if (guess.Tiles[index] == WordleTileState.Correct)
                    pattern[index] = guess.Guess[index];
            }
        }

        return string.Join(' ', pattern);
    }

    public IReadOnlyDictionary<char, WordleTileState> BuildAggregateKeyboardState()
    {
        var letters = new Dictionary<char, WordleTileState>();
        foreach (var guess in _boards.SelectMany(board => board.Guesses))
        {
            for (var index = 0; index < guess.Guess.Length; index++)
            {
                var letter = guess.Guess[index];
                var state = guess.Tiles[index];
                if (letters.TryGetValue(letter, out var current) && GetRank(current) >= GetRank(state))
                    continue;

                letters[letter] = state;
            }
        }

        return letters;
    }

    public IReadOnlyList<WordleSession> GetEndangeredBoards(int count = 5)
    {
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be at least 1.");

        return _boards
            .Where(board => board.Status == GameStatus.InProgress)
            .OrderBy(board => board.RemainingAttempts)
            .ThenByDescending(GetCorrectTileCount)
            .Take(count)
            .ToArray();
    }

    public IReadOnlyList<WordleGuessResult> SubmitGuess(string guess)
    {
        if (IsComplete)
            throw new InvalidOperationException("Mega Board run is already complete.");

        return _boards
            .Where(board => board.Status == GameStatus.InProgress)
            .Select(board => board.AddGuess(guess))
            .ToArray();
    }

    public void ReplaceBoard(WordleSession currentBoard, WordleSession replacement)
    {
        ArgumentNullException.ThrowIfNull(currentBoard);
        ArgumentNullException.ThrowIfNull(replacement);
        if (replacement.WordLength != _boards[0].WordLength)
            throw new ArgumentException("Replacement board must use the same word length.", nameof(replacement));
        if (replacement.MaxAttempts != _boards[0].MaxAttempts)
            throw new ArgumentException("Replacement board must use the same attempt count.", nameof(replacement));

        var index = _boards.FindIndex(board => ReferenceEquals(board, currentBoard));
        if (index < 0)
            throw new InvalidOperationException("Mega Board replacement target was not found.");

        _boards[index] = replacement;
    }

    private WordleSession? GetSmartFocusBoard() =>
        _boards
            .Where(board => board.Status == GameStatus.InProgress)
            .OrderByDescending(GetCorrectTileCount)
            .ThenBy(board => board.RemainingAttempts)
            .FirstOrDefault();

    private static int GetCorrectTileCount(WordleSession board) =>
        board.Guesses.Sum(guess => guess.Tiles.Count(tile => tile == WordleTileState.Correct));

    private static int GetRank(WordleTileState state) => state switch
    {
        WordleTileState.Correct => 3,
        WordleTileState.Present => 2,
        WordleTileState.Absent => 1,
        _ => 0
    };
}
