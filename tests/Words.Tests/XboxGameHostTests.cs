using System.IO;
using Words.Core.Models;
using Words.Core.Services;
using Words.Xbox;

namespace Words.Tests;

public class XboxGameHostTests
{
    [Fact]
    public void Run_LeaderboardMenu_ShowsScoresAndExits()
    {
        var scoreService = new ScoreService();
        scoreService.AwardPoints(new Player("HighScore"), 250);

        var wordService = new WordService(new[]
        {
            new Word("DRAGON", WordCategory.General, GameDifficulty.Medium, "Fire-breathing creature")
        });
        var gameService = new GameService(wordService, scoreService);

        using var input = new StringReader("PlayerOne\n3\nq\n");
        using var output = new StringWriter();
        var host = new XboxGameHost(gameService, scoreService, input, output);

        host.Run();

        var text = output.ToString();
        Assert.Contains("=== Leaderboard ===", text);
        Assert.Contains("HighScore", text);
        Assert.Contains("Thanks for playing, PlayerOne!", text);
    }

    [Fact]
    public void Run_DemoMode_CompletesRepeatableRound()
    {
        using var output = new StringWriter();

        DemoRunner.Run(output);

        var text = output.ToString();
        Assert.Contains("=== Demo Tour ===", text);
        Assert.Contains("A full, scripted walkthrough of menu pages and gameplay flows.", text);
        Assert.Contains("Hint: A feature-length film shown in cinemas", text);
        Assert.Contains("🎉 You guessed 'MOVIE'! +160 points", text);
        Assert.Contains("💀 Out of guesses! The word was 'DRAGON'.", text);
        Assert.Contains("=== Multiplayer ===", text);
        Assert.Contains("=== Statistics ===", text);
        Assert.Contains("=== Settings ===", text);
        Assert.Contains("=== Achievements ===", text);
        Assert.Contains("=== Credits ===", text);
        Assert.Contains("=== Leaderboard ===", text);
        Assert.Contains("DemoPlayer", text);
        Assert.Contains("Thanks for playing, DemoPlayer!", text);
    }

    [Fact]
    public void Run_PlayMenu_AllowsNumericSelections()
    {
        var scoreService = new ScoreService();
        var wordService = new WordService(new[]
        {
            new Word("DOG", WordCategory.Animals, GameDifficulty.Easy, "A common pet")
        });
        var gameService = new GameService(wordService, scoreService);

        using var input = new StringReader("PlayerOne\n1\n1\n1\n2\nd\no\ng\nb\nq\n");
        using var output = new StringWriter();
        var host = new XboxGameHost(gameService, scoreService, input, output);

        host.Run();

        var text = output.ToString();
        Assert.Contains("Main menu:", text);
        Assert.Contains("1) Single player", text);
        Assert.Contains("Select difficulty:", text);
        Assert.Contains("1) Easy", text);
        Assert.Contains("Select category:", text);
        Assert.Contains("2) Animals", text);
        Assert.Contains("🎉 You guessed 'DOG'!", text);
    }

    [Fact]
    public void Run_PlaySetup_BackOptionReturnsToMainMenu()
    {
        var scoreService = new ScoreService();
        var wordService = new WordService(new[]
        {
            new Word("DRAGON", WordCategory.General, GameDifficulty.Medium, "Fire-breathing creature")
        });
        var gameService = new GameService(wordService, scoreService);

        using var input = new StringReader("PlayerOne\n1\nb\nq\n");
        using var output = new StringWriter();
        var host = new XboxGameHost(gameService, scoreService, input, output);

        host.Run();

        var text = output.ToString();
        Assert.Contains("B) Back", text);
        Assert.Contains("=== Single player ===", text);
    }

