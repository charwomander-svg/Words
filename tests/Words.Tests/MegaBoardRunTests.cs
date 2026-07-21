using Words.Core.Models;

namespace Words.Tests;

public class MegaBoardRunTests
{
    [Fact]
    public void SubmitGuess_AppliesGuessToAllActiveBoards()
    {
        var run = new MegaBoardRun(
        [
            new WordleSession("CRANE"),
            new WordleSession("SLATE"),
            new WordleSession("MOUND")
        ]);

        var results = run.SubmitGuess("CRANE");

        Assert.Equal(3, results.Count);
        Assert.Equal(1, run.SolvedCount);
        Assert.Equal(3, run.Boards.Count(board => board.Guesses.Count == 1));
        Assert.Equal("SLATE", run.FocusBoard.Answer);
    }

    [Fact]
    public void IsComplete_WhenEveryBoardResolved()
    {
        var run = new MegaBoardRun(
        [
            new WordleSession("CRANE", maxAttempts: 1),
            new WordleSession("SLATE", maxAttempts: 1)
        ]);

        run.SubmitGuess("CRANE");

        Assert.True(run.IsComplete);
        Assert.Equal(1, run.SolvedCount);
        Assert.Equal(1, run.LostCount);
        Assert.False(run.IsPerfectClear);
    }

    [Fact]
    public void Constructor_AllowsOneHundredTwentyLetterBoards()
    {
        var boards = Enumerable.Range(0, 100)
            .Select(_ => new WordleSession("ABCDEFGHIJKLMNOPQRST", maxAttempts: 22));

        var run = new MegaBoardRun(boards);

        Assert.Equal(100, run.BoardCount);
        Assert.Equal(20, run.FocusBoard.WordLength);
        Assert.Equal(22, run.FocusBoard.MaxAttempts);
    }

    [Fact]
    public void FocusBoard_PrefersBoardWithMostKnownCorrectTiles()
    {
        var run = new MegaBoardRun(
        [
            new WordleSession("CRANE"),
            new WordleSession("CRATE")
        ]);

        run.SubmitGuess("CRANE");

        Assert.Equal("CRATE", run.FocusBoard.Answer);
        Assert.Equal("C R A _ E", run.BuildKnownPattern(run.FocusBoard));
    }

    [Fact]
    public void DangerCount_CountsActiveBoardsWithTwoOrFewerAttempts()
    {
        var run = new MegaBoardRun(
        [
            new WordleSession("CRANE", maxAttempts: 3),
            new WordleSession("SLATE", maxAttempts: 3)
        ]);

        run.SubmitGuess("MOUND");

        Assert.Equal(2, run.DangerCount);
    }

    [Fact]
    public void ReplaceBoard_SwapsFocusedBoard()
    {
        var first = new WordleSession("CRANE");
        var run = new MegaBoardRun([first, new WordleSession("SLATE")]);
        var replacement = new WordleSession("MOUND");

        run.ReplaceBoard(first, replacement);

        Assert.DoesNotContain(first, run.Boards);
        Assert.Contains(replacement, run.Boards);
    }

    [Fact]
    public void BuildAggregateKeyboardState_UsesBestLetterStateAcrossBoards()
    {
        var run = new MegaBoardRun(
        [
            new WordleSession("CRANE"),
            new WordleSession("SLATE")
        ]);

        run.SubmitGuess("TRACE");
        var keyboard = run.BuildAggregateKeyboardState();

        Assert.Equal(WordleTileState.Correct, keyboard['A']);
        Assert.Equal(WordleTileState.Present, keyboard['T']);
    }

    [Fact]
    public void GetEndangeredBoards_ReturnsActiveBoardsClosestToFailure()
    {
        var run = new MegaBoardRun(
        [
            new WordleSession("CRANE", maxAttempts: 3),
            new WordleSession("SLATE", maxAttempts: 3),
            new WordleSession("MOUND", maxAttempts: 3)
        ]);

        run.Boards[0].AddGuess("SLATE");
        run.Boards[0].AddGuess("MOUND");
        run.Boards[1].AddGuess("CRANE");

        var endangered = run.GetEndangeredBoards(2);

        Assert.Equal("CRANE", endangered[0].Answer);
        Assert.Equal("SLATE", endangered[1].Answer);
    }
}
