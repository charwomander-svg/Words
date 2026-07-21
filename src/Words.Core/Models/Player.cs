namespace Words.Core.Models;

/// <summary>
/// Represents a player in the game.
/// </summary>
public class Player
{
    private readonly HashSet<string> _achievementIds = new(StringComparer.OrdinalIgnoreCase);

    public string GamerTag { get; }
    public int Score { get; private set; }
    public int GamesPlayed { get; private set; }
    public int GamesWon { get; private set; }
    public IReadOnlySet<string> AchievementIds => _achievementIds;

    public Player(string gamerTag)
    {
        if (string.IsNullOrWhiteSpace(gamerTag))
            throw new ArgumentException("GamerTag cannot be empty.", nameof(gamerTag));

        GamerTag = gamerTag;
    }

    public void AddScore(int points)
    {
        if (points < 0)
            throw new ArgumentOutOfRangeException(nameof(points), "Points must be non-negative.");
        Score += points;
    }

    public void RecordGameResult(bool won)
    {
        GamesPlayed++;
        if (won) GamesWon++;
    }

    public bool HasAchievement(string achievementId) =>
        _achievementIds.Contains(achievementId);

    public bool AddAchievement(string achievementId)
    {
        if (string.IsNullOrWhiteSpace(achievementId))
            throw new ArgumentException("Achievement id cannot be empty.", nameof(achievementId));

        return _achievementIds.Add(achievementId);
    }
}