    [Fact]
    public void Run_MultiplayerHeadToHead_CanCreateLobby()
    {
        var scoreService = new ScoreService();
        var wordService = new WordService(new[]
        {
            new Word("DRAGON", WordCategory.General, GameDifficulty.Medium, "Fire-breathing creature")
        });
        var gameService = new GameService(wordService, scoreService);

        using var input = new StringReader("PlayerOne\n2\n3\n1\nb\nb\nq\n");
        using var output = new StringWriter();
        var host = new XboxGameHost(gameService, scoreService, input, output);

        host.Run();

        var text = output.ToString();
        Assert.Contains("=== Head to Head ===", text);
        Assert.Contains("Create lobby", text);
        Assert.Contains("Lobby created. Share code:", text);
        Assert.Contains("Players: PlayerOne", text);
    }

    [Fact]
    public void Run_TournamentJoin_ValidatesCodeAndJoins()
    {
        var scoreService = new ScoreService();
        var wordService = new WordService(new[]
        {
            new Word("DRAGON", WordCategory.General, GameDifficulty.Medium, "Fire-breathing creature")
        });
        var gameService = new GameService(wordService, scoreService);

        using var input = new StringReader("PlayerOne\n2\n6\n1\nabc\n1\nABC123\nb\nb\nq\n");
        using var output = new StringWriter();
        var host = new XboxGameHost(gameService, scoreService, input, output);

        host.Run();

        var text = output.ToString();
        Assert.Contains("=== Tournament ===", text);
        Assert.Contains("Invalid code format. Use 6 letters or numbers.", text);
        Assert.Contains("PlayerOne created and joined lobby ABC123 for Tournament.", text);
        Assert.Contains("Waiting for players: 1/4 ready.", text);
    }

    [Fact]
    public void Run_SinglePlayerHard_UsesHardModeFlow()
    {
        var scoreService = new ScoreService();
        var wordService = new WordService(new[]
        {
            new Word("DRAGON", WordCategory.General, GameDifficulty.Hard, "Fire-breathing creature")
        });
        var gameService = new GameService(wordService, scoreService);

        using var input = new StringReader("PlayerOne\n1\n2\n1\nd\nr\na\ng\no\nn\nb\nq\n");
        using var output = new StringWriter();
        var host = new XboxGameHost(gameService, scoreService, input, output);

        host.Run();

        var text = output.ToString();
        Assert.Contains("Hard mode: tougher words and tighter margins.", text);
        Assert.Contains("Select category:", text);
        Assert.Contains("🎉 You guessed 'DRAGON'!", text);
    }

    [Fact]
    public void Run_MultiplayerLocalVs_OpensLobbyActions()
    {
        var scoreService = new ScoreService();
        var wordService = new WordService(new[]
        {
            new Word("DRAGON", WordCategory.General, GameDifficulty.Medium, "Fire-breathing creature")
        });
        var gameService = new GameService(wordService, scoreService);

        using var input = new StringReader("PlayerOne\n2\n2\n1\nb\nb\nq\n");
        using var output = new StringWriter();
        var host = new XboxGameHost(gameService, scoreService, input, output);

        host.Run();

        var text = output.ToString();
        Assert.Contains("=== Local Vs ===", text);
        Assert.Contains("Create lobby", text);
    }

    [Fact]
    public void Run_SinglePlayerAlphabet_EnforcesForwardGuessOrder()
    {
        var scoreService = new ScoreService();
        var wordService = new WordService(new[]
        {
            new Word("ACE", WordCategory.General, GameDifficulty.Easy, "A high card")
        });
        var gameService = new GameService(wordService, scoreService);

        using var input = new StringReader("PlayerOne\n1\n5\n1\n1\nz\ny\nx\nw\nv\nu\nb\nq\n");
        using var output = new StringWriter();
        var host = new XboxGameHost(gameService, scoreService, input, output);

        host.Run();

        var text = output.ToString();
        Assert.Contains("A-Z challenge: clear one word per letter from A to Z.", text);
        Assert.Contains("Alphabet bonus: first letter revealed for free (and one fewer guess).", text);
        Assert.Contains("A-Z challenge letter: A", text);
        Assert.Contains("A-Z challenge ended at letter A.", text);
    }

