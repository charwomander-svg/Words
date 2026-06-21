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
    public GameSession PlayerOneSession { get; private set; }
    public GameSession PlayerTwoSession { get; private set; }
    public int? TargetWins { get; }
    public int PlayerOneWins { get; private set; }
    public int PlayerTwoWins { get; private set; }
    public int RoundNumber { get; private set; } = 1;
    public PvpMatchStatus Status { get; private set; } = PvpMatchStatus.InProgress;
    public Player? Winner { get; private set; }
    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; private set; }

    public PvpMatch(
        Player playerOne,
        Player playerTwo,
        GameSession playerOneSession,
        GameSession playerTwoSession,
        int? targetWins)
    {
        PlayerOne = playerOne ?? throw new ArgumentNullException(nameof(playerOne));
        PlayerTwo = playerTwo ?? throw new ArgumentNullException(nameof(playerTwo));
        PlayerOneSession = playerOneSession ?? throw new ArgumentNullException(nameof(playerOneSession));
        PlayerTwoSession = playerTwoSession ?? throw new ArgumentNullException(nameof(playerTwoSession));
        if (targetWins is < 1)
            throw new ArgumentOutOfRangeException(nameof(targetWins), "Target wins must be positive when specified.");

        TargetWins = targetWins;
    }

    public GameSession GetSession(string gamerTag) =>
        string.Equals(gamerTag, PlayerOne.GamerTag, StringComparison.OrdinalIgnoreCase) ? PlayerOneSession :
        string.Equals(gamerTag, PlayerTwo.GamerTag, StringComparison.OrdinalIgnoreCase) ? PlayerTwoSession :
        throw new KeyNotFoundException($"No player '{gamerTag}' is registered in match '{Id}'.");

    public void Finish(Player winner)
    {
        if (Status == PvpMatchStatus.Completed)
            return;

        Winner = winner ?? throw new ArgumentNullException(nameof(winner));
        Status = PvpMatchStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void RecordRoundWin(Player winner)
    {
        if (Status == PvpMatchStatus.Completed)
            throw new InvalidOperationException("Cannot record a round win for a completed match.");

        if (winner is null)
            throw new ArgumentNullException(nameof(winner));

        if (string.Equals(winner.GamerTag, PlayerOne.GamerTag, StringComparison.OrdinalIgnoreCase))
        {
            PlayerOneWins++;
        }
        else if (string.Equals(winner.GamerTag, PlayerTwo.GamerTag, StringComparison.OrdinalIgnoreCase))
        {
            PlayerTwoWins++;
        }
        else
        {
            throw new ArgumentException($"Player '{winner.GamerTag}' is not registered in match '{Id}'.", nameof(winner));
        }

        if (TargetWins.HasValue && (PlayerOneWins >= TargetWins.Value || PlayerTwoWins >= TargetWins.Value))
            Finish(winner);
    }

    public void StartNextRound(GameSession playerOneSession, GameSession playerTwoSession)
    {
        if (Status == PvpMatchStatus.Completed)
            throw new InvalidOperationException("Cannot start a new round for a completed match.");

        PlayerOneSession = playerOneSession ?? throw new ArgumentNullException(nameof(playerOneSession));
        PlayerTwoSession = playerTwoSession ?? throw new ArgumentNullException(nameof(playerTwoSession));
        RoundNumber++;
    }
}
