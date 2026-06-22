using Words.Core.Services;
using Words.Xbox;

var wordService  = WordService.FromEmbeddedResource();
var storageRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
var leaderboardPath = string.IsNullOrWhiteSpace(storageRoot)
    ? Path.Combine(AppContext.BaseDirectory, "leaderboard.json")
    : Path.Combine(storageRoot, "Words", "leaderboard.json");
var scoreService = new ScoreService(leaderboardPath);
var gameService  = new GameService(wordService, scoreService);

var host = new XboxGameHost(gameService, scoreService);
host.Run();
