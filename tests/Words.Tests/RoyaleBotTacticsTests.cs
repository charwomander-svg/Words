using Words.Core.Models;
using Words.Core.Services;

namespace Words.Tests;

public class RoyaleBotTacticsTests
{
    [Fact]
    public void ShouldSolve_IncreasesElitePressureInFinal10()
    {
        var bot = CreateBot("Elite");

        Assert.False(RoyaleBotTactics.ShouldSolve(bot, index: 2, pulse: 7, phase: "Opening"));
        Assert.True(RoyaleBotTactics.ShouldSolve(bot, index: 2, pulse: 7, phase: "Final 10"));
    }

    [Fact]
    public void ShouldFail_EliteGetsSaferInFinal10()
    {
        var bot = CreateBot("Elite");

        Assert.True(RoyaleBotTactics.ShouldFail(bot, index: 17, pulse: 11, phase: "Opening"));
        Assert.False(RoyaleBotTactics.ShouldFail(bot, index: 17, pulse: 11, phase: "Final 10"));
    }

    [Fact]
    public void ShouldSolve_StreakyBotAcceleratesOnBubble()
    {
        var bot = CreateBot("Streaky");

        Assert.False(RoyaleBotTactics.ShouldSolve(bot, index: 2, pulse: 7, phase: "Opening"));
        Assert.True(RoyaleBotTactics.ShouldSolve(bot, index: 2, pulse: 7, phase: "Bubble"));
    }

    private static RoyaleEntrant CreateBot(string persona) =>
        new RoyaleRun(["Player"], entrantCount: 10)
            .Entrants
            .First(entrant => entrant.IsBot && entrant.Persona == persona);
}
