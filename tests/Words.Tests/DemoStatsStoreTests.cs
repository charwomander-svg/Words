using Words.Core.Services;
using Words.Core.Models;
using System.Text.Json;

namespace Words.Tests;

public class DemoStatsStoreTests
{
    [Fact]
    public void Save_PersistsVersionedEnvelope()
    {
        var stats = new DemoStatsStore
        {
            GamesPlayed = 7,
            LastMode = WordleGameMode.RareHunt.ToString()
        };
        var path = Path.Combine(Path.GetTempPath(), $"demo-stats-{Guid.NewGuid():N}.json");

        try
        {
            stats.Save(path);
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            Assert.Equal(1, document.RootElement.GetProperty("schemaVersion").GetInt32());
            Assert.Equal(7, document.RootElement.GetProperty("stats").GetProperty("gamesPlayed").GetInt32());
            Assert.Equal(WordleGameMode.RareHunt.ToString(), document.RootElement.GetProperty("stats").GetProperty("lastMode").GetString());
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void Load_HandlesVersionedEnvelope()
    {
        var path = Path.Combine(Path.GetTempPath(), $"demo-stats-{Guid.NewGuid():N}.json");
        var json = """
                   {
                     "schemaVersion": 1,
                     "stats": {
                       "gamesPlayed": 12,
                       "wins": 8,
                       "lastMode": "Timed",
                       "lastWordLength": 6
                     }
                   }
                   """;

        try
        {
            File.WriteAllText(path, json);
            var loaded = DemoStatsStore.Load(path);
            Assert.Equal(12, loaded.GamesPlayed);
            Assert.Equal(8, loaded.Wins);
            Assert.Equal("Timed", loaded.LastMode);
            Assert.Equal(6, loaded.LastWordLength);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void Load_HandlesLegacyPayload()
    {
        var path = Path.Combine(Path.GetTempPath(), $"demo-stats-{Guid.NewGuid():N}.json");
        var json = """
                   {
                     "gamesPlayed": 3,
                     "wins": 2,
                     "lastMode": "Classic",
                     "lastWordLength": 5
                   }
                   """;

        try
        {
            File.WriteAllText(path, json);
            var loaded = DemoStatsStore.Load(path);
            Assert.Equal(3, loaded.GamesPlayed);
            Assert.Equal(2, loaded.Wins);
            Assert.Equal("Classic", loaded.LastMode);
            Assert.Equal(5, loaded.LastWordLength);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void Load_CorruptJson_ReturnsDefaultStore()
    {
        var path = Path.Combine(Path.GetTempPath(), $"demo-stats-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, "{ definitely-not-json");
            var loaded = DemoStatsStore.Load(path);
            Assert.Equal(0, loaded.GamesPlayed);
            Assert.Equal(WordleGameMode.Classic.ToString(), loaded.LastMode);
            Assert.Equal(5, loaded.LastWordLength);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void RecordRoyalePlacement_KeepsBestPlacement()
    {
        var stats = new DemoStatsStore();

        stats.RecordRoyalePlacement(12);
        stats.RecordRoyalePlacement(4);
        stats.RecordRoyalePlacement(18);

        Assert.Equal(4, stats.BestRoyalePlacement);
        Assert.Equal(3, stats.RoyaleRuns);
    }

    [Fact]
    public void RecordRoyalePlacement_FirstPlaceCountsWin()
    {
        var stats = new DemoStatsStore();

        stats.RecordRoyalePlacement(1);

        Assert.Equal(1, stats.BestRoyalePlacement);
        Assert.Equal(1, stats.RoyaleWins);
        Assert.Equal(1, stats.RoyaleTopTenFinishes);
        Assert.Equal(1, stats.PowerUps[nameof(PowerUpType.Crown)]);
        Assert.Equal(1, stats.PowerUps[nameof(PowerUpType.Shield)]);
    }

    [Fact]
    public void RecordRoyaleRun_TracksTopTenBestSolvedAndRewards()
    {
        var stats = new DemoStatsStore();
        var run = new RoyaleRun(["Player"], entrantCount: 10, startingSeconds: 45, solveBonusSeconds: 20);

        for (var index = 0; index < 10; index++)
            run.RecordSolved("Player");

        stats.RecordRoyaleRun(run, "Player");

        Assert.Equal(1, stats.RoyaleRuns);
        Assert.Equal(1, stats.RoyaleTopTenFinishes);
        Assert.Equal(10, stats.BestRoyaleSolved);
        Assert.Equal(1, stats.PowerUps[nameof(PowerUpType.Shield)]);
        Assert.Equal(1, stats.PowerUps[nameof(PowerUpType.Clock)]);
        Assert.Equal(1, stats.PowerUps[nameof(PowerUpType.Freeze)]);
    }

    [Fact]
    public void RecordRound_FastWin_AddsPowerUps()
    {
        var stats = new DemoStatsStore();
        var session = new WordleSession("CRANE");
        session.AddGuess("CRANE");

        stats.RecordRound(session, WordleGameMode.Classic, new WordMetadata("CRANE", WordCommonality.Common, "Common", "test"));

        Assert.Equal(1, stats.PowerUps[nameof(PowerUpType.Crown)]);
    }

    [Fact]
    public void RecordMegaBoardRun_TracksClearsScoreAndRewards()
    {
        var stats = new DemoStatsStore();
        var run = new MegaBoardRun(Enumerable.Range(0, 100)
            .Select(_ => new WordleSession("CRANE", maxAttempts: 1)));

        run.SubmitGuess("CRANE");
        stats.RecordMegaBoardRun(run);

        Assert.Equal(1, stats.MegaBoardRuns);
        Assert.Equal(100, stats.BestMegaBoardClears);
        Assert.Equal(1, stats.MegaBoardPerfectClears);
        Assert.Equal(1, stats.MegaBoardCenturionClears);
        Assert.True(stats.BestMegaBoardScore > 0);
        Assert.Equal(1, stats.PowerUps[nameof(PowerUpType.Crown)]);
    }

    [Fact]
    public void RecordTimeAttackRun_TracksRecordsAndComboRewards()
    {
        var stats = new DemoStatsStore();
        var run = new TimeAttackRun();
        for (var index = 0; index < 5; index++)
            run.RecordSolved(score: 100);

        stats.RecordTimeAttackRun(run);

        Assert.Equal(1, stats.TimeAttackRuns);
        Assert.Equal(run.Score, stats.BestTimeAttackScore);
        Assert.Equal(5, stats.BestTimeAttackSolved);
        Assert.Equal(5, stats.BestTimeAttackCombo);
        Assert.Equal(run.Score, stats.TotalScore);
        Assert.Equal(1, stats.PowerUps[nameof(PowerUpType.Clock)]);
    }
}
