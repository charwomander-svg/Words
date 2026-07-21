using Words.Core.Models;
using Words.Core.Services;

namespace Words.Tests;

public class RunProgressServiceTests
{
    [Fact]
    public void StartRun_ResetsRunAndExposesNextRound()
    {
        var run = new RunProgressService();

        run.StartRun(5, GameDifficulty.Easy, WordCategory.General, autoDifficultyRamp: true, rotateCategories: false);

        Assert.True(run.IsActive);
        Assert.Equal(5, run.RoundsPerRun);
        Assert.Equal(0, run.RoundsPlayed);
        Assert.Equal(0, run.RoundsWon);
        Assert.Equal(1, run.NextRoundNumber);
    }

    [Fact]
    public void GetDifficultyForRound_AutoRamp_IncreasesDifficulty()
    {
        var run = new RunProgressService();
        run.StartRun(6, GameDifficulty.Easy, WordCategory.General, autoDifficultyRamp: true, rotateCategories: false);

        Assert.Equal(GameDifficulty.Easy, run.GetDifficultyForRound(1));
        Assert.Equal(GameDifficulty.Medium, run.GetDifficultyForRound(4));
        Assert.Equal(GameDifficulty.Hard, run.GetDifficultyForRound(6));
    }

    [Fact]
    public void RecordRoundOutcome_TracksStreakAndCompletesRun()
    {
        var run = new RunProgressService();
        run.StartRun(3, GameDifficulty.Medium, WordCategory.General, autoDifficultyRamp: false, rotateCategories: false);

        run.RecordRoundOutcome(GameStatus.Won);
        run.RecordRoundOutcome(GameStatus.Won);
        run.RecordRoundOutcome(GameStatus.Lost);

        Assert.False(run.IsActive);
        Assert.Equal(3, run.RoundsPlayed);
        Assert.Equal(2, run.RoundsWon);
        Assert.Equal(0, run.CurrentStreak);
        Assert.Equal(2, run.BestStreak);
        Assert.Equal(0, run.NextRoundNumber);
    }

    [Fact]
    public void GetCategoryForRound_WhenRotationEnabled_CyclesFromBaseCategory()
    {
        var run = new RunProgressService();
        run.StartRun(10, GameDifficulty.Easy, WordCategory.Science, autoDifficultyRamp: false, rotateCategories: true);

        Assert.Equal(WordCategory.Science, run.GetCategoryForRound(1));
        Assert.Equal(WordCategory.Geography, run.GetCategoryForRound(2));
        Assert.Equal(WordCategory.Entertainment, run.GetCategoryForRound(3));
    }

    [Fact]
    public void GetSummary_ReturnsWinRateAndWinningRunFlag()
    {
        var run = new RunProgressService();
        run.StartRun(4, GameDifficulty.Easy, WordCategory.General, autoDifficultyRamp: false, rotateCategories: false);

        run.RecordRoundOutcome(GameStatus.Won);
        run.RecordRoundOutcome(GameStatus.Lost);
        run.RecordRoundOutcome(GameStatus.Won);
        run.RecordRoundOutcome(GameStatus.Lost);

        var summary = run.GetSummary();

        Assert.Equal(2, summary.RoundsWon);
        Assert.Equal(4, summary.RoundsPerRun);
        Assert.Equal(50, summary.WinRatePercent);
        Assert.True(summary.IsWinningRun);
        Assert.Equal(RunGrade.Silver, summary.Grade);
    }

    [Fact]
    public void RecordRoundOutcome_WinStreaksAccumulateBonusPoints()
    {
        var run = new RunProgressService();
        run.StartRun(5, GameDifficulty.Easy, WordCategory.General, autoDifficultyRamp: false, rotateCategories: false);

        run.RecordRoundOutcome(GameStatus.Won);
        run.RecordRoundOutcome(GameStatus.Won);
        run.RecordRoundOutcome(GameStatus.Won);
        run.RecordRoundOutcome(GameStatus.Won);
        run.RecordRoundOutcome(GameStatus.Won);

        var summary = run.GetSummary();

        Assert.Equal(5, summary.BestStreak);
        Assert.Equal(145, summary.StreakBonusPoints);
        Assert.True(summary.IsPerfectRun);
        Assert.Equal(RunGrade.Perfect, summary.Grade);
    }
}
