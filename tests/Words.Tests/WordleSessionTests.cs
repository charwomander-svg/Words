using Words.Core.Models;
using Words.Core.Services;

namespace Words.Tests;

public class WordleSessionTests
{
    [Fact]
    public void ScoreGuess_HandlesDuplicateLettersCorrectly()
    {
        var result = WordleSession.ScoreGuess("ALLEY", "APPLE");

        Assert.Equal(new[]
        {
            WordleTileState.Correct,
            WordleTileState.Present,
            WordleTileState.Absent,
            WordleTileState.Present,
            WordleTileState.Absent
        }, result.Tiles);
    }

    [Fact]
    public void AddGuess_CorrectGuess_WinsSession()
    {
        var session = new WordleSession("CRANE");

        var result = session.AddGuess("crane");

        Assert.True(result.IsCorrect);
        Assert.Equal(GameStatus.Won, session.Status);
    }

    [Fact]
    public void AddGuess_ExhaustedAttempts_LosesSession()
    {
        var session = new WordleSession("CRANE", maxAttempts: 2);

        session.AddGuess("SLATE");
        session.AddGuess("MOUND");

        Assert.Equal(GameStatus.Lost, session.Status);
        Assert.Equal(0, session.RemainingAttempts);
    }

    [Fact]
    public void Expire_InProgressSession_LosesSession()
    {
        var session = new WordleSession("CRANE");

        session.Expire();

        Assert.Equal(GameStatus.Lost, session.Status);
    }

    [Fact]
    public void Expire_WonSession_DoesNotChangeStatus()
    {
        var session = new WordleSession("CRANE");
        session.AddGuess("CRANE");

        session.Expire();

        Assert.Equal(GameStatus.Won, session.Status);
    }

    [Fact]
    public void KeyboardState_DoesNotDowngradeKnownLetters()
    {
        var keyboard = new WordleKeyboardState();
        keyboard.Apply(new WordleGuessResult("TRACE", new[]
        {
            WordleTileState.Absent,
            WordleTileState.Present,
            WordleTileState.Absent,
            WordleTileState.Absent,
            WordleTileState.Absent
        }, IsCorrect: false));

        keyboard.Apply(new WordleGuessResult("RIVER", new[]
        {
            WordleTileState.Absent,
            WordleTileState.Absent,
            WordleTileState.Absent,
            WordleTileState.Absent,
            WordleTileState.Absent
        }, IsCorrect: false));

        Assert.True(keyboard.TryGet('R', out var state));
        Assert.Equal(WordleTileState.Present, state);
    }
}
