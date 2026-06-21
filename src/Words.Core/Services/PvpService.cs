using Words.Core.Interfaces;
using Words.Core.Models;

namespace Words.Core.Services;

/// <summary>
/// In-memory PvP match service.
/// </summary>
public class PvpService : IPvpService
{
    private readonly IWordService _wordService;
    private readonly Dictionary<Guid, PvpMatch> _matches = new();

    public PvpService(IWordService wordService)
    {
        _wordService = wordService ?? throw new ArgumentNullException(nameof(wordService));
    }

    /// <inheritdoc/>
    public PvpMatch StartMatch(Player playerOne, Player playerTwo, int wordLength, int? targetWins = 1)
    {
        ArgumentNullException.ThrowIfNull(playerOne);
        ArgumentNullException.ThrowIfNull(playerTwo);
        if (targetWins is < 1)
            throw new ArgumentOutOfRangeException(nameof(targetWins), "Target wins must be positive when specified.");

        var (playerOneSession, playerTwoSession) = CreateRound(playerOne, playerTwo, wordLength);

        var match = new PvpMatch(playerOne, playerTwo, playerOneSession, playerTwoSession, targetWins);

        _matches[match.Id] = match;
        return match;
    }

    /// <inheritdoc/>
    public GuessResult SubmitGuess(Guid matchId, string gamerTag, char letter)
    {
        var match = GetMatch(matchId);
        var session = match.GetSession(gamerTag);
        var result = session.Guess(letter);

        if (session.Status == GameStatus.Won && match.Status != PvpMatchStatus.Completed)
        {
            match.RecordRoundWin(session.Player);

            if (match.Status != PvpMatchStatus.Completed)
            {
                var (playerOneSession, playerTwoSession) = CreateRound(match.PlayerOne, match.PlayerTwo, match.PlayerOneSession.Config.WordLength);
                match.StartNextRound(playerOneSession, playerTwoSession);
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public PvpMatch GetMatch(Guid matchId)
    {
        if (!_matches.TryGetValue(matchId, out var match))
            throw new KeyNotFoundException($"No match found with id '{matchId}'.");

        return match;
    }

    private (GameSession PlayerOneSession, GameSession PlayerTwoSession) CreateRound(Player playerOne, Player playerTwo, int wordLength)
    {
        var word = _wordService.GetRandomWord(wordLength);
        var config = new GameConfig { WordLength = wordLength };
        var sharedWord = new Word(word, WordCategory.General, GameDifficulty.Medium, $"{wordLength}-letter word");

        return (
            new GameSession(playerOne, sharedWord, config),
            new GameSession(playerTwo, sharedWord, config)
        );
    }
}
