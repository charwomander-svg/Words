using Words.Core.Models;
using Words.Core.Services;

namespace Words.Tests;

public class GameSessionTests
{
    private static GameSession CreateSession(string wordText = "CASTLE") =>
        new GameSession(
            new Player("TestPlayer"),
            new Word(wordText, WordCategory.General, GameDifficulty.Easy, "A hint"),
            new GameConfig { MaxIncorrectGuesses = 6 }
        );

    [Fact]
    public void MaskedWord_AllUnderscores_BeforeAnyGuess()
    {
        var session = CreateSession("CAT");
        Assert.Equal("___", session.MaskedWord);
        Assert.Equal("CAT", session.Answer);
    }

    [Fact]
    public void Guess_CorrectLetter_RevealsInMask()
    {
        var session = CreateSession("CAT");
        var result = session.Guess('C');
        Assert.Equal(GuessOutcome.Correct, result.Outcome);
        Assert.Equal("C__", result.MaskedWord);
    }

    [Fact]
    public void Guess_IncorrectLetter_IncrementsIncorrectGuesses()
    {
        var session = CreateSession("CAT");
        session.Guess('Z');
        Assert.Equal(1, session.IncorrectGuesses);
        Assert.Equal(GameStatus.InProgress, session.Status);
    }

    [Fact]
    public void Guess_NonLetter_Throws()
    {
        var session = CreateSession("CAT");
        Assert.Throws<ArgumentException>(() => session.Guess('1'));
    }

    [Fact]
    public void Guess_DuplicateLetter_ReturnsAlreadyGuessed()
    {
        var session = CreateSession("CAT");
        session.Guess('C');
        var result = session.Guess('C');
        Assert.Equal(GuessOutcome.AlreadyGuessed, result.Outcome);
        Assert.Equal(0, session.IncorrectGuesses);
    }

    [Fact]
    public void Guess_AllLetters_SetsStatusWon()
    {
        var session = CreateSession("CAT");
        session.Guess('C');
        session.Guess('A');
        var result = session.Guess('T');
        Assert.Equal(GameStatus.Won, session.Status);
        Assert.True(result.IsWordSolved);
    }

    [Fact]
    public void GuessWord_CorrectWord_SetsStatusWon()
    {
        var session = CreateSession("CAT");
        var result = session.GuessWord("cat");
        Assert.Equal(GuessOutcome.Correct, result.Outcome);
        Assert.Equal("CAT", result.MaskedWord);
        Assert.Equal(GameStatus.Won, session.Status);
        Assert.True(result.IsWordSolved);
    }

    [Fact]
    public void GuessWord_IncorrectWord_CostsOneGuess()
    {
        var session = CreateSession("CAT");
        var result = session.GuessWord("dog");
        Assert.Equal(GuessOutcome.Incorrect, result.Outcome);
        Assert.Equal("___", result.MaskedWord);
        Assert.Equal(1, session.IncorrectGuesses);
        Assert.Equal(GameStatus.InProgress, session.Status);
    }

    [Fact]
    public void GuessWord_EmptyOrNonLetters_Throws()
    {
        var session = CreateSession("CAT");

        Assert.Throws<ArgumentException>(() => session.GuessWord(""));
        Assert.Throws<ArgumentException>(() => session.GuessWord("c4t"));
        Assert.Equal(0, session.IncorrectGuesses);
    }

    [Fact]
    public void Guess_MaxIncorrectGuesses_SetsStatusLost()
    {
        var session = CreateSession("CAT");
        foreach (var letter in new[] { 'Z', 'X', 'Q', 'W', 'V', 'U' })
            session.Guess(letter);
        Assert.Equal(GameStatus.Lost, session.Status);
    }

    [Fact]
    public void Guess_AfterGameOver_ReturnsGameOverOutcome()
    {
        var session = CreateSession("CAT");
        session.Guess('C'); session.Guess('A'); session.Guess('T');
        var result = session.Guess('Z');
        Assert.Equal(GuessOutcome.GameOver, result.Outcome);
    }

    [Fact]
    public void CalculateScore_WonGame_ReturnsPositiveScore()
    {
        var config = new GameConfig { BasePoints = 100, BonusPerRemainingGuess = 10, MaxIncorrectGuesses = 6 };
        var session = new GameSession(
            new Player("P1"),
            new Word("CAT", WordCategory.Animals, GameDifficulty.Easy, "hint"),
            config);
        session.Guess('C'); session.Guess('A'); session.Guess('T');
        Assert.True(session.CalculateScore() > 0);
    }

    [Fact]
    public void CalculateScore_LostGame_ReturnsZero()
    {
        var session = CreateSession("CAT");
        foreach (var l in new[] { 'Z', 'X', 'Q', 'W', 'V', 'U' })
            session.Guess(l);
        Assert.Equal(0, session.CalculateScore());
    }

    [Fact]
    public void ToState_ReturnsImmutableUiSnapshot()
    {
        var session = CreateSession("CAT");
        session.Guess('T');
        session.Guess('C');

        var state = session.ToState();

        Assert.Equal(session.Id, state.Id);
        Assert.Equal("TestPlayer", state.GamerTag);
        Assert.Equal(GameStatus.InProgress, state.Status);
        Assert.Equal("C_T", state.MaskedWord);
        Assert.Equal(new[] { 'C', 'T' }, state.GuessedLetters);
        Assert.Equal("A hint", state.Hint);
    }
}
