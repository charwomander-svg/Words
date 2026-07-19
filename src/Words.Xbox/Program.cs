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
        output ??= Console.Out;

        var wordService = new WordService(new[]
        {
            new Word("MOVIE", WordCategory.Entertainment, GameDifficulty.Easy, "A feature-length film shown in cinemas"),
            new Word("DRAGON", WordCategory.General, GameDifficulty.Medium, "Fire-breathing creature")
        });

        var demoStorageDirectory = Path.Combine(Path.GetTempPath(), "Words-Demo");
        Directory.CreateDirectory(demoStorageDirectory);

        var demoStoragePath = Path.Combine(demoStorageDirectory, $"leaderboard-{Guid.NewGuid():N}.json");

        output.WriteLine("=== Demo Tour ===");
        output.WriteLine("A short, scripted playthrough of Guess That Word.");
        output.WriteLine("It uses curated words and an isolated demo leaderboard.");
        output.WriteLine("Tour path: reveal part of MOVIE, solve with a full-word guess, lose DRAGON, view the leaderboard, then quit.");
        output.WriteLine();

        try
        {
            var scoreService = new ScoreService(demoStoragePath);
            var gameService = new GameService(wordService, scoreService);

            using var input = new DemoScriptReader(BuildScript(), output);
            var host = new XboxGameHost(gameService, scoreService, input, output);
            host.Run();

            output.WriteLine();
            output.WriteLine("=== Demo Tour Complete ===");
            output.WriteLine("Replay with --demo any time to see the same isolated tour.");
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
            "Play",
            "Easy",
            "Entertainment",
            "m",
            "o",
            "movie",
            "Play",
            "Medium",
            "General",
            "q",
            "w",
            "x",
            "y",
            "z",
            "k",
            "Leaderboard",
            "Quit"
        }) + Environment.NewLine;

    private sealed class DemoScriptReader : TextReader
    {
        private readonly StringReader _script;
        private readonly TextWriter _output;

        public DemoScriptReader(string script, TextWriter output)
        {
            _script = new StringReader(script);
            _output = output;
        }

        public override string? ReadLine()
        {
            var line = _script.ReadLine();
            if (line is not null)
                _output.WriteLine(line);

            return line;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _script.Dispose();

            base.Dispose(disposing);
        }
    }
}
