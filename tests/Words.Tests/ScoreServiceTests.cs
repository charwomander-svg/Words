using Words.Core.Models;
using Words.Core.Services;

namespace Words.Tests;

public class ScoreServiceTests
{
    [Fact]
    public void AwardPoints_AddsToPlayerScore()
    {
        var service = new ScoreService();
        var player  = new Player("Gamer1");
        service.AwardPoints(player, 150);
        Assert.Equal(150, player.Score);
    }

    [Fact]
    public void AwardPoints_MultipleCallsSamePlayer_Accumulates()
    {
        var service = new ScoreService();
        var player  = new Player("Gamer1");
        service.AwardPoints(player, 100);
        service.AwardPoints(player, 50);
        Assert.Equal(150, player.Score);
    }

    [Fact]
    public void GetLeaderboard_SortedByScoreDescending()
    {
        var service = new ScoreService();
        var p1 = new Player("Low");
        var p2 = new Player("High");
        service.AwardPoints(p1, 50);
        service.AwardPoints(p2, 200);
        var board = service.GetLeaderboard();
        Assert.Equal("High", board[0].GamerTag);
        Assert.Equal("Low",  board[1].GamerTag);
    }

    [Fact]
    public void AwardPoints_NegativePoints_Throws()
    {
        var service = new ScoreService();
        var player  = new Player("Gamer1");
        Assert.Throws<ArgumentOutOfRangeException>(() => service.AwardPoints(player, -1));
    }

    [Fact]
    public void AwardPoints_WithStoragePath_PersistsLeaderboard()
    {
        var path = Path.GetTempFileName();

        try
        {
            var service = new ScoreService(path);
            var player = new Player("PersistentPlayer");

            service.AwardPoints(player, 125);

            var reloaded = new ScoreService(path);
            var leaderboard = reloaded.GetLeaderboard();

            Assert.Single(leaderboard);
            Assert.Equal("PersistentPlayer", leaderboard[0].GamerTag);
            Assert.Equal(125, leaderboard[0].Score);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void AwardPoints_SameGamerTag_CombinesLeaderboardScore()
    {
        var service = new ScoreService();
        var first = new Player("Gamer1");
        var second = new Player(" gamer1 ");

        service.AwardPoints(first, 100);
        service.AwardPoints(second, 50);

        var leaderboard = service.GetLeaderboard();
        Assert.Single(leaderboard);
        Assert.Equal("Gamer1", leaderboard[0].GamerTag);
        Assert.Equal(150, leaderboard[0].Score);
        Assert.Equal(50, second.Score);
    }
}
