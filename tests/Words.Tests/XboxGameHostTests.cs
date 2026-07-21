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

    [Fact]
    public void Run_DemoMode_CompletesRepeatableRound()
    {
        using var output = new StringWriter();

        DemoRunner.Run(output);

        var text = output.ToString();
        Assert.Contains("=== Demo Tour ===", text);
        Assert.Contains("A short, scripted playthrough of the legacy Words Xbox host.", text);
        Assert.Contains("Hint: A feature-length film shown in cinemas", text);
        Assert.Contains("🎉 You guessed 'MOVIE'! +160 points", text);
        Assert.Contains("💀 Out of guesses! The word was 'DRAGON'.", text);
        Assert.Contains("=== Leaderboard ===", text);
        Assert.Contains("DemoPlayer", text);
        Assert.Contains("Thanks for playing, DemoPlayer!", text);
    }
}
