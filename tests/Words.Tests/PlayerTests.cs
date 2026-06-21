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
    public void FiveLetterWordAchievements_UnlockByStartingLetter()
    {
        var player = new Player("Tester");

        foreach (var letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        {
            player.RecordFiveLetterWordSolvedStartingWith($"{letter}AAAA");
            Assert.Contains($"Starts with {letter}", player.UnlockedAchievements);
        }
    }

    [Fact]
    public void TitleUpdateAchievements_UnlockIndividually()
    {
        var player = new Player("Tester");

        player.RecordCreditsViewed();
        player.RecordHeadToHeadWin();
        player.RecordTenLetterWordSolvedWithoutHints();

        Assert.Contains("Credits Roll", player.UnlockedAchievements);
        Assert.Contains("Rivalry Crown", player.UnlockedAchievements);
        Assert.Contains("Perfect Ten", player.UnlockedAchievements);
    }
}
