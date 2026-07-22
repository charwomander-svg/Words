using Words.Core.Models;
using Words.Core.Services;

namespace Words.Tests;

public class WordleEvaluatorTests
{
    [Fact]
    public void Evaluate_ExactMatch_AllCorrect()
    {
        var result = WordleEvaluator.Evaluate("CRANE", "CRANE");
        Assert.All(result, state => Assert.Equal(WordleLetterState.Correct, state));
    }

    [Fact]
    public void Evaluate_DuplicateLetters_UsesWordleCountingRules()
    {
        var result = WordleEvaluator.Evaluate("ALLEY", "BALMY");
        var expected = new[]
        {
            WordleLetterState.Present,
            WordleLetterState.Absent,
            WordleLetterState.Correct,
            WordleLetterState.Absent,
            WordleLetterState.Correct
        };

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Evaluate_DifferentLengths_Throws()
    {
        Assert.Throws<ArgumentException>(() => WordleEvaluator.Evaluate("CAT", "CRANE"));
    }
}
