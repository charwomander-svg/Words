using Words.Core.Models;
using Words.Core.Services;

namespace Words.Tests;

public class TimeAttackResultFormatterTests
{
    [Fact]
    public void BuildCeremonyText_IncludesHighlightsAndRating()
    {
        var run = new TimeAttackRun();
        for (var index = 0; index < 5; index++)
            run.RecordSolved(score: 100);

        var text = TimeAttackResultFormatter.BuildCeremonyText(run);

        Assert.Contains("=== TIME ATTACK RESULTS ===", text);
        Assert.Contains("Best combo: x5", text);
        Assert.Contains("Rating: Hot Streak", text);
        Assert.Contains("Highlights:", text);
    }

    [Fact]
    public void BuildSummaryText_IncludesRunRecordsAndRating()
    {
        var run = new TimeAttackRun();
        for (var index = 0; index < 5; index++)
            run.RecordSolved(score: 100);

        var text = TimeAttackResultFormatter.BuildSummaryText(run);

        Assert.Contains("TIME ATTACK COMPLETE", text);
        Assert.Contains("Score:", text);
        Assert.Contains("Solved: 5", text);
        Assert.Contains("Best combo: x5", text);
        Assert.Contains("Rating: Hot Streak", text);
    }

    [Fact]
    public void BuildShareText_UsesTimeAttackHeader()
    {
        var run = new TimeAttackRun();
        run.RecordSolved(score: 100);

        var text = TimeAttackResultFormatter.BuildShareText(run);

        Assert.StartsWith("Words Time Attack", text);
    }
}
