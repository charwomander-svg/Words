using Words.Core.Models;
using Words.Core.Services;
using Words.Xbox.Presentation;

namespace Words.Tests;

public class XboxPresentationTests
{
    [Fact]
    public void ComposeTitleScreen_ShowsMenuAndAccessibilityNotes()
    {
        var text = XboxPresentation.ComposeTitleScreen(new Player("ArcadeAce"), frameIndex: 1, reducedMotion: false);

        Assert.Contains("WORDS", text);
        Assert.Contains("Menu:", text);
        Assert.Contains("Tutorial", text);
        Assert.Contains("Privacy", text);
        Assert.Contains("Accessibility", text);
    }

    [Fact]
    public void ComposeInfoPages_UseSharedTemplate()
    {
        var tutorial = XboxPresentation.ComposeTutorialPage();
        var privacy = XboxPresentation.ComposePrivacyPage();
        var legal = XboxPresentation.ComposeLegalPage();

        Assert.Contains("TUTORIAL", tutorial);
        Assert.Contains("PRIVACY POLICY", privacy);
        Assert.Contains("LEGALESE", legal);
        Assert.StartsWith("╔", tutorial);
        Assert.StartsWith("╔", privacy);
        Assert.StartsWith("╔", legal);
    }

    [Fact]
    public void ComposeRoundScreen_ShowsKeyboardAndStatus()
    {
        var wordService = new WordService(new[]
        {
            new Word("DRAGON", WordCategory.General, GameDifficulty.Medium, "Fire-breathing creature")
        });
        var session = new GameService(wordService, new ScoreService())
            .StartGame(new Player("ArcadeAce"), new GameConfig { WordLength = 6 });

        var text = XboxPresentation.ComposeRoundScreen(session, pendingGuess: "DR", selectedIndex: 0);

        Assert.Contains("GAME SCREEN", text);
        Assert.Contains("Keyboard:", text);
        Assert.Contains("[Q]", text);
        Assert.Contains("Current guess: DR", text);
    }
}
