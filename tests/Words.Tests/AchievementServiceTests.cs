using Words.Core.Models;
using Words.Core.Services;

namespace Words.Tests;

public class AchievementServiceTests
{
    [Fact]
    public void EvaluateNewAchievements_PerfectRun_ReturnsAllRunBadges()
    {
        var service = new AchievementService();
        var player = new Player("BadgeHunter");
        var summary = new RunSummary(
            RoundsWon: 5,
            RoundsPerRun: 5,
            BestStreak: 5,
            StreakBonusPoints: 175
        );

        var earned = service.EvaluateNewAchievements(player, summary);

        Assert.Contains(earned, achievement => achievement.Id == AchievementService.FirstWin.Id);
        Assert.Contains(earned, achievement => achievement.Id == AchievementService.HotStreak.Id);
        Assert.Contains(earned, achievement => achievement.Id == AchievementService.PerfectRun.Id);
        Assert.Contains(earned, achievement => achievement.Id == AchievementService.GoldRun.Id);
    }

    [Fact]
    public void EvaluateNewAchievements_AlreadyEarned_DoesNotReturnDuplicate()
    {
        var service = new AchievementService();
        var player = new Player("BadgeHunter");
        player.AddAchievement(AchievementService.FirstWin.Id);
        var summary = new RunSummary(
            RoundsWon: 1,
            RoundsPerRun: 3,
            BestStreak: 1,
            StreakBonusPoints: 0
        );

        var earned = service.EvaluateNewAchievements(player, summary);

        Assert.DoesNotContain(earned, achievement => achievement.Id == AchievementService.FirstWin.Id);
    }
}
