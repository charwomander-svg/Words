using Words.Core.Models;
using Words.Core.Services;

namespace Words.Tests;

public class CpuRaceResultFormatterTests
{
    [Fact]
    public void BuildCeremonyText_IncludesWinnerPersonaAndRating()
    {
        var run = new CpuRaceRun(targetWords: 2, cpuSecondsPerWord: 50, cpuPersona: "Closer");
        run.RecordPlayerSolved();
        run.RecordPlayerSolved();

        var text = CpuRaceResultFormatter.BuildCeremonyText(run);

        Assert.Contains("=== VS CPU RESULTS ===", text);
        Assert.Contains("Winner: Player", text);
        Assert.Contains("Persona: Closer", text);
        Assert.Contains("Rating:", text);
    }

    [Fact]
    public void BuildShareText_UsesVsCpuHeader()
    {
        var run = new CpuRaceRun(targetWords: 1, cpuSecondsPerWord: 1);
        run.Tick(1);

        var text = CpuRaceResultFormatter.BuildShareText(run);

        Assert.StartsWith("Words Vs. CPU", text);
    }
}
