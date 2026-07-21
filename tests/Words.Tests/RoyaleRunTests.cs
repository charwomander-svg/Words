using Words.Core.Models;

namespace Words.Tests;

public class RoyaleRunTests
{
    [Fact]
    public void Constructor_BackfillsBotsToEntrantCount()
    {
        var run = new RoyaleRun(["Player"], entrantCount: 99);

        Assert.Equal(99, run.Entrants.Count);
        Assert.Single(run.Entrants, entrant => !entrant.IsBot);
        Assert.Equal(98, run.Entrants.Count(entrant => entrant.IsBot));
        Assert.Contains(run.Entrants, entrant => entrant.IsBot && entrant.Persona != "Human");
    }

    [Fact]
    public void RecordSolved_AddsTimeAndSolvedCount()
    {
        var run = new RoyaleRun(["Player"], entrantCount: 2, startingSeconds: 90, solveBonusSeconds: 20);

        run.RecordSolved("Player");

        var player = run.Entrants.Single(entrant => entrant.Name == "Player");
        Assert.Equal(110, player.SecondsRemaining);
        Assert.Equal(1, player.WordsSolved);
    }

    [Fact]
    public void RecordFailed_RemovesLargeTimePenalty()
    {
        var run = new RoyaleRun(["Player"], entrantCount: 2, startingSeconds: 90, failurePenaltySeconds: 45);

        run.RecordFailed("Player");

        var player = run.Entrants.Single(entrant => entrant.Name == "Player");
        Assert.Equal(45, player.SecondsRemaining);
        Assert.Equal(1, player.WordsFailed);
    }

    [Fact]
    public void AddTime_ExtendsEntrantTimer()
    {
        var run = new RoyaleRun(["Player"], entrantCount: 2, startingSeconds: 90);

        run.AddTime("Player", 10);

        var player = run.Entrants.Single(entrant => entrant.Name == "Player");
        Assert.Equal(100, player.SecondsRemaining);
    }

    [Fact]
    public void ApplyPenalty_RemovesTimeAndUpdatesPlacement()
    {
        var run = new RoyaleRun(["Player"], entrantCount: 2, startingSeconds: 10);

        run.ApplyPenalty("Player", 10);

        var player = run.Entrants.Single(entrant => entrant.Name == "Player");
        Assert.True(player.IsEliminated);
        Assert.Equal(2, run.GetPlacement("Player"));
    }

    [Fact]
    public void Tick_WhenAllButOneExpire_DeclaresWinner()
    {
        var run = new RoyaleRun(["Player"], entrantCount: 2, startingSeconds: 10, solveBonusSeconds: 20);
        run.RecordSolved("Player");

        run.Tick(10);

        Assert.True(run.IsComplete);
        Assert.Equal("Player", run.Winner?.Name);
        Assert.Equal(1, run.AliveCount);
    }

    [Fact]
    public void GetPlacement_WhenPlayerEliminated_ReturnsPlacement()
    {
        var run = new RoyaleRun(["Player"], entrantCount: 3, startingSeconds: 45, failurePenaltySeconds: 45);

        run.RecordFailed("Player");

        Assert.Equal(3, run.GetPlacement("Player"));
    }

    [Fact]
    public void GetLeaders_ReturnsAliveEntrantsBySolvedThenTime()
    {
        var run = new RoyaleRun(["Player"], entrantCount: 3, startingSeconds: 90);
        run.RecordSolved("Player");

        var leader = Assert.Single(run.GetLeaders(1));
        Assert.Equal("Player", leader.Name);
    }

    [Fact]
    public void SabotageLeaders_PenalizesLeadersExceptProtectedEntrant()
    {
        var run = new RoyaleRun(["Player"], entrantCount: 4, startingSeconds: 90);
        var firstBot = run.Entrants.First(entrant => entrant.IsBot);
        var secondBot = run.Entrants.Where(entrant => entrant.IsBot).Skip(1).First();
        run.RecordSolved("Player");
        run.RecordSolved(firstBot.Name);
        run.RecordSolved(firstBot.Name);
        run.RecordSolved(secondBot.Name);

        var targets = run.SabotageLeaders("Player", count: 2, penaltySeconds: 10);

        Assert.Equal([firstBot.Name, secondBot.Name], targets.Select(target => target.Name));
        Assert.Equal(120, run.Entrants.Single(entrant => entrant.Name == firstBot.Name).SecondsRemaining);
        Assert.Equal(100, run.Entrants.Single(entrant => entrant.Name == secondBot.Name).SecondsRemaining);
        Assert.Equal(110, run.Entrants.Single(entrant => entrant.Name == "Player").SecondsRemaining);
    }

