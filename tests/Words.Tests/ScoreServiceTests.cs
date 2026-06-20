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
}
