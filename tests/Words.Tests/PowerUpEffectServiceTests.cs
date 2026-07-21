using Words.Core.Models;
using Words.Core.Services;

namespace Words.Tests;

public class PowerUpEffectServiceTests
{
    [Fact]
    public void UseBomb_RemovesOnlyLettersNotInAnswerOrGuessed()
    {
        var result = PowerUpEffectService.UseBomb("CRANE", ['B', 'D'], count: 5);

        Assert.DoesNotContain('C', result.Letters);
        Assert.DoesNotContain('R', result.Letters);
        Assert.DoesNotContain('A', result.Letters);
        Assert.DoesNotContain('N', result.Letters);
        Assert.DoesNotContain('E', result.Letters);
        Assert.DoesNotContain('B', result.Letters);
        Assert.DoesNotContain('D', result.Letters);
        Assert.True(result.Letters.Count <= 5);
    }

    [Fact]
    public void UsePingPongPaddle_ReportsWhetherVowelAppears()
    {
        var result = PowerUpEffectService.UsePingPongPaddle("CRANE", 'A');

        Assert.Equal(PowerUpType.PingPongPaddle, result.PowerUp);
        Assert.Contains("appears", result.Message);
    }

    [Fact]
    public void TrySpendPowerUp_RemovesOneInventoryItem()
    {
        var stats = new DemoStatsStore();
        stats.AddPowerUp(PowerUpType.Bomb);
        stats.AddPowerUp(PowerUpType.Bomb);

        Assert.True(stats.TrySpendPowerUp(PowerUpType.Bomb));

        Assert.Equal(1, stats.PowerUps[nameof(PowerUpType.Bomb)]);
    }
}
