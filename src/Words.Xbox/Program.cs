using Words.Core.Models;
using Words.Core.Services;
using Words.Xbox;

if (args.Any(arg => arg.Equals("--demo", StringComparison.OrdinalIgnoreCase)))
{
    DemoRunner.Run();
    return;
}

var wordService  = WordService.FromEmbeddedResource();
var storageRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
var leaderboardPath = string.IsNullOrWhiteSpace(storageRoot)
    ? Path.Combine(AppContext.BaseDirectory, "leaderboard.json")
    : Path.Combine(storageRoot, "Words", "leaderboard.json");
var scoreService = new ScoreService(leaderboardPath);
var gameService  = new GameService(wordService, scoreService);

var host = new XboxGameHost(gameService, scoreService);
host.Run();

public static class DemoRunner
{
    public static void Run(TextWriter? output = null)
    {
        var wordService = new WordService(new[]
        {
            new Word("MOVIE", WordCategory.Entertainment, GameDifficulty.Easy, "A feature-length film shown in cinemas")
        });
        var scoreService = new ScoreService();
        var gameService = new GameService(wordService, scoreService);

        using var input = new StringReader(BuildScript());
        var host = new XboxGameHost(gameService, scoreService, input, output);
        host.Run();
    }

    private static string BuildScript() =>
        string.Join(Environment.NewLine, new[]
        {
            "DemoPlayer",
            "Play",
            "Easy",
            "Entertainment",
            "m",
            "o",
            "v",
            "i",
            "e",
            "Leaderboard",
            "Quit"
        }) + Environment.NewLine;
}
