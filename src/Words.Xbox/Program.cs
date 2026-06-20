using Words.Core.Services;
using Words.Xbox;

var wordService  = WordService.FromEmbeddedResource();
var scoreService = new ScoreService();
var gameService  = new GameService(wordService, scoreService);

var host = new XboxGameHost(gameService);
host.Run();
