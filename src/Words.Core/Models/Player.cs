namespace Words.Core.Models;

/// <summary>
/// Represents a player in the game.
/// </summary>
public class Player
{
    private static readonly (int Level, string Cosmetic)[] CosmeticUnlocks =
    [
        (1, "Classic tiles"),
        (2, "Retro title"),
        (3, "Neon background"),
        (4, "Arcade music"),
        (5, "Champion border")
    ];

    public string GamerTag { get; }
    public int Score { get; private set; }
    public int ExperiencePoints { get; private set; }
    public int GamesPlayed { get; private set; }
    public int GamesWon { get; private set; }
    public int Rank => 1 + ExperiencePoints / 100;
    public string RankTitle => Rank switch
    {
        1 => "Rookie",
        2 => "Scout",
        3 => "Hunter",
        4 => "Ace",
        5 => "Champion",
        _ => "Legend"
    };
    public IReadOnlyList<string> UnlockedCosmetics =>
        CosmeticUnlocks.Where(unlock => Rank >= unlock.Level).Select(unlock => unlock.Cosmetic).ToArray();

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

    public void AddExperience(int points)
    {
        if (points < 0)
            throw new ArgumentOutOfRangeException(nameof(points), "Experience must be non-negative.");

        ExperiencePoints += points;
    }

    public void RecordGameResult(bool won)
    {
        GamesPlayed++;
        if (won) GamesWon++;
    }
}
