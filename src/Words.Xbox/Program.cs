using Words.Core.Services;
using Words.Xbox;
using Words.Xbox.Audio;

var wordService  = WordService.FromEmbeddedResource();
var scoreService = new ScoreService();
var gameService  = new GameService(wordService, scoreService);
using var audio = new GameAudioService();

var host = new XboxGameHost(gameService, audio);
host.Run();
