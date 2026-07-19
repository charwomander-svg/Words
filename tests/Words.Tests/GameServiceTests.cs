using Words.Core.Models;
using Words.Core.Services;

namespace Words.Tests;

public class GameServiceTests
{
    private static (GameService service, Player player) Setup()
    {
        var words = new List<Word>
        {
            new("DRAGON", WordCategory.General, GameDifficulty.Medium, "Fire-breathing creature")
        };
        var wordService  = new WordService(words);
        var scoreService = new ScoreService();
        var gameService  = new GameService(wordService, scoreService);
        var player       = new Player("XboxGamer");
        return (gameService, player);
    }

    [Fact]
    public void StartGame_ReturnsActiveSession()
    {
        var (service, player) = Setup();
        var config  = new GameConfig { Category = WordCategory.General, Difficulty = GameDifficulty.Medium };
        var session = service.StartGame(player, config);
        Assert.Equal(GameStatus.InProgress, session.Status);
    }

    [Fact]
    public void SubmitGuess_CorrectLetter_ReturnsCorrectOutcome()
    {
        var (service, player) = Setup();
        var config  = new GameConfig { Category = WordCategory.General, Difficulty = GameDifficulty.Medium };
        var session = service.StartGame(player, config);
        var result  = service.SubmitGuess(session.Id, 'D');
        Assert.Equal(GuessOutcome.Correct, result.Outcome);
    }

    [Fact]
    public void SubmitWordGuess_CorrectWord_AwardsScore()
    {
        var (service, player) = Setup();
        var config  = new GameConfig { Category = WordCategory.General, Difficulty = GameDifficulty.Medium };
        var session = service.StartGame(player, config);
        var result  = service.SubmitWordGuess(session.Id, "dragon");
        Assert.Equal(GuessOutcome.Correct, result.Outcome);
        Assert.True(player.Score > 0);
    }

    [Fact]
    public void EndGame_Won_AwardsScore()
    {
        var (service, player) = Setup();
        var config = new GameConfig
        {
            Category = WordCategory.General,
            Difficulty = GameDifficulty.Medium,
            BasePoints = 100,
            BonusPerRemainingGuess = 10
        };
        var session = service.StartGame(player, config);
        foreach (var letter in "DRAGON")
            service.SubmitGuess(session.Id, letter);

        Assert.True(player.Score > 0);
    }

    [Fact]
    public void GetSession_UnknownId_Throws()
    {
        var (service, _) = Setup();
        Assert.Throws<KeyNotFoundException>(() => service.GetSession(Guid.NewGuid()));
    }
}
