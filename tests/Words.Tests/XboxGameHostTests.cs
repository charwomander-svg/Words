using System.IO;
using Words.Core.Models;
using Words.Core.Services;
using Words.Xbox;

namespace Words.Tests;

public class XboxGameHostTests
{
    [Fact]
    public void Run_LeaderboardMenu_ShowsScoresAndExits()
    {
        var scoreService = new ScoreService();
        scoreService.AwardPoints(new Player("HighScore"), 250);

        var wordService = new WordService(new[]
        {
            new Word("DRAGON", WordCategory.General, GameDifficulty.Medium, "Fire-breathing creature")
        });
        var gameService = new GameService(wordService, scoreService);

        using var input = new StringReader("PlayerOne\n2\n3\n");
        using var output = new StringWriter();
        var host = new XboxGameHost(gameService, scoreService, input, output);

        host.Run();

        var text = output.ToString();
        Assert.Contains("=== Leaderboard ===", text);
        Assert.Contains("HighScore", text);
        Assert.Contains("Thanks for playing, PlayerOne!", text);
    }
}
