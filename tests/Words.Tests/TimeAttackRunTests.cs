using Words.Core.Models;

namespace Words.Tests;

public class TimeAttackRunTests
{
    [Fact]
    public void RecordSolved_AddsTimeScoreAndSolvedCount()
    {
        var run = new TimeAttackRun(startingSeconds: 180, solveBonusSeconds: 15);

        run.RecordSolved(score: 500);

        Assert.Equal(195, run.SecondsRemaining);
        Assert.Equal(1, run.WordsSolved);
        Assert.Equal(500, run.Score);
        Assert.Equal(1, run.CurrentCombo);
        Assert.Equal(1, run.BestCombo);
        Assert.Equal(500, run.LastScoreAwarded);
        Assert.Equal(0, run.LastComboBonus);
    }

    [Fact]
    public void RecordFailed_RemovesLargeTimePenalty()
    {
        var run = new TimeAttackRun(startingSeconds: 180, failurePenaltySeconds: 30);

        run.RecordFailed();

        Assert.Equal(150, run.SecondsRemaining);
        Assert.Equal(1, run.WordsFailed);
    }

    [Fact]
    public void RecordSolved_BuildsComboAndAddsBonusScore()
    {
        var run = new TimeAttackRun(startingSeconds: 180);

        run.RecordSolved(score: 500);
        run.RecordSolved(score: 500);
        run.RecordSolved(score: 500);

        Assert.Equal(3, run.CurrentCombo);
        Assert.Equal(3, run.BestCombo);
        Assert.Equal(200, run.LastComboBonus);
        Assert.Equal(700, run.LastScoreAwarded);
        Assert.Equal(1800, run.Score);
    }

    [Fact]
    public void RecordFailed_ResetsCombo()
    {
        var run = new TimeAttackRun(startingSeconds: 180);
        run.RecordSolved(score: 500);
        run.RecordSolved(score: 500);

        run.RecordFailed();

        Assert.Equal(0, run.CurrentCombo);
        Assert.Equal(2, run.BestCombo);
        Assert.Equal(0, run.LastComboBonus);
        Assert.Equal(0, run.LastScoreAwarded);
    }

    [Fact]
    public void AddSeconds_ExtendsActiveRunTimer()
    {
        var run = new TimeAttackRun(startingSeconds: 180);

        run.AddSeconds(20);

        Assert.Equal(200, run.SecondsRemaining);
    }

    [Fact]
    public void Tick_WhenTimerReachesZero_EndsRun()
    {
        var run = new TimeAttackRun(startingSeconds: 5);

        run.Tick(5);
        run.RecordSolved(score: 500);
        run.RecordFailed();

        Assert.True(run.IsOver);
        Assert.Equal(0, run.SecondsRemaining);
        Assert.Equal(0, run.WordsSolved);
        Assert.Equal(0, run.WordsFailed);
    }
}
