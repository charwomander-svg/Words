using Words.Core.Models;

namespace Words.Core.Services;

public sealed class AchievementService
{
    public static readonly Achievement FirstWin = new(
        "first-win",
        "First Win",
        "Win your first round."
    );

    public static readonly Achievement HotStreak = new(
        "hot-streak",
        "Hot Streak",
        "Build a 3-round win streak."
    );

    public static readonly Achievement PerfectRun = new(
        "perfect-run",
        "Perfect Run",
        "Win every round in a run."
    );

    public static readonly Achievement GoldRun = new(
        "gold-run",
        "Gold Run",
        "Finish a run with a Gold grade or better."
    );

    public static readonly IReadOnlyList<Achievement> All = new[]
    {
        FirstWin,
        HotStreak,
        PerfectRun,
        GoldRun
    };

    public IReadOnlyList<Achievement> EvaluateNewAchievements(Player player, RunSummary summary)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(summary);

        var earned = new List<Achievement>();

        AddIfNew(summary.RoundsWon > 0, FirstWin);
        AddIfNew(summary.BestStreak >= 3, HotStreak);
        AddIfNew(summary.IsPerfectRun, PerfectRun);
        AddIfNew(summary.Grade is RunGrade.Gold or RunGrade.Perfect, GoldRun);

        return earned;

        void AddIfNew(bool condition, Achievement achievement)
        {
            if (condition && !player.HasAchievement(achievement.Id))
                earned.Add(achievement);
        }
    }
}