    [Fact]
    public void Run_SinglePlayerWordHunt_ShowsThreeRoundFlow()
    {
        var scoreService = new ScoreService();
        var wordService = new WordService(new[]
        {
            new Word("DOG", WordCategory.Animals, GameDifficulty.Easy, "A common pet")
        });
        var gameService = new GameService(wordService, scoreService);

        using var input = new StringReader(
            "PlayerOne\n1\n10\n1\n2\nd\no\ng\n1\n2\nd\no\ng\n1\n2\nd\no\ng\nb\nq\n");
        using var output = new StringWriter();
        var host = new XboxGameHost(gameService, scoreService, input, output);

        host.Run();

        var text = output.ToString();
        Assert.Contains("Word Hunt round 1/3", text);
        Assert.Contains("Word Hunt round 2/3", text);
        Assert.Contains("Word Hunt round 3/3", text);
    }

    [Fact]
    public void Run_Settings_CanToggleHintsAndUnlockPuristAchievement()
    {
        var scoreService = new ScoreService();
        var wordService = new WordService(new[]
        {
            new Word("DOG", WordCategory.Animals, GameDifficulty.Easy, "A common pet")
        });
        var gameService = new GameService(wordService, scoreService);

        using var input = new StringReader("PlayerOne\n5\n1\nb\n6\nq\n");
        using var output = new StringWriter();
        var host = new XboxGameHost(gameService, scoreService, input, output);

        host.Run();

        var text = output.ToString();
        Assert.Contains("=== Settings ===", text);
        Assert.Contains("Hints set to Off.", text);
        Assert.Contains("[Unlocked] Purist", text);
    }

    [Fact]
    public void Run_Statistics_ReflectsSettingsState()
    {
        var scoreService = new ScoreService();
        var wordService = new WordService(new[]
        {
            new Word("DOG", WordCategory.Animals, GameDifficulty.Easy, "A common pet")
        });
        var gameService = new GameService(wordService, scoreService);

        using var input = new StringReader("PlayerOne\n5\n2\n4\n3\nb\n4\nq\n");
        using var output = new StringWriter();
        var host = new XboxGameHost(gameService, scoreService, input, output);

        host.Run();

        var text = output.ToString();
        Assert.Contains("Sound set to Off.", text);
        Assert.Contains("Theme set to Dark.", text);
        Assert.Contains("Sound enabled: No", text);
        Assert.Contains("Theme: Dark", text);
        Assert.Contains("Top mode: N/A", text);
    }

    [Fact]
    public void Run_Statistics_TracksLobbyPlayersCount()
    {
        var scoreService = new ScoreService();
        var wordService = new WordService(new[]
        {
            new Word("DOG", WordCategory.Animals, GameDifficulty.Easy, "A common pet")
        });
        var gameService = new GameService(wordService, scoreService);

        using var input = new StringReader("PlayerOne\n2\n3\n1\nb\nb\n4\nq\n");
        using var output = new StringWriter();
        var host = new XboxGameHost(gameService, scoreService, input, output);

        host.Run();

        var text = output.ToString();
        Assert.Contains("Open lobbies: 1", text);
        Assert.Contains("Lobby players: 1", text);
    }

    [Fact]
    public void Run_WordsRoyaleJoin_CreatesTrackedLobby()
    {
        var scoreService = new ScoreService();
        var wordService = new WordService(new[]
        {
            new Word("DOG", WordCategory.Animals, GameDifficulty.Easy, "A common pet")
        });
        var gameService = new GameService(wordService, scoreService);

        using var input = new StringReader("PlayerOne\n2\n7\n1\nROYALE\nb\nb\n4\nq\n");
        using var output = new StringWriter();
        var host = new XboxGameHost(gameService, scoreService, input, output);

        host.Run();

        var text = output.ToString();
        Assert.Contains("PlayerOne created and joined lobby ROYALE for Words Royale.", text);
        Assert.Contains("Waiting for players: 1/8 ready.", text);
        Assert.Contains("Open lobbies: 1", text);
    }

