using Words.Core.Models;
using Words.Core.Services;

namespace Words.Tests;

public class PvpServiceTests
{
    [Fact]
    public void StartMatch_CreatesSharedWordMatch()
    {
        var service = new PvpService(WordService.FromEmbeddedResource(new Random(0)));
        var match = service.StartMatch(new Player("Alpha"), new Player("Bravo"), 5);

        Assert.Equal(PvpMatchStatus.InProgress, match.Status);
        Assert.Equal(match.PlayerOneSession.PrimaryWord.Text, match.PlayerTwoSession.PrimaryWord.Text);
    }

    [Fact]
    public void SubmitGuess_FirstWinner_EndsMatch()
    {
        var service = new PvpService(new WordService(new List<Word>
        {
            new("GAME", WordCategory.General, GameDifficulty.Medium, "hint")
        }));

        var match = service.StartMatch(new Player("Alpha"), new Player("Bravo"), 4);
        foreach (var letter in "GAME")
            service.SubmitGuess(match.Id, "Alpha", letter);

        Assert.Equal(PvpMatchStatus.Completed, match.Status);
        Assert.Equal("Alpha", match.Winner?.GamerTag);
    }
}
