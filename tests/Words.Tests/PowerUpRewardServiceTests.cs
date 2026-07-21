using Words.Core.Models;
using Words.Core.Services;

namespace Words.Tests;

public class PowerUpRewardServiceTests
{
    [Fact]
    public void GetFastSolveRewards_OneGuess_AwardsCrown()
    {
        var rewards = PowerUpRewardService.GetFastSolveRewards(1);

        Assert.Equal([PowerUpType.Crown], rewards);
    }

    [Fact]
    public void GetFastSolveRewards_ThreeGuesses_AwardsLightPowerUps()
    {
        var rewards = PowerUpRewardService.GetFastSolveRewards(3);

        Assert.Contains(PowerUpType.Bomb, rewards);
        Assert.Contains(PowerUpType.PingPongPaddle, rewards);
        Assert.Contains(PowerUpType.Clock, rewards);
    }

    [Theory]
    [InlineData(3, PowerUpType.Shield)]
    [InlineData(5, PowerUpType.Reveal)]
    [InlineData(10, PowerUpType.Freeze)]
    [InlineData(15, PowerUpType.Reroll)]
    public void GetStreakRewards_Milestones_AwardExpectedPowerUp(int streak, PowerUpType expected)
    {
        var rewards = PowerUpRewardService.GetStreakRewards(streak);

        Assert.Equal([expected], rewards);
    }
}
