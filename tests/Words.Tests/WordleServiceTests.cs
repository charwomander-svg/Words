using Words.Core.Interfaces;
using Words.Core.Models;
using Words.Core.Services;

namespace Words.Tests;

public class WordleServiceTests
{
    [Fact]
    public void SubmitGuess_InvalidDictionaryWord_Throws()
    {
        var service = new WordleService(new FakeWordService());
        var session = new WordleSession("CRANE");

        Assert.Throws<InvalidOperationException>(() => service.SubmitGuess(session, "ZZZZZ"));
    }

    [Fact]
    public void SubmitGuess_ValidWord_AddsGuess()
    {
        var service = new WordleService(new FakeWordService());
        var session = new WordleSession("CRANE");

        var result = service.SubmitGuess(session, "SLATE");

        Assert.Equal("SLATE", result.Guess);
        Assert.Single(session.Guesses);
    }

    [Fact]
    public void SubmitGuess_HardMode_RequiresKnownCorrectPositions()
    {
        var service = new WordleService(new FakeWordService());
        var session = new WordleSession("CRANE");
        service.SubmitGuess(session, "CRATE", WordleGameMode.Hard);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            service.SubmitGuess(session, "SLATE", WordleGameMode.Hard));
        Assert.Contains("position 1", ex.Message);
    }

    [Fact]
    public void SubmitGuess_HardMode_RequiresKnownPresentLetters()
    {
        var service = new WordleService(new FakeWordService());
        var session = new WordleSession("CRANE");
        service.SubmitGuess(session, "ROBOT", WordleGameMode.Hard);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            service.SubmitGuess(session, "SLATE", WordleGameMode.Hard));
        Assert.Contains("include 'R'", ex.Message);
    }

    [Fact]
    public void SubmitGuess_HardMode_AllowsGuessUsingKnownInformation()
    {
        var service = new WordleService(new FakeWordService());
        var session = new WordleSession("CRANE");
        service.SubmitGuess(session, "MOUND", WordleGameMode.Hard);

        var result = service.SubmitGuess(session, "CRANE", WordleGameMode.Hard);

        Assert.True(result.IsCorrect);
    }

    [Fact]
    public void StartSession_Classic_UsesNonWildAnswer()
    {
        var service = new WordleService(new FakeWordService());

        var session = service.StartSession(WordleGameMode.Classic);

        Assert.NotEqual("XYLYL", session.Answer);
        Assert.Contains(session.Answer, new[] { "CRANE", "CRATE", "SLATE", "MOUND", "PRISM", "ROBOT" });
    }

    [Fact]
    public void StartSession_WildDictionary_CanUseDictionaryOnlyAnswer()
    {
        var service = new WordleService(new FakeWordService());

        var session = service.StartSession(WordleGameMode.WildDictionary);

        Assert.Equal("XYLYL", session.Answer);
    }

    [Fact]
    public void StartSession_RareHunt_UsesRareOrWildAnswer()
    {
        var service = new WordleService(new FakeWordService());

        var session = service.StartSession(WordleGameMode.RareHunt);

        Assert.Contains(session.Answer, new[] { "XYLYL", "CRATE" });
    }

    [Fact]
    public void StartSession_Discovery_UsesClassicAnswerPool()
    {
        var service = new WordleService(new FakeWordService());

        var session = service.StartSession(WordleGameMode.Discovery);

        Assert.NotEqual("XYLYL", session.Answer);
    }

    [Fact]
    public void StartSession_Classic_FallsBackToDictionaryWhenNoClassicAnswerExistsForLength()
    {
        var service = new WordleService(new FakeWordService());

        var session = service.StartSession(WordleGameMode.Classic, wordLength: 6);

        Assert.Equal("XYLYLS", session.Answer);
    }

    [Fact]
    public void StartDailySession_SameDateAndLength_ReturnsSameAnswer()
    {
        var service = new WordleService(new FakeWordService());

        var first = service.StartDailySession(new DateOnly(2026, 7, 18));
        var second = service.StartDailySession(new DateOnly(2026, 7, 18));

        Assert.Equal(first.Answer, second.Answer);
    }

    [Fact]
    public void StartDailySession_DifferentLengths_ReturnDifferentLengthAnswers()
    {
        var service = new WordleService(new FakeWordService());

        var five = service.StartDailySession(new DateOnly(2026, 7, 18), wordLength: 5);
        var six = service.StartDailySession(new DateOnly(2026, 7, 18), wordLength: 6);

        Assert.Equal(5, five.WordLength);
        Assert.Equal(6, six.WordLength);
    }

    [Fact]
    public void GetMetadataCoverage_CountsTaggedAndWildWords()
    {
        var service = new WordleService(new FakeWordService());

        var coverage = service.GetMetadataCoverage(5);

        Assert.Equal(5, coverage.WordLength);
        Assert.Equal(7, coverage.TotalWords);
        Assert.Equal(6, coverage.TaggedWords);
        Assert.Equal(1, coverage.WildDictionaryWords);
        Assert.True(coverage.TaggedPercent > 0);
    }

    [Fact]
    public void GetModeCoverage_CountsModeAnswerCandidates()
    {
        var service = new WordleService(new FakeWordService());

        var classic = service.GetModeCoverage(WordleGameMode.Classic, 5);
        var wild = service.GetModeCoverage(WordleGameMode.WildDictionary, 5);
        var rare = service.GetModeCoverage(WordleGameMode.RareHunt, 5);

        Assert.Equal(6, classic.CandidateCount);
        Assert.Equal(7, wild.CandidateCount);
        Assert.Equal(2, rare.CandidateCount);
    }

    [Theory]
    [InlineData(WordleGameMode.Classic)]
    [InlineData(WordleGameMode.Hard)]
    [InlineData(WordleGameMode.Daily)]
    [InlineData(WordleGameMode.Alphabet)]
    [InlineData(WordleGameMode.Timed)]
    [InlineData(WordleGameMode.Ghost)]
    [InlineData(WordleGameMode.VsCpu)]
    [InlineData(WordleGameMode.WordsRoyale)]
    [InlineData(WordleGameMode.MegaBoard)]
    [InlineData(WordleGameMode.Survival)]
    [InlineData(WordleGameMode.RareHunt)]
    [InlineData(WordleGameMode.Discovery)]
    [InlineData(WordleGameMode.WildDictionary)]
    public void StartSession_AllModes_ReturnPlayableSession(WordleGameMode mode)
    {
        var service = new WordleService(new FakeWordService());

        var session = service.StartSession(mode, wordLength: 5, maxAttempts: 6);
        var result = service.SubmitGuess(session, session.Answer, mode);

        Assert.Equal(5, session.WordLength);
        Assert.True(result.IsCorrect);
        Assert.Equal(GameStatus.Won, session.Status);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void StartDailySession_SupportedLengths_ReturnsPlayableSession(int wordLength)
    {
        var service = new WordleService(new FakeWordService());

        var session = service.StartDailySession(new DateOnly(2026, 7, 18), wordLength);
        var result = service.SubmitGuess(session, session.Answer, WordleGameMode.Daily);

        Assert.Equal(wordLength, session.WordLength);
        Assert.True(result.IsCorrect);
    }

    [Fact]
    public void StartAlphabetSession_ReturnsAnswerWithRequiredFirstLetterAndReducedAttempts()
    {
        var service = new WordleService(new FakeWordService());

        var session = service.StartAlphabetSession('C', wordLength: 5, maxAttempts: 5);

        Assert.StartsWith("C", session.Answer);
        Assert.Equal(5, session.MaxAttempts);
    }

    [Fact]
    public void StartMegaBoardRun_ReturnsMultiplePlayableBoards()
    {
        var service = new WordleService(new FakeWordService());

        var run = service.StartMegaBoardRun(boardCount: 3, wordLength: 5, maxAttempts: 8);
        var results = service.SubmitGuess(run, "CRANE");

        Assert.Equal(3, run.BoardCount);
        Assert.Equal(8, run.FocusBoard.MaxAttempts);
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void StartMegaBoardRun_AllowsBoardCountLargerThanUniquePool()
    {
        var service = new WordleService(new FakeWordService());

        var run = service.StartMegaBoardRun(boardCount: 10, wordLength: 5, maxAttempts: 8);

        Assert.Equal(10, run.BoardCount);
    }

    private sealed class FakeWordService : IWordService
    {
        private static readonly string[] Words = ["WORD", "WILD", "ABLE", "XYLYL", "CRANE", "CRATE", "SLATE", "MOUND", "PRISM", "ROBOT", "XYLYLS"];

        public Word GetRandomWord(WordCategory category, GameDifficulty difficulty) =>
            new("CRANE", category, difficulty, "test");

        public IReadOnlyList<Word> GetAllWords() => [];

        public string GetRandomWord(int length) => Words.First(word => word.Length == length);

        public IReadOnlyList<string> GetRandomWords(int length, int count) =>
            Words.Where(word => word.Length == length).Take(count).ToList();

        public IReadOnlyList<string> GetWordsByLength(int length) =>
            Words.Where(word => word.Length == length).ToList();

        public bool IsValidDictionaryWord(string word) =>
            Words.Contains(word.ToUpperInvariant());

        public WordMetadata GetWordMetadata(string word)
        {
            var normalized = word.ToUpperInvariant();
            return normalized switch
            {
                "XYLYL" => new(normalized, WordCommonality.WildDictionary, "Wild Dictionary", "test"),
                "CRATE" => new(normalized, WordCommonality.Rare, "Rare", "test"),
                _ => new(normalized, WordCommonality.Common, "Common", "test")
            };
        }
    }
}
