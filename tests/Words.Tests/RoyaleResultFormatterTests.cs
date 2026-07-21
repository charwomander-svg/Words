using Words.Core.Models;
using Words.Core.Services;

namespace Words.Tests;

public class RoyaleResultFormatterTests
{
    [Fact]
    public void BuildShareText_IncludesPlacementAndLeaders()
    {
        var run = new RoyaleRun(["Player"], entrantCount: 3, startingSeconds: 90);
        run.RecordSolved("Player");

        var text = RoyaleResultFormatter.BuildShareText(run, "Player");

        Assert.Contains("Words Royale", text);
        Assert.Contains("Placement:", text);
        Assert.Contains("Solved: 1", text);
        Assert.Contains("Phase:", text);
        Assert.Contains("Final table:", text);
    }

    [Fact]
    public void BuildCeremonyText_IncludesResultWinnerAndTopFinishers()
    {
        var run = new RoyaleRun(["Player"], entrantCount: 3, startingSeconds: 45, failurePenaltySeconds: 45);
        foreach (var bot in run.Entrants.Where(entrant => entrant.IsBot))
            run.RecordFailed(bot.Name);

        var text = RoyaleResultFormatter.BuildCeremonyText(run, "Player");

        Assert.Contains("WORDS ROYALE VICTORY", text);
        Assert.Contains("=== Player Card ===", text);
        Assert.Contains("Placement: #1/3", text);
        Assert.Contains("Winner:", text);
        Assert.Contains("=== Podium ===", text);
        Assert.Contains("[GOLD] Player", text);
        Assert.Contains("=== Final Table ===", text);
        Assert.Contains("#1 Player", text);
    }

    [Fact]
    public void BuildLiveDramaText_IncludesReadableDramaSections()
    {
        var run = new RoyaleRun(["Player"], entrantCount: 5, startingSeconds: 90);
        run.RecordSolved("Player");

        var text = RoyaleResultFormatter.BuildLiveDramaText(run, "Player");

        Assert.Contains("alive | rank #", text);
        Assert.Contains("Leaders:", text);
        Assert.Contains("Danger:", text);
        Assert.Contains("Rival:", text);
        Assert.Contains("Surging:", text);
        Assert.Contains("Collapsing:", text);
        Assert.Contains("Bubble:", text);
    }
}
