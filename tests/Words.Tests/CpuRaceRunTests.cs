using Words.Core.Models;

namespace Words.Tests;

public class CpuRaceRunTests
{
    [Fact]
    public void RecordPlayerSolved_WhenTargetReached_PlayerWins()
    {
        var run = new CpuRaceRun(targetWords: 2);

        run.RecordPlayerSolved();
        run.RecordPlayerSolved();

        Assert.True(run.IsComplete);
        Assert.Equal("Player", run.Winner);
    }

    [Fact]
    public void Tick_WhenCpuPaceReachesTarget_CpuWins()
    {
        var run = new CpuRaceRun(targetWords: 2, cpuSecondsPerWord: 5);

        run.Tick(10);

        Assert.True(run.IsComplete);
        Assert.Equal("CPU", run.Winner);
        Assert.Equal(2, run.CpuSolved);
    }

    [Fact]
    public void Tick_SprinterPersona_GainsEarlyPressurePace()
    {
        var steady = new CpuRaceRun(targetWords: 5, cpuSecondsPerWord: 10, cpuPersona: "Steady");
        var sprinter = new CpuRaceRun(targetWords: 5, cpuSecondsPerWord: 10, cpuPersona: "Sprinter");

        steady.Tick(25);
        sprinter.Tick(25);

        Assert.True(sprinter.CpuSolved > steady.CpuSolved);
    }
}
