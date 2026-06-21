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
    private readonly HashSet<string> _unlockedAchievements = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _achievementOrder = [];
    private bool _creditsViewed;
    private bool _firstHeadToHeadWin;
    private bool _tenLetterWordSolvedWithoutHints;

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
    public IReadOnlyList<string> UnlockedAchievements => _achievementOrder.AsReadOnly();

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
        UpdateAchievements();
    }

    public void AddExperience(int points)
    {
        if (points < 0)
            throw new ArgumentOutOfRangeException(nameof(points), "Experience must be non-negative.");

        ExperiencePoints += points;
        UpdateAchievements();
    }

    public void RecordGameResult(bool won)
    {
        GamesPlayed++;
        if (won) GamesWon++;
        UpdateAchievements();
    }

    public void RecordCreditsViewed()
    {
        _creditsViewed = true;
        UpdateAchievements();
    }

    public void RecordHeadToHeadWin()
    {
        _firstHeadToHeadWin = true;
        UpdateAchievements();
    }

    public void RecordTenLetterWordSolvedWithoutHints()
    {
        _tenLetterWordSolvedWithoutHints = true;
        UpdateAchievements();
    }

    private void UpdateAchievements()
    {
        UnlockAchievement("First Win", GamesWon >= 1);
        UnlockAchievement("Triple Threat", GamesWon >= 3);
        UnlockAchievement("Century Club", Score >= 100);
        UnlockAchievement("Arcade Veteran", Score >= 500);
        UnlockAchievement("Rising Star", Rank >= 3);
        UnlockAchievement("Legend", Rank >= 5);
        UnlockAchievement("Backstage Pass", _creditsViewed);
        UnlockAchievement("Head-to-Head Challenger", _firstHeadToHeadWin);
        UnlockAchievement("No-Hint Ten", _tenLetterWordSolvedWithoutHints);
    }

    private void UnlockAchievement(string achievement, bool condition)
    {
        if (!condition || !_unlockedAchievements.Add(achievement))
            return;

        _achievementOrder.Add(achievement);
    }
}
