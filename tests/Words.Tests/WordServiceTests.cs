using Words.Core.Models;
using Words.Core.Services;

namespace Words.Tests;

public class WordServiceTests
{
    private static readonly IReadOnlyList<Word> SampleWords = new List<Word>
    {
        new("ELEPHANT", WordCategory.Animals,  GameDifficulty.Medium, "Large land animal"),
        new("CAT",      WordCategory.Animals,  GameDifficulty.Easy,   "Common pet"),
        new("PIZZA",    WordCategory.Food,     GameDifficulty.Easy,   "Italian dish"),
        new("ALGORITHM",WordCategory.Technology,GameDifficulty.Hard, "Step-by-step procedure")
    };

    [Fact]
    public void GetAllWords_ReturnsAllWords()
    {
        var service = new WordService(SampleWords);
        Assert.Equal(4, service.GetAllWords().Count);
    }

    [Fact]
    public void GetRandomWord_MatchingFilter_ReturnsCorrectWord()
    {
        var service = new WordService(SampleWords);
        var word = service.GetRandomWord(WordCategory.Food, GameDifficulty.Easy);
        Assert.Equal("PIZZA", word.Text);
    }

    [Fact]
    public void GetRandomWord_NoMatch_ThrowsInvalidOperation()
    {
        var service = new WordService(SampleWords);
        Assert.Throws<InvalidOperationException>(() =>
            service.GetRandomWord(WordCategory.Sports, GameDifficulty.Easy));
    }

    [Fact]
    public void FromEmbeddedResource_LoadsWords()
    {
        var service = WordService.FromEmbeddedResource();
        Assert.NotEmpty(service.GetAllWords());
    }

    [Fact]
    public void GetRandomWordByLength_ReturnsRequestedLength()
    {
        var service = WordService.FromEmbeddedResource(new Random(0));
        var word = service.GetRandomWord(4);
        Assert.Equal(4, word.Length);
    }

    [Fact]
    public void GetWordsByLength_ReturnsOnlyRequestedLength()
    {
        var service = WordService.FromEmbeddedResource();
        var words = service.GetWordsByLength(8);
        Assert.NotEmpty(words);
        Assert.All(words, word => Assert.Equal(8, word.Length));
    }
}
