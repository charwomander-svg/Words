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

        var word = _wordService.GetRandomWord(config.Category, config.Difficulty);
        var session = new GameSession(player, word, config);
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
    public GuessResult SubmitWordGuess(Guid sessionId, string word)
    {
        var session = GetSession(sessionId);
        var result = session.GuessWord(word);

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
    public GameSessionState GetSessionState(Guid sessionId) => GetSession(sessionId).ToState();

    /// <inheritdoc/>
    public void EndGame(Guid sessionId)
    {
        var session = GetSession(sessionId);
        int score = session.CalculateScore();
        session.Player.RecordGameResult(session.Status == GameStatus.Won);
        if (score > 0)
            _scoreService.AwardPoints(session.Player, score);
        _sessions.Remove(sessionId);
    }
}
