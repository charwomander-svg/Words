using Words.Core.Models;

namespace Words.Tests;

public class PlayerTests
{
    [Fact]
    public void AddExperience_UnlocksCosmeticsByRank()
    {
        var player = new Player("Tester");

        player.AddExperience(250);

        Assert.True(player.Rank >= 3);
        Assert.Contains("Classic tiles", player.UnlockedCosmetics);
        Assert.Contains("Neon background", player.UnlockedCosmetics);
    }
}
