using Words.Core.Models;
using Words.Core.Services;
using Words.Xbox;
using System.Text;
using System.Threading;

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
        var targetOutput = output ?? Console.Out;
        var pacedOutput = output is null
            ? new DemoPacedWriter(targetOutput, lineDelayMs: 120)
            : targetOutput;

        var wordService = new WordService(new[]
        {
            new Word("MOVIE", WordCategory.Entertainment, GameDifficulty.Easy, "A feature-length film shown in cinemas"),
            new Word("DRAGON", WordCategory.General, GameDifficulty.Medium, "Fire-breathing creature")
        });

        var demoStorageDirectory = Path.Combine(Path.GetTempPath(), "Words-Demo");
        Directory.CreateDirectory(demoStorageDirectory);

        var demoStoragePath = Path.Combine(demoStorageDirectory, $"leaderboard-{Guid.NewGuid():N}.json");

        pacedOutput.WriteLine("=== Demo Tour ===");
        pacedOutput.WriteLine("A full, scripted walkthrough of menu pages and gameplay flows.");
        pacedOutput.WriteLine("It uses curated words and an isolated demo leaderboard.");
        pacedOutput.WriteLine();

        try
        {
            var scoreService = new ScoreService(demoStoragePath);
            var gameService = new GameService(wordService, scoreService);

            using var input = new StringReader(BuildScript());
            var host = new XboxGameHost(gameService, scoreService, input, pacedOutput);
            host.Run();
        }
        finally
        {
            try
            {
                if (File.Exists(demoStoragePath))
                    File.Delete(demoStoragePath);

                if (Directory.Exists(demoStorageDirectory) && !Directory.EnumerateFileSystemEntries(demoStorageDirectory).Any())
                    Directory.Delete(demoStorageDirectory);
            }
            catch
            {
            }
        }
    }

    private static string BuildScript() =>
        string.Join(Environment.NewLine, new[]
        {
            "DemoPlayer",
            "SinglePlayer",
            "Classic",
            "Easy",
            "Entertainment",
            "m",
            "o",
            "v",
            "i",
            "e",
            "B",
            "SinglePlayer",
            "Classic",
            "Medium",
            "General",
            "q",
            "w",
            "x",
            "y",
            "z",
            "k",
            "B",
            "Multiplayer",
            "HeadToHead",
            "CreateLobby",
            "AddLocalGuest",
            "DemoGuest",
            "StartMatch",
            "B",
            "Multiplayer",
            "Tournament",
            "Join",
            "TRNY01",
            "AddLocalGuest",
            "Guest2",
            "AddLocalGuest",
            "Guest3",
            "AddLocalGuest",
            "Guest4",
            "StartMatch",
            "B",
            "B",
            "Statistics",
            "Settings",
            "SetTheme",
            "Dark",
            "SetDifficultyPreference",
            "Competitive",
            "B",
            "Achievements",
            "Credits",
            "Leaderboards",
            "Q"
        }) + Environment.NewLine;

    private sealed class DemoPacedWriter : TextWriter
    {
        private readonly TextWriter _inner;
        private readonly int _lineDelayMs;

        public DemoPacedWriter(TextWriter inner, int lineDelayMs)
        {
            _inner = inner;
            _lineDelayMs = lineDelayMs;
        }

        public override Encoding Encoding => _inner.Encoding;

        public override void Write(char value) => _inner.Write(value);

        public override void Write(string? value) => _inner.Write(value);

        public override void WriteLine(string? value)
        {
            _inner.WriteLine(value);
            _inner.Flush();
            Thread.Sleep(_lineDelayMs);
        }
    }
}
