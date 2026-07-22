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
        return StartGame(player, config, null);
    }

    public GameSession StartGame(Player player, GameConfig config, Func<string, bool>? answerPredicate)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(config);

        bool MatchesWord(Word candidate, bool includeLengthConstraint)
        {
            if (answerPredicate is not null && !answerPredicate(candidate.Text))
            {
                return false;
            }

            if (includeLengthConstraint && config.PreferredWordLength > 0 && candidate.Text.Length != config.PreferredWordLength)
            {
                return false;
            }

            return true;
        }

        Word? word = null;
        const int maxAttempts = 200;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var candidate = _wordService.GetRandomWord(config.Category, config.Difficulty);
            if (MatchesWord(candidate, includeLengthConstraint: true))
            {
                word = candidate;
                break;
            }
        }

        // Fall back to any length if no matching word exists at the preferred length.
        if (word is null)
        {
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var candidate = _wordService.GetRandomWord(config.Category, config.Difficulty);
                if (MatchesWord(candidate, includeLengthConstraint: false))
                {
                    word = candidate;
                    break;
                }
            }
        }

        if (word is null)
            throw new InvalidOperationException("No matching word found for the selected mode constraints.");

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
        session.Player.RecordGameResult(session.Status == GameStatus.Won);
        if (score > 0)
            _scoreService.AwardPoints(session.Player, score);
        _sessions.Remove(sessionId);
    }
}
