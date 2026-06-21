using Words.Core.Models;
using Words.Core.Interfaces;
using Words.Core.Services;

namespace Words.Tests;

public class PvpServiceTests
{
    [Fact]
    public void StartMatch_CreatesSharedWordMatch()
    {
        var service = new PvpService(WordService.FromEmbeddedResource(new Random(0)));
        var match = service.StartMatch(new Player("Alpha"), new Player("Bravo"), 5);

        Assert.Equal(PvpMatchStatus.InProgress, match.Status);
        Assert.Equal(match.PlayerOneSession.PrimaryWord.Text, match.PlayerTwoSession.PrimaryWord.Text);
        Assert.Equal(1, match.TargetWins);
    }

    [Fact]
    public void StartMatch_CanRaceToMultipleWins()
    {
        var service = new PvpService(new SequencedWordService("GAME", "MATH"));

        var match = service.StartMatch(new Player("Alpha"), new Player("Bravo"), 4, targetWins: 2);

        foreach (var letter in "GAME")
            service.SubmitGuess(match.Id, "Alpha", letter);

        Assert.Equal(PvpMatchStatus.InProgress, match.Status);
        Assert.Equal(1, match.PlayerOneWins);
        Assert.Equal(2, match.RoundNumber);
        Assert.Equal("Alpha", match.PlayerOneSession.Player.GamerTag);

        foreach (var letter in "MATH")
            service.SubmitGuess(match.Id, "Alpha", letter);

        Assert.Equal(PvpMatchStatus.Completed, match.Status);
        Assert.Equal("Alpha", match.Winner?.GamerTag);
        Assert.Equal(2, match.PlayerOneWins);
        Assert.Equal(2, match.RoundNumber);
    }

    [Fact]
    public void StartMatch_TargetWinsNull_LeavesMatchOpenEnded()
    {
        var service = new PvpService(new SequencedWordService("GAME", "MATH"));

        var match = service.StartMatch(new Player("Alpha"), new Player("Bravo"), 4, targetWins: null);

        foreach (var letter in "GAME")
            service.SubmitGuess(match.Id, "Alpha", letter);

        Assert.Equal(PvpMatchStatus.InProgress, match.Status);
        Assert.Equal(1, match.PlayerOneWins);
        Assert.Equal(2, match.RoundNumber);
    }

    [Fact]
    public void SubmitGuess_FirstWinner_EndsMatch()
    {
        var service = new PvpService(new WordService(new List<Word>
        {
            new("GAME", WordCategory.General, GameDifficulty.Medium, "hint")
        }));

        var match = service.StartMatch(new Player("Alpha"), new Player("Bravo"), 4);
        foreach (var letter in "GAME")
            service.SubmitGuess(match.Id, "Alpha", letter);

        Assert.Equal(PvpMatchStatus.Completed, match.Status);
        Assert.Equal("Alpha", match.Winner?.GamerTag);
        Assert.Equal(1, match.PlayerOneWins);
        Assert.True(match.PlayerOne.ExperiencePoints > 0);
    }

    [Fact]
    public void StartMatch_DisablesHints()
    {
        var service = new PvpService(new WordService(new List<Word>
        {
            new("GAME", WordCategory.General, GameDifficulty.Medium, "hint")
        }));

        var match = service.StartMatch(new Player("Alpha"), new Player("Bravo"), 4);

        Assert.False(match.PlayerOneSession.CanUseHints);
        Assert.Equal("Hints are disabled in this mode.", match.PlayerOneSession.RequestHint().Message);
    }

    private sealed class SequencedWordService : IWordService
    {
        private readonly Queue<string> _words;

        public SequencedWordService(params string[] words) => _words = new Queue<string>(words);

        public Word GetRandomWord(WordCategory category, GameDifficulty difficulty) =>
            throw new NotSupportedException();

        public IReadOnlyList<Word> GetAllWords() => [];

        public string GetRandomWord(int length) =>
            _words.Count > 0 ? _words.Dequeue() : throw new InvalidOperationException("No more test words.");

        public IReadOnlyList<string> GetRandomWords(int length, int count) => throw new NotSupportedException();

        public IReadOnlyList<string> GetWordsByLength(int length) => _words.ToArray();
    }
}