    [Fact]
    public void GetThreats_ReturnsAliveEntrantsClosestToElimination()
    {
        var run = new RoyaleRun(["Player"], entrantCount: 3, startingSeconds: 90, failurePenaltySeconds: 45);

        run.RecordFailed("Player");

        var threat = Assert.Single(run.GetThreats(1));
        Assert.Equal("Player", threat.Name);
    }

    [Fact]
    public void GetSurgingBots_ReturnsHighSolvingBots()
    {
        var run = new RoyaleRun(["Player"], entrantCount: 4, startingSeconds: 90);
        var bot = run.Entrants.First(entrant => entrant.IsBot);
        run.RecordSolved(bot.Name);
        run.RecordSolved(bot.Name);

        var surging = Assert.Single(run.GetSurgingBots(1));
        Assert.Equal(bot.Name, surging.Name);
    }

    [Fact]
    public void GetCollapsingBots_ReturnsLowTimeBots()
    {
        var run = new RoyaleRun(["Player"], entrantCount: 4, startingSeconds: 90, failurePenaltySeconds: 45);
        var bot = run.Entrants.First(entrant => entrant.IsBot);
        run.RecordFailed(bot.Name);

        var collapsing = Assert.Single(run.GetCollapsingBots(1));
        Assert.Equal(bot.Name, collapsing.Name);
    }

    [Fact]
    public void GetNearestRival_ReturnsClosestRankedAliveEntrant()
    {
        var run = new RoyaleRun(["Player"], entrantCount: 4, startingSeconds: 90);
        var bot = run.Entrants.First(entrant => entrant.IsBot);
        run.RecordSolved("Player");
        run.RecordSolved(bot.Name);

        var rival = run.GetNearestRival("Player");

        Assert.Equal(bot.Name, rival?.Name);
    }

    [Fact]
    public void GetPlayerBubble_ReturnsNearbyRankedEntrants()
    {
        var run = new RoyaleRun(["Player"], entrantCount: 5, startingSeconds: 90);
        run.RecordSolved("Player");

        var bubble = run.GetPlayerBubble("Player", neighborsEachSide: 1);

        Assert.Contains(bubble, entrant => entrant.Name == "Player");
        Assert.True(bubble.Count <= 3);
    }

    [Fact]
    public void GetFinalTable_ReturnsPlacementOrderedFinishers()
    {
        var run = new RoyaleRun(["Player"], entrantCount: 3, startingSeconds: 45, failurePenaltySeconds: 45);
        var bots = run.Entrants.Where(entrant => entrant.IsBot).ToArray();
        run.RecordFailed(bots[0].Name);
        run.RecordFailed(bots[1].Name);

        var finalTable = run.GetFinalTable(3);

        Assert.Equal("Player", finalTable[0].Name);
        Assert.Equal(bots[1].Name, finalTable[1].Name);
        Assert.Equal(bots[0].Name, finalTable[2].Name);
    }

    [Fact]
    public void GetRank_ReturnsLeaderboardRank()
    {
        var run = new RoyaleRun(["Player"], entrantCount: 3, startingSeconds: 90);
        run.RecordSolved("Player");

        Assert.Equal(1, run.GetRank("Player"));
    }

    [Fact]
    public void GetEliminationZoneCount_CountsLowTimeAliveEntrants()
    {
        var run = new RoyaleRun(["Player"], entrantCount: 3, startingSeconds: 20, failurePenaltySeconds: 10);
        run.RecordFailed("Player");

        Assert.Equal(1, run.GetEliminationZoneCount(secondsThreshold: 15));
    }

    [Fact]
    public void Phase_ReportsBattleRoyaleStage()
    {
        var run = new RoyaleRun(["Player"], entrantCount: 11, startingSeconds: 45, failurePenaltySeconds: 45);

        Assert.Equal("Bubble", run.Phase);

        foreach (var bot in run.Entrants.Where(entrant => entrant.IsBot).Take(1))
            run.RecordFailed(bot.Name);

        Assert.Equal("Final 10", run.Phase);
        Assert.Contains("Final 10", run.PressureMessage);
    }
}
