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
    public void FromEmbeddedResource_HasBalancedPoolAcrossCategoriesAndDifficulties()
    {
        var service = WordService.FromEmbeddedResource();
        var words = service.GetAllWords();

        foreach (var category in Enum.GetValues<WordCategory>())
        {
            foreach (var difficulty in Enum.GetValues<GameDifficulty>())
            {
                var count = words.Count(word => word.Category == category && word.Difficulty == difficulty);
                Assert.True(count >= 2, $"Expected at least 2 words for {category}/{difficulty}, but found {count}.");
            }
        }
    }

    [Fact]
    public void FromEmbeddedResource_HasNoDuplicateWords()
    {
        var service = WordService.FromEmbeddedResource();
        var duplicates = service.GetAllWords()
            .GroupBy(word => word.Text, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void FromEmbeddedResource_WordsAreUppercaseLettersAndHaveUsefulHints()
    {
        var service = WordService.FromEmbeddedResource();

        foreach (var word in service.GetAllWords())
        {
            Assert.Matches("^[A-Z]+$", word.Text);
            Assert.True(word.Hint.Length >= 12, $"Hint for {word.Text} is too short.");
            Assert.DoesNotContain(word.Text, word.Hint, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void FromEmbeddedResource_LoadsCompressedDictionary()
    {
        var service = WordService.FromEmbeddedResource();

        var fiveLetterWords = service.GetWordsByLength(5);

        Assert.True(fiveLetterWords.Count > 1000);
        Assert.True(service.IsValidDictionaryWord(fiveLetterWords[0]));
    }

    [Fact]
    public void GetWordMetadata_CuratedEasyWord_ReturnsCommonMetadata()
    {
        var service = new WordService(SampleWords);

        var metadata = service.GetWordMetadata("cat");

        Assert.Equal("CAT", metadata.Text);
        Assert.Equal(WordCommonality.Common, metadata.Commonality);
        Assert.Equal("Common", metadata.Label);
        Assert.Equal("Common pet", metadata.Description);
    }

    [Fact]
    public void GetWordMetadata_DictionaryOnlyWord_ReturnsWildDictionaryMetadata()
    {
        var service = WordService.FromEmbeddedResource();
        var dictionaryOnlyWord = service.GetWordsByLength(5)
            .First(word =>
            {
                var metadata = service.GetWordMetadata(word);
                return metadata.Commonality == WordCommonality.WildDictionary;
            });

        var metadata = service.GetWordMetadata(dictionaryOnlyWord);

        Assert.Equal(WordCommonality.WildDictionary, metadata.Commonality);
        Assert.Equal("Wild Dictionary", metadata.Label);
    }

    [Fact]
    public void GetWordMetadata_EmbeddedMetadataWord_ReturnsFrequencyMetadata()
    {
        var service = WordService.FromEmbeddedResource();

        var metadata = service.GetWordMetadata("water");

        Assert.Equal("WATER", metadata.Text);
        Assert.Equal(WordCommonality.Common, metadata.Commonality);
        Assert.Equal("Common", metadata.Label);
        Assert.Contains("everyday", metadata.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetWordMetadata_EmbeddedLongerMetadataWord_ReturnsFrequencyMetadata()
    {
        var service = WordService.FromEmbeddedResource();

        var metadata = service.GetWordMetadata("family");

        Assert.Equal("FAMILY", metadata.Text);
        Assert.Equal(WordCommonality.Common, metadata.Commonality);
        Assert.Equal("Common", metadata.Label);
    }

    [Theory]
    [InlineData(4, 10)]
    [InlineData(5, 50)]
    [InlineData(6, 20)]
    [InlineData(7, 2)]
    [InlineData(8, 1)]
    public void FromEmbeddedResource_HasSeededSafeAnswerMetadataByLength(int wordLength, int minimumTaggedWords)
    {
        var service = WordService.FromEmbeddedResource();

        var taggedWords = service.GetWordsByLength(wordLength)
            .Count(word => service.GetWordMetadata(word).Commonality != WordCommonality.WildDictionary);

        Assert.True(
            taggedWords >= minimumTaggedWords,
            $"Expected at least {minimumTaggedWords} tagged {wordLength}-letter words, but found {taggedWords}.");
    }
}