    [Fact]
    public void Run_MultiplayerHeadToHead_AddGuest_ReadiesMatchAndUnlocksPartyStarter()
    {
        var scoreService = new ScoreService();
        var wordService = new WordService(new[]
        {
            new Word("DOG", WordCategory.Animals, GameDifficulty.Easy, "A common pet")
        });
        var gameService = new GameService(wordService, scoreService);

        using var input = new StringReader("PlayerOne\n2\n3\n1\n4\nPlayerTwo\nb\nb\n6\nq\n");
        using var output = new StringWriter();
        var host = new XboxGameHost(gameService, scoreService, input, output);

        host.Run();

        var text = output.ToString();
        Assert.Contains("PlayerTwo joined lobby", text);
        Assert.Contains("Match ready in", text);
        Assert.Contains("[Unlocked] Party Starter", text);
    }

    [Fact]
    public void Run_MultiplayerHeadToHead_StartMatch_TracksStatsAndAchievement()
    {
        var scoreService = new ScoreService();
        var wordService = new WordService(new[]
        {
            new Word("DOG", WordCategory.Animals, GameDifficulty.Easy, "A common pet")
        });
        var gameService = new GameService(wordService, scoreService);

        using var input = new StringReader("PlayerOne\n2\n3\n1\n4\nPlayerTwo\n5\nb\nb\n4\n6\nq\n");
        using var output = new StringWriter();
        var host = new XboxGameHost(gameService, scoreService, input, output);

        host.Run();

        var text = output.ToString();
        Assert.Contains("Starting Head to Head match in lobby", text);
        Assert.Contains("is now in-game.", text);
        Assert.Contains("Matches started: 1", text);
        Assert.Contains("[Unlocked] Competitor", text);
    }

    [Fact]
    public void Run_Statistics_TracksSinglePlayerWinsAndTopMode()
    {
        var scoreService = new ScoreService();
        var wordService = new WordService(new[]
        {
            new Word("DOG", WordCategory.Animals, GameDifficulty.Easy, "A common pet")
        });
        var gameService = new GameService(wordService, scoreService);

        using var input = new StringReader("PlayerOne\n1\n1\n1\n2\nd\no\ng\nb\n4\nq\n");
        using var output = new StringWriter();
        var host = new XboxGameHost(gameService, scoreService, input, output);

        host.Run();

        var text = output.ToString();
        Assert.Contains("Single-player mode wins: 1", text);
        Assert.Contains("Top mode: Classic (1)", text);
    }

    [Fact]
    public void Run_DailyChallenge_TracksCompletionAndPreventsReplaySameDay()
    {
        var scoreService = new ScoreService();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var categories = Enum.GetValues<WordCategory>();
        var category = categories[today.DayNumber % categories.Length];
        var difficulty = (today.DayNumber % 3) switch
        {
            0 => GameDifficulty.Easy,
            1 => GameDifficulty.Medium,
            _ => GameDifficulty.Hard
        };

        var wordService = new WordService(new[]
        {
            new Word("DOG", category, difficulty, "A common pet")
        });
        var gameService = new GameService(wordService, scoreService);

        using var input = new StringReader("PlayerOne\n1\n3\nd\no\ng\n3\nb\n4\n6\nq\n");
        using var output = new StringWriter();
        var host = new XboxGameHost(gameService, scoreService, input, output);

        host.Run();

        var text = output.ToString();
        Assert.Contains("Daily streak: 1 (best 1)", text);
        Assert.Contains("Daily challenge already completed today. Come back tomorrow!", text);
        Assert.Contains("Daily completions: 1", text);
        Assert.Contains("[Locked] Daily Devotee", text);
    }

