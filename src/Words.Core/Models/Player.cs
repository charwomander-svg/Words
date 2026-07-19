namespace Words.Core.Models;

/// <summary>
/// Represents a player in the game.
/// </summary>
public class Player
{
    public string GamerTag { get; }
    public int Score { get; private set; }
    public int GamesPlayed { get; private set; }
    public int GamesWon { get; private set; }

    public Player(string gamerTag)
    {
        if (string.IsNullOrWhiteSpace(gamerTag))
            throw new ArgumentException("GamerTag cannot be empty.", nameof(gamerTag));

        GamerTag = gamerTag.Trim();
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
}
