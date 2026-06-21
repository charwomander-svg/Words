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

    [Fact]
    public void Progress_UnlocksAchievements()
    {
        var player = new Player("Tester");

        player.RecordGameResult(true);
        player.AddScore(100);
        player.AddExperience(250);

        Assert.Contains("First Win", player.UnlockedAchievements);
        Assert.Contains("Century Club", player.UnlockedAchievements);
        Assert.Contains("Rising Star", player.UnlockedAchievements);
    }

    [Fact]
    public void TitleUpdateAchievements_UnlockIndividually()
    {
        var player = new Player("Tester");

        player.RecordCreditsViewed();
        player.RecordHeadToHeadWin();
        player.RecordTenLetterWordSolvedWithoutHints();

        Assert.Contains("Backstage Pass", player.UnlockedAchievements);
        Assert.Contains("Head-to-Head Challenger", player.UnlockedAchievements);
        Assert.Contains("No-Hint Ten", player.UnlockedAchievements);
    }
}
