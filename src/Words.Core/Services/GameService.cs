using Words.Core.Interfaces;
using Words.Core.Models;

namespace Words.Core.Services;

/// <summary>
/// Manages game session lifecycle: start, guess, and end.
/// </summary>
public class GameService : IGameService
{
    private readonly IWordService _wordService;
    private readonly IScoreService _scoreService;
    private readonly Dictionary<Guid, GameSession> _sessions = new();

    public GameService(IWordService wordService, IScoreService scoreService)
    {
        _wordService = wordService ?? throw new ArgumentNullException(nameof(wordService));
        _scoreService = scoreService ?? throw new ArgumentNullException(nameof(scoreService));
    }

    /// <inheritdoc/>
    public GameSession StartGame(Player player, GameConfig config)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(config);
        config.Validate();

        var words = _wordService
            .GetRandomWords(config.WordLength, config.ConcurrentWords)
            .Select(text => new Word(text, WordCategory.General, GameDifficulty.Medium, $"{config.WordLength}-letter word"))
            .ToList();

        var session = new GameSession(player, words, config);
        _sessions[session.Id] = session;
        return session;
    }

    /// <inheritdoc/>
    public GuessResult SubmitGuess(Guid sessionId, char letter)
    {
        var session = GetSession(sessionId);
        var result = session.Guess(letter);

        if (session.Status != GameStatus.InProgress)
            EndGame(sessionId);

        return result;
    }

    /// <inheritdoc/>
    public GameSession GetSession(Guid sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            throw new KeyNotFoundException($"No active session with id '{sessionId}'.");
        return session;
    }

    /// <inheritdoc/>
    public void EndGame(Guid sessionId)
    {
        var session = GetSession(sessionId);
        int score = session.CalculateScore();

        if (session.Status == GameStatus.Won)
            session.Player.AddExperience(CalculateExperience(session.Config.WordLength, session.Config.ConcurrentWords));

        session.Player.RecordGameResult(session.Status == GameStatus.Won);
        if (score > 0)
            _scoreService.AwardPoints(session.Player, score);

        if (session.Status == GameStatus.Won && session.Config.WordLength == 5)
            session.Player.RecordFiveLetterWordSolvedStartingWith(session.PrimaryWord.Text);

        if (session.Status == GameStatus.Won
            && session.Config.WordLength == 10
            && session.HintsUsed == 0)
        {
            session.Player.RecordTenLetterWordSolvedWithoutHints();
        }

        _sessions.Remove(sessionId);
    }

    private static int CalculateExperience(int wordLength, int concurrentWords) =>
        Math.Max(10, wordLength * 10 + Math.Max(0, concurrentWords - 1) * 15);
}
