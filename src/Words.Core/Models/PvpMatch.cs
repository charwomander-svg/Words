namespace Words.Core.Models;

/// <summary>
/// The lifecycle state of a PvP match.
/// </summary>
public enum PvpMatchStatus
{
    Waiting,
    InProgress,
    Completed
}

/// <summary>
/// Represents a match where two players race to solve the same word.
/// </summary>
public class PvpMatch
{
    public Guid Id { get; } = Guid.NewGuid();
    public Player PlayerOne { get; }
    public Player PlayerTwo { get; }
    public GameSession PlayerOneSession { get; }
    public GameSession PlayerTwoSession { get; }
    public PvpMatchStatus Status { get; private set; } = PvpMatchStatus.InProgress;
    public Player? Winner { get; private set; }
    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; private set; }

    public PvpMatch(Player playerOne, Player playerTwo, GameSession playerOneSession, GameSession playerTwoSession)
    {
        PlayerOne = playerOne ?? throw new ArgumentNullException(nameof(playerOne));
        PlayerTwo = playerTwo ?? throw new ArgumentNullException(nameof(playerTwo));
        PlayerOneSession = playerOneSession ?? throw new ArgumentNullException(nameof(playerOneSession));
        PlayerTwoSession = playerTwoSession ?? throw new ArgumentNullException(nameof(playerTwoSession));
    }

    public GameSession GetSession(string gamerTag) =>
        string.Equals(gamerTag, PlayerOne.GamerTag, StringComparison.OrdinalIgnoreCase) ? PlayerOneSession :
        string.Equals(gamerTag, PlayerTwo.GamerTag, StringComparison.OrdinalIgnoreCase) ? PlayerTwoSession :
        throw new KeyNotFoundException($"No player '{gamerTag}' is registered in match '{Id}'.");

    public void Finish(Player winner)
    {
        Winner = winner ?? throw new ArgumentNullException(nameof(winner));
        Status = PvpMatchStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