    [Fact]
    public void Run_GhostMode_ShowsCorrectLetterCountOnly()
    {
        var scoreService = new ScoreService();
        var wordService = new WordService(new[]
        {
            new Word("DOG", WordCategory.Animals, GameDifficulty.Easy, "A common pet")
        });
        var gameService = new GameService(wordService, scoreService);

        using var input = new StringReader("PlayerOne\n1\n7\n1\n2\nd\no\ng\nb\nq\n");
        using var output = new StringWriter();
        var host = new XboxGameHost(gameService, scoreService, input, output);

        host.Run();

        var text = output.ToString();
        Assert.Contains("Ghost mode: shows how many letters are correct, but not which letters.", text);
        Assert.Contains("Correct letters:", text);
    }

    [Fact]
    public void Run_SpeedrunMode_IsRaceToTenWords()
    {
        var scoreService = new ScoreService();
        var wordService = new WordService(new[]
        {
            new Word("A", WordCategory.General, GameDifficulty.Easy, "First letter")
        });
        var gameService = new GameService(wordService, scoreService);

        using var input = new StringReader("PlayerOne\n1\n4\n1\n1\na\na\na\na\na\na\na\na\na\na\nb\nq\n");
        using var output = new StringWriter();
        var host = new XboxGameHost(gameService, scoreService, input, output);

        host.Run();

        var text = output.ToString();
        Assert.Contains("Speedrun: race to complete 10 words. Power-ups are disabled.", text);
        Assert.Contains("Speedrun complete! 10 words solved in 10 rounds.", text);
    }

    [Fact]
    public void Run_MultiplayerHostCanDisbandLobby()
    {
        var scoreService = new ScoreService();
        var wordService = new WordService(new[]
        {
            new Word("DOG", WordCategory.Animals, GameDifficulty.Easy, "A common pet")
        });
        var gameService = new GameService(wordService, scoreService);

        using var input = new StringReader("PlayerOne\n2\n3\n1\n7\nb\nb\n4\nq\n");
        using var output = new StringWriter();
        var host = new XboxGameHost(gameService, scoreService, input, output);

        host.Run();

        var text = output.ToString();
        Assert.Contains("Lobby ", text);
        Assert.Contains("disbanded.", text);
        Assert.Contains("Open lobbies: 0", text);
    }

    [Fact]
    public void Run_MultiplayerCanListMultipleOpenLobbies()
    {
        var scoreService = new ScoreService();
        var wordService = new WordService(new[]
        {
            new Word("DOG", WordCategory.Animals, GameDifficulty.Easy, "A common pet")
        });
        var gameService = new GameService(wordService, scoreService);

        using var input = new StringReader("PlayerOne\n2\n3\n1\n1\n8\nb\nb\nq\n");
        using var output = new StringWriter();
        var host = new XboxGameHost(gameService, scoreService, input, output);

        host.Run();

        var text = output.ToString();
        Assert.Contains("Open lobbies for Head to Head:", text);
        Assert.Contains("players: 1/2", text);
    }

    [Fact]
    public void Run_Survival_TracksBestStreakAndUnlocksIronWill()
    {
        var scoreService = new ScoreService();
        var wordService = new WordService(new[]
        {
            new Word("DOG", WordCategory.Animals, GameDifficulty.Easy, "A common pet")
        });
        var gameService = new GameService(wordService, scoreService);

        using var input = new StringReader("PlayerOne\n1\n9\n1\n2\nd\no\ng\n1\n1\n2\nd\no\ng\n2\nb\n4\n6\nq\n");
        using var output = new StringWriter();
        var host = new XboxGameHost(gameService, scoreService, input, output);

        host.Run();

        var text = output.ToString();
        Assert.Contains("Survival streak: 1", text);
        Assert.Contains("Survival streak: 2", text);
        Assert.Contains("Best survival streak: 2", text);
        Assert.Contains("[Unlocked] Iron Will", text);
    }
}
