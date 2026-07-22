using Words.Core.Models;
using Words.Core.Services;
using Words.Core.Interfaces;

namespace Words.Xbox;

/// <summary>
/// Xbox game host: drives a console-based "Guess That Word" game loop.
/// On a real Xbox title this class would be wired into the game engine's
/// update/render cycle rather than a console read loop.
/// </summary>
public class XboxGameHost
{
    private readonly GameService _gameService;
    private readonly IScoreService _scoreService;
    private readonly TextReader _input;
    private readonly TextWriter _output;
    private readonly Dictionary<string, Lobby> _lobbies = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<SinglePlayerMode, int> _singlePlayerModePlays = new();
    private readonly Dictionary<SinglePlayerMode, int> _singlePlayerModeWins = new();
    private readonly Dictionary<MultiplayerMode, int> _multiplayerModeVisits = new();
    private int _matchesStarted;
    private DateOnly? _lastDailyCompletionDate;
    private int _dailyCompletions;
    private int _dailyStreak;
    private int _bestDailyStreak;
    private int _lastSurvivalStreak;
    private int _bestSurvivalStreak;
    private bool _showHints = true;
    private bool _soundEnabled = true;
    private DifficultyPreference _difficultyPreference = DifficultyPreference.Adaptive;
    private Theme _theme = Theme.System;

    public XboxGameHost(GameService gameService, IScoreService scoreService, TextReader? input = null, TextWriter? output = null)
    {
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
        _scoreService = scoreService ?? throw new ArgumentNullException(nameof(scoreService));
        _input = input ?? Console.In;
        _output = output ?? Console.Out;
    }

    /// <summary>
    /// Runs the game loop until the player decides to quit.
    /// </summary>
    public void Run()
    {
        WriteBanner();

        var player = CreatePlayer();

        bool quit = false;
        while (!quit)
        {
            switch (PromptMainMenuChoice(player))
            {
                case MainMenuChoice.SinglePlayer:
                    ShowSinglePlayerMenu(player);
                    break;
                case MainMenuChoice.Multiplayer:
                    ShowMultiplayerMenu(player);
                    break;
                case MainMenuChoice.Leaderboards:
                    ShowLeaderboard();
                    break;
                case MainMenuChoice.Statistics:
                    ShowStatistics(player);
                    break;
                case MainMenuChoice.Settings:
                    ShowSettings();
                    break;
                case MainMenuChoice.Achievements:
                    ShowAchievements(player);
                    break;
                case MainMenuChoice.Credits:
                    ShowCredits();
                    break;
                case null:
                    quit = true;
                    break;
            }
        }

        _output.WriteLine($"\nThanks for playing, {player.GamerTag}!");
        _output.WriteLine($"Final score: {player.Score}  |  Won {player.GamesWon}/{player.GamesPlayed} games");
    }

    // -------------------------------------------------------------------------

    private void WriteBanner()
    {
        _output.WriteLine("=== Guess That Word – Xbox Edition ===");
        _output.WriteLine();
        _output.WriteLine("Guess the word one letter at a time.");
        _output.WriteLine("Pick a mode from the main menu to get started.");
    }

    private Player CreatePlayer()
    {
        _output.Write("Enter your GamerTag: ");
        var tag = _input.ReadLine()?.Trim();
        while (string.IsNullOrWhiteSpace(tag))
        {
            _output.Write("GamerTag cannot be empty. Try again: ");
            tag = _input.ReadLine()?.Trim();
        }
        return new Player(tag);
    }

    private MainMenuChoice? PromptMainMenuChoice(Player player)
    {
        while (true)
        {
            _output.WriteLine($"\nCurrent player: {player.GamerTag}  |  Score: {player.Score}");
            _output.WriteLine("Main menu:");
            _output.WriteLine("  1) Single player");
            _output.WriteLine("  2) Multiplayer");
            _output.WriteLine("  3) Leaderboards");
            _output.WriteLine("  4) Statistics");
            _output.WriteLine("  5) Settings");
            _output.WriteLine("  6) Achievements");
            _output.WriteLine("  7) Credits");
            _output.WriteLine("  Q) Quit");
            _output.Write("Choose an option: ");

            var input = _input.ReadLine()?.Trim() ?? string.Empty;
            if (input.Equals("Q", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (int.TryParse(input, out var option) && option >= 1 && option <= 7)
            {
                return (MainMenuChoice)option;
            }

            if (Enum.TryParse<MainMenuChoice>(input.Replace(" ", string.Empty), ignoreCase: true, out var parsed) &&
                Enum.IsDefined(parsed))
            {
                return parsed;
            }

            _output.WriteLine("  Invalid choice. Enter 1-7, a menu name, or Q to quit.");
        }
    }

    private void ShowSinglePlayerMenu(Player player)
    {
        while (true)
        {
            _output.WriteLine("\n=== Single player ===");
            var mode = PromptEnum<SinglePlayerMode>("Choose a mode", allowBack: true);
            if (!mode.HasValue)
            {
                return;
            }

            _singlePlayerModePlays[mode.Value] = _singlePlayerModePlays.GetValueOrDefault(mode.Value) + 1;

            switch (mode.Value)
            {
                case SinglePlayerMode.Classic:
                    var config = SelectConfig();
                    if (config is not null)
                    {
                        PlayModeRound(player, SinglePlayerMode.Classic, config);
                    }
                    break;
                case SinglePlayerMode.Hard:
                    PlayHardRound(player);
                    break;
                case SinglePlayerMode.Daily:
                    PlayDailyRound(player);
                    break;
                case SinglePlayerMode.Speedrun:
                    PlaySpeedrunRound(player);
                    break;
                case SinglePlayerMode.TimeAttack:
                    PlayTimeAttackRound(player);
                    break;
                case SinglePlayerMode.Alphabet:
                    PlayAlphabetRound(player);
                    break;
                case SinglePlayerMode.Ghost:
                    PlayGhostRound(player);
                    break;
                case SinglePlayerMode.Survival:
                    PlaySurvivalRun(player);
                    break;
                case SinglePlayerMode.VsCpu:
                    PlayVsCpuRound(player);
                    break;
                case SinglePlayerMode.WordHunt:
                    PlayWordHuntRun(player);
                    break;
                case SinglePlayerMode.WildDictionary:
                    PlayWildDictionaryRound(player);
                    break;
            }
        }
    }

    private void ShowMultiplayerMenu(Player player)
    {
        while (true)
        {
            _output.WriteLine("\n=== Multiplayer ===");
            var mode = PromptEnum<MultiplayerMode>("Choose a mode", allowBack: true);
            if (!mode.HasValue)
            {
                return;
            }

            _multiplayerModeVisits[mode.Value] = _multiplayerModeVisits.GetValueOrDefault(mode.Value) + 1;

            switch (mode.Value)
            {
                case MultiplayerMode.LocalCoOp:
                case MultiplayerMode.LocalVs:
                case MultiplayerMode.HeadToHead:
                case MultiplayerMode.Showdown:
                case MultiplayerMode.TwoVsTwo:
                    ShowLobbyModeMenu(player, mode.Value);
                    break;
                case MultiplayerMode.Tournament:
                case MultiplayerMode.WordsRoyale:
                    ShowJoinModeMenu(player, mode.Value);
                    break;
            }
        }
    }

    private void ShowLobbyModeMenu(Player player, MultiplayerMode mode)
    {
        var modeName = GetMultiplayerModeDisplayName(mode);
        while (true)
        {
            _output.WriteLine($"\n=== {modeName} ===");
            var action = PromptChoice("Choose an option", ["Create lobby", "Find lobby", "Quick match", "Add local guest", "Start match", "Leave lobby", "Disband lobby", "List lobbies"], allowBack: true);
            if (action is null)
            {
                return;
            }
            switch (action)
            {
                case "Create lobby":
                    CreateLobby(player, mode);
                    break;
                case "Find lobby":
                    FindLobby(player, mode);
                    break;
                case "Quick match":
                    QuickMatch(player, mode);
                    break;
                case "Add local guest":
                    AddLocalGuest(mode);
                    break;
                case "Start match":
                    StartMatch(mode);
                    break;
                case "Leave lobby":
                    LeaveLobby(player, mode);
                    break;
                case "Disband lobby":
                    DisbandLobby(player, mode);
                    break;
                case "List lobbies":
                    ListLobbies(mode);
                    break;
            }
        }
    }

    private void ShowJoinModeMenu(Player player, MultiplayerMode mode)
    {
        var modeName = GetMultiplayerModeDisplayName(mode);
        while (true)
        {
            _output.WriteLine($"\n=== {modeName} ===");
            var action = PromptChoice("Choose an option", ["Join", "Add local guest", "Start match", "Leave lobby", "Disband lobby", "List lobbies"], allowBack: true);
            if (action is null)
            {
                return;
            }
            switch (action)
            {
                case "Join":
                    JoinQueue(player, mode);
                    break;
                case "Add local guest":
                    AddLocalGuest(mode);
                    break;
                case "Start match":
                    StartMatch(mode);
                    break;
                case "Leave lobby":
                    LeaveLobby(player, mode);
                    break;
                case "Disband lobby":
                    DisbandLobby(player, mode);
                    break;
                case "List lobbies":
                    ListLobbies(mode);
                    break;
            }
        }
    }

    private Lobby CreateLobby(Player player, MultiplayerMode mode, string? forcedCode = null)
    {
        string code = forcedCode ?? string.Empty;
        if (string.IsNullOrWhiteSpace(forcedCode))
        {
            do
            {
                code = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            } while (_lobbies.ContainsKey(code));
        }

        var lobby = new Lobby(code, mode, player.GamerTag, [player.GamerTag]);
        _lobbies[code] = lobby;
        _output.WriteLine($"Lobby created. Share code: {code}");
        _output.WriteLine($"Players: {string.Join(", ", lobby.Players)}");
        return lobby;
    }

    private void FindLobby(Player player, MultiplayerMode mode)
    {
        _output.Write("Enter lobby code: ");
        var code = (_input.ReadLine() ?? string.Empty).Trim().ToUpperInvariant();
        if (!IsValidCode(code))
        {
            _output.WriteLine("Invalid code format. Use 6 letters or numbers.");
            return;
        }

        if (!_lobbies.TryGetValue(code, out var lobby))
        {
            _output.WriteLine("Lobby not found.");
            return;
        }

        if (lobby.Mode != mode)
        {
            _output.WriteLine($"Lobby {code} exists but is for {GetMultiplayerModeDisplayName(lobby.Mode)}.");
            return;
        }

        JoinLobby(player, lobby);
        _output.WriteLine($"Lobby found: {code} hosted by {lobby.HostGamerTag}.");
        _output.WriteLine($"Players: {string.Join(", ", lobby.Players)}");
        MaybeStartLobbyMatch(lobby);
    }

    private void QuickMatch(Player player, MultiplayerMode mode)
    {
        var lobby = _lobbies.Values.FirstOrDefault(candidate => candidate.Mode == mode);
        if (lobby is null)
        {
            _output.WriteLine("No open lobby found. Creating one for quick match...");
            lobby = CreateLobby(player, mode);
            MaybeStartLobbyMatch(lobby);
            return;
        }

        JoinLobby(player, lobby);
        _output.WriteLine($"Joined lobby {lobby.Code} hosted by {lobby.HostGamerTag}.");
        _output.WriteLine($"Players: {string.Join(", ", lobby.Players)}");
        MaybeStartLobbyMatch(lobby);
    }

    private void AddLocalGuest(MultiplayerMode mode)
    {
        var lobby = SelectLobby(mode, "Enter lobby code to add a guest");
        if (lobby is null)
        {
            return;
        }

        _output.Write("Enter guest GamerTag: ");
        var guestTag = (_input.ReadLine() ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(guestTag))
        {
            _output.WriteLine("Guest GamerTag cannot be empty.");
            return;
        }

        if (lobby.Players.Contains(guestTag, StringComparer.OrdinalIgnoreCase))
        {
            _output.WriteLine($"{guestTag} is already in lobby {lobby.Code}.");
            return;
        }

        lobby.Players.Add(guestTag);
        _output.WriteLine($"{guestTag} joined lobby {lobby.Code}.");
        _output.WriteLine($"Players: {string.Join(", ", lobby.Players)}");
        MaybeStartLobbyMatch(lobby);
    }

    private void StartMatch(MultiplayerMode mode)
    {
        var lobby = SelectLobby(mode, "Enter lobby code to start");
        if (lobby is null)
        {
            return;
        }

        var requiredPlayers = GetRequiredPlayers(mode);
        if (lobby.Players.Count < requiredPlayers)
        {
            _output.WriteLine($"Cannot start match. Need {requiredPlayers} players, currently {lobby.Players.Count}.");
            return;
        }

        _matchesStarted++;
        _output.WriteLine($"Starting {GetMultiplayerModeDisplayName(mode)} match in lobby {lobby.Code}.");
        _output.WriteLine($"Players: {string.Join(" vs ", lobby.Players.Take(requiredPlayers))}");
        _lobbies.Remove(lobby.Code);
        _output.WriteLine($"Lobby {lobby.Code} is now in-game.");
    }

    private void LeaveLobby(Player player, MultiplayerMode mode)
    {
        var lobby = SelectLobby(mode, "Enter lobby code to leave", player.GamerTag);
        if (lobby is null)
        {
            _output.WriteLine($"You are not currently in a {GetMultiplayerModeDisplayName(mode)} lobby.");
            return;
        }

        if (string.Equals(lobby.HostGamerTag, player.GamerTag, StringComparison.OrdinalIgnoreCase))
        {
            _lobbies.Remove(lobby.Code);
            _output.WriteLine($"Host left. Lobby {lobby.Code} was disbanded.");
            return;
        }

        lobby.Players.RemoveAll(tag => string.Equals(tag, player.GamerTag, StringComparison.OrdinalIgnoreCase));
        _output.WriteLine($"{player.GamerTag} left lobby {lobby.Code}.");
        _output.WriteLine($"Players: {string.Join(", ", lobby.Players)}");
    }

    private void DisbandLobby(Player player, MultiplayerMode mode)
    {
        var lobby = SelectLobby(mode, "Enter lobby code to disband", player.GamerTag, hostOnly: true);
        if (lobby is null)
        {
            _output.WriteLine($"No {GetMultiplayerModeDisplayName(mode)} lobby exists yet.");
            return;
        }

        _lobbies.Remove(lobby.Code);
        _output.WriteLine($"Lobby {lobby.Code} disbanded.");
    }

    private void ListLobbies(MultiplayerMode mode)
    {
        var lobbies = _lobbies.Values.Where(candidate => candidate.Mode == mode).OrderBy(candidate => candidate.Code).ToList();
        if (lobbies.Count == 0)
        {
            _output.WriteLine($"No {GetMultiplayerModeDisplayName(mode)} lobbies are open.");
            return;
        }

        _output.WriteLine($"Open lobbies for {GetMultiplayerModeDisplayName(mode)}:");
        var requiredPlayers = GetRequiredPlayers(mode);
        foreach (var lobby in lobbies)
        {
            _output.WriteLine($"- {lobby.Code} hosted by {lobby.HostGamerTag} | players: {lobby.Players.Count}/{requiredPlayers}");
        }
    }

    private Lobby? SelectLobby(MultiplayerMode mode, string prompt, string? playerTag = null, bool hostOnly = false)
    {
        var lobbies = _lobbies.Values
            .Where(candidate => candidate.Mode == mode)
            .Where(candidate => playerTag is null || candidate.Players.Contains(playerTag, StringComparer.OrdinalIgnoreCase))
            .Where(candidate => !hostOnly || (playerTag is not null && string.Equals(candidate.HostGamerTag, playerTag, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(candidate => candidate.Code)
            .ToList();

        if (lobbies.Count == 0)
        {
            _output.WriteLine($"No {GetMultiplayerModeDisplayName(mode)} lobby exists yet.");
            return null;
        }

        if (lobbies.Count == 1)
        {
            return lobbies[0];
        }

        _output.Write($"{prompt}: ");
        var code = (_input.ReadLine() ?? string.Empty).Trim().ToUpperInvariant();
        if (!IsValidCode(code))
        {
            _output.WriteLine("Invalid code format. Use 6 letters or numbers.");
            return null;
        }

        var lobby = lobbies.FirstOrDefault(candidate => string.Equals(candidate.Code, code, StringComparison.OrdinalIgnoreCase));
        if (lobby is null)
        {
            _output.WriteLine("Lobby not found.");
            return null;
        }

        return lobby;
    }

    private void JoinQueue(Player player, MultiplayerMode mode)
    {
        _output.Write("Enter join code: ");
        var code = (_input.ReadLine() ?? string.Empty).Trim().ToUpperInvariant();
        if (!IsValidCode(code))
        {
            _output.WriteLine("Invalid code format. Use 6 letters or numbers.");
            return;
        }

        if (_lobbies.TryGetValue(code, out var existingLobby))
        {
            if (existingLobby.Mode != mode)
            {
                _output.WriteLine($"Code {code} is assigned to {GetMultiplayerModeDisplayName(existingLobby.Mode)}.");
                return;
            }

            JoinLobby(player, existingLobby);
            _output.WriteLine($"{player.GamerTag} joined lobby {code} for {GetMultiplayerModeDisplayName(mode)}.");
            _output.WriteLine($"Players: {string.Join(", ", existingLobby.Players)}");
            MaybeStartLobbyMatch(existingLobby);
            return;
        }

        var lobby = CreateLobby(player, mode, code);
        _output.WriteLine($"{player.GamerTag} created and joined lobby {code} for {GetMultiplayerModeDisplayName(mode)}.");
        MaybeStartLobbyMatch(lobby);
    }

    private void JoinLobby(Player player, Lobby lobby)
    {
        if (lobby.Players.Contains(player.GamerTag, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        lobby.Players.Add(player.GamerTag);
    }

    private void MaybeStartLobbyMatch(Lobby lobby)
    {
        var requiredPlayers = GetRequiredPlayers(lobby.Mode);
        if (lobby.Players.Count < requiredPlayers)
        {
            _output.WriteLine($"Waiting for players: {lobby.Players.Count}/{requiredPlayers} ready.");
            return;
        }

        _output.WriteLine($"Match ready in {lobby.Code}: {string.Join(" vs ", lobby.Players.Take(requiredPlayers))}");
    }

    private static int GetRequiredPlayers(MultiplayerMode mode) =>
        mode switch
        {
            MultiplayerMode.LocalCoOp => 2,
            MultiplayerMode.TwoVsTwo => 4,
            MultiplayerMode.Tournament => 4,
            MultiplayerMode.WordsRoyale => 8,
            _ => 2
        };

    private static bool IsValidCode(string code) =>
        code.Length == 6 && code.All(char.IsLetterOrDigit);

    private void ShowStatistics(Player player)
    {
        _output.WriteLine("\n=== Statistics ===");
        _output.WriteLine($"GamerTag: {player.GamerTag}");
        _output.WriteLine($"Score: {player.Score}");
        _output.WriteLine($"Games won: {player.GamesWon}");
        _output.WriteLine($"Games played: {player.GamesPlayed}");
        var winRate = player.GamesPlayed == 0 ? 0 : (player.GamesWon * 100) / player.GamesPlayed;
        _output.WriteLine($"Win rate: {winRate}%");
        _output.WriteLine($"Single-player modes played: {_singlePlayerModePlays.Values.Sum()}");
        _output.WriteLine($"Single-player mode wins: {_singlePlayerModeWins.Values.Sum()}");
        _output.WriteLine($"Multiplayer modes visited: {_multiplayerModeVisits.Values.Sum()}");
        _output.WriteLine($"Open lobbies: {_lobbies.Count}");
        _output.WriteLine($"Lobby players: {_lobbies.Values.Sum(lobby => lobby.Players.Count)}");
        _output.WriteLine($"Matches started: {_matchesStarted}");
        _output.WriteLine($"Daily completions: {_dailyCompletions}");
        _output.WriteLine($"Daily streak: {_dailyStreak}");
        _output.WriteLine($"Best daily streak: {_bestDailyStreak}");
        _output.WriteLine($"Last survival streak: {_lastSurvivalStreak}");
        _output.WriteLine($"Best survival streak: {_bestSurvivalStreak}");
        _output.WriteLine($"Hints enabled: {(_showHints ? "Yes" : "No")}");
        _output.WriteLine($"Sound enabled: {(_soundEnabled ? "Yes" : "No")}");
        _output.WriteLine($"Theme: {_theme}");
        _output.WriteLine($"Difficulty preference: {_difficultyPreference}");
        var topMode = _singlePlayerModeWins
            .OrderByDescending(entry => entry.Value)
            .Select(entry => $"{GetModeDisplayName(entry.Key)} ({entry.Value})")
            .FirstOrDefault() ?? "N/A";
        _output.WriteLine($"Top mode: {topMode}");
    }

    private void ShowSettings()
    {
        while (true)
        {
            _output.WriteLine("\n=== Settings ===");
            _output.WriteLine($"Hints: {(_showHints ? "On" : "Off")}");
            _output.WriteLine($"Sound: {(_soundEnabled ? "On" : "Off")}");
            _output.WriteLine($"Difficulty preference: {_difficultyPreference}");
            _output.WriteLine($"Theme: {_theme}");

            var action = PromptChoice("Choose an option", ["Toggle hints", "Toggle sound", "Set difficulty preference", "Set theme"], allowBack: true);
            if (action is null)
            {
                return;
            }

            switch (action)
            {
                case "Toggle hints":
                    _showHints = !_showHints;
                    _output.WriteLine($"Hints set to {(_showHints ? "On" : "Off")}.");
                    break;
                case "Toggle sound":
                    _soundEnabled = !_soundEnabled;
                    _output.WriteLine($"Sound set to {(_soundEnabled ? "On" : "Off")}.");
                    break;
                case "Set difficulty preference":
                    var difficultyPreference = PromptEnum<DifficultyPreference>("Choose difficulty preference", allowBack: true);
                    if (difficultyPreference.HasValue)
                    {
                        _difficultyPreference = difficultyPreference.Value;
                        _output.WriteLine($"Difficulty preference set to {_difficultyPreference}.");
                    }
                    break;
                case "Set theme":
                    var theme = PromptEnum<Theme>("Choose theme", allowBack: true);
                    if (theme.HasValue)
                    {
                        _theme = theme.Value;
                        _output.WriteLine($"Theme set to {_theme}.");
                    }
                    break;
            }
        }
    }

    private void ShowAchievements(Player player)
    {
        _output.WriteLine("\n=== Achievements ===");
        var achievements = GetAchievements(player);
        foreach (var achievement in achievements)
        {
            var status = achievement.Unlocked ? "[Unlocked]" : "[Locked]";
            _output.WriteLine($"{status} {achievement.Name}");
        }
    }

    private void ShowCredits()
    {
        _output.WriteLine("\n=== Credits ===");
        _output.WriteLine("Words - Guess That Word");
        _output.WriteLine("Built by charwomander-svg.");
    }

    private GameConfig? SelectConfig()
    {
        var difficulty = PromptEnum<GameDifficulty>("Select difficulty", allowBack: true);
        if (!difficulty.HasValue)
        {
            _output.WriteLine("Round setup cancelled. Returning to main menu.");
            return null;
        }

        var category = PromptEnum<WordCategory>("Select category", allowBack: true);
        if (!category.HasValue)
        {
            _output.WriteLine("Round setup cancelled. Returning to main menu.");
            return null;
        }

        return new GameConfig { Difficulty = difficulty.Value, Category = category.Value };
    }

    private WordCategory? SelectCategory()
    {
        var category = PromptEnum<WordCategory>("Select category", allowBack: true);
        if (!category.HasValue)
        {
            _output.WriteLine("Round setup cancelled. Returning to main menu.");
            return null;
        }

        return category.Value;
    }

    private void PlayHardRound(Player player)
    {
        var category = SelectCategory();
        if (!category.HasValue)
        {
            return;
        }

        var config = new GameConfig
        {
            Difficulty = GameDifficulty.Hard,
            Category = category.Value,
            MaxIncorrectGuesses = 5,
            BasePoints = 140,
            BonusPerRemainingGuess = 15
        };

        _output.WriteLine("Hard mode: tougher words and tighter margins.");
        PlayModeRound(player, SinglePlayerMode.Hard, config);
    }

    private void PlayDailyRound(Player player)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (_lastDailyCompletionDate == today)
        {
            _output.WriteLine("Daily challenge already completed today. Come back tomorrow!");
            return;
        }

        var categories = Enum.GetValues<WordCategory>();
        var dayIndex = today.DayNumber;
        var category = categories[dayIndex % categories.Length];
        var difficulty = (dayIndex % 3) switch
        {
            0 => GameDifficulty.Easy,
            1 => GameDifficulty.Medium,
            _ => GameDifficulty.Hard
        };

        var config = new GameConfig
        {
            Difficulty = difficulty,
            Category = category,
            MaxIncorrectGuesses = 6,
            BasePoints = 120,
            BonusPerRemainingGuess = 12
        };

        _output.WriteLine($"Daily challenge: {difficulty} / {category}");
        var result = PlayModeRound(player, SinglePlayerMode.Daily, config);
        if (result == GameStatus.Won)
        {
            RecordDailyCompletion(today);
        }
    }

    private void PlaySpeedrunRound(Player player)
    {
        var config = SelectConfig();
        if (config is null)
        {
            return;
        }

        var speedrunConfig = new GameConfig
        {
            Difficulty = config.Difficulty,
            Category = config.Category,
            MaxIncorrectGuesses = 4,
            BasePoints = 110,
            BonusPerRemainingGuess = 20
        };

        _output.WriteLine("Speedrun: race to complete 10 words. Power-ups are disabled.");
        var solvedWords = 0;
        var roundsPlayed = 0;
        while (solvedWords < 10)
        {
            roundsPlayed++;
            _output.WriteLine($"Speedrun progress: {solvedWords}/10 words solved.");
            var result = PlayModeRound(player, SinglePlayerMode.Speedrun, speedrunConfig);
            if (result == GameStatus.Won)
            {
                solvedWords++;
            }
        }

        _output.WriteLine($"Speedrun complete! 10 words solved in {roundsPlayed} rounds.");
    }

    private void PlayAlphabetRound(Player player)
    {
        var baseConfig = SelectConfig();
        if (baseConfig is null)
        {
            return;
        }

        _output.WriteLine("A-Z challenge: clear one word per letter from A to Z.");
        var currentLetterCode = (int)'A';
        while (currentLetterCode <= 'Z')
        {
            var currentLetter = (char)currentLetterCode;
            _output.WriteLine($"A-Z challenge letter: {currentLetter}");
            var alphabetConfig = new GameConfig
            {
                Difficulty = baseConfig.Difficulty,
                Category = baseConfig.Category,
                MaxIncorrectGuesses = Math.Max(1, baseConfig.MaxIncorrectGuesses - 1),
                BasePoints = baseConfig.BasePoints,
                BonusPerRemainingGuess = baseConfig.BonusPerRemainingGuess
            };
            _output.WriteLine("Alphabet bonus: first letter revealed for free (and one fewer guess).");
            var result = PlayModeRound(
                player,
                SinglePlayerMode.Alphabet,
                alphabetConfig,
                firstFreeGuess: currentLetter,
                answerPredicate: answer => answer.Length > 0 && char.ToUpperInvariant(answer[0]) == currentLetter);
            if (result != GameStatus.Won)
            {
                _output.WriteLine($"A-Z challenge ended at letter {currentLetter}.");
                return;
            }

            currentLetterCode++;
        }

        _output.WriteLine("A-Z challenge complete! You cleared all 26 letters.");
    }

    private void PlayGhostRound(Player player)
    {
        var config = SelectConfig();
        if (config is null)
        {
            return;
        }

        var ghostConfig = new GameConfig
        {
            Difficulty = config.Difficulty,
            Category = config.Category,
            MaxIncorrectGuesses = 3,
            BasePoints = 150,
            BonusPerRemainingGuess = 20
        };

        _output.WriteLine("Ghost mode: shows how many letters are correct, but not which letters.");
        PlayModeRound(player, SinglePlayerMode.Ghost, ghostConfig, displayMode: RoundDisplayMode.CorrectCountOnly);
    }

    private void PlayWordHuntRun(Player player)
    {
        _output.WriteLine("Word Hunt: complete three rounds in a row.");
        for (var round = 1; round <= 3; round++)
        {
            _output.WriteLine($"Word Hunt round {round}/3");
            var config = SelectConfig();
            if (config is null)
            {
                return;
            }

            PlayModeRound(player, SinglePlayerMode.WordHunt, config);
        }
    }

    private void PlayWildDictionaryRound(Player player)
    {
        var categories = Enum.GetValues<WordCategory>();
        var difficulties = Enum.GetValues<GameDifficulty>();
        var category = categories[Random.Shared.Next(categories.Length)];
        var difficulty = difficulties[Random.Shared.Next(difficulties.Length)];

        var config = new GameConfig
        {
            Difficulty = difficulty,
            Category = category,
            MaxIncorrectGuesses = 6,
            BasePoints = 125,
            BonusPerRemainingGuess = 12
        };

        _output.WriteLine($"Wild Dictionary: random challenge {difficulty} / {category}.");
        PlayModeRound(player, SinglePlayerMode.WildDictionary, config);
    }

    private void PlayTimeAttackRound(Player player)
    {
        var baseConfig = SelectConfig();
        if (baseConfig is null)
        {
            return;
        }

        var config = new GameConfig
        {
            Difficulty = baseConfig.Difficulty,
            Category = baseConfig.Category,
            MaxIncorrectGuesses = 4,
            BasePoints = 130,
            BonusPerRemainingGuess = 15
        };

        _output.WriteLine("Time Attack: fewer mistakes allowed for higher rewards.");
        PlayModeRound(player, SinglePlayerMode.TimeAttack, config);
    }

    private void PlaySurvivalRun(Player player)
    {
        _output.WriteLine("Survival: keep winning as guesses get tighter each round.");
        int streak = 0;
        while (true)
        {
            var baseConfig = SelectConfig();
            if (baseConfig is null)
            {
                return;
            }

            var config = new GameConfig
            {
                Difficulty = baseConfig.Difficulty,
                Category = baseConfig.Category,
                MaxIncorrectGuesses = Math.Max(3, 6 - streak),
                BasePoints = 100 + streak * 20,
                BonusPerRemainingGuess = 10
            };

            var result = PlayModeRound(player, SinglePlayerMode.Survival, config);
            if (result != GameStatus.Won)
            {
                _lastSurvivalStreak = streak;
                _bestSurvivalStreak = Math.Max(_bestSurvivalStreak, _lastSurvivalStreak);
                _output.WriteLine($"Survival ended with streak: {streak}");
                return;
            }

            streak++;
            _output.WriteLine($"Survival streak: {streak}");
            var next = PromptChoice("Continue survival?", ["Continue", "Exit"], allowBack: false);
            if (!string.Equals(next, "Continue", StringComparison.OrdinalIgnoreCase))
            {
                _lastSurvivalStreak = streak;
                _bestSurvivalStreak = Math.Max(_bestSurvivalStreak, _lastSurvivalStreak);
                return;
            }
        }
    }

    private void PlayVsCpuRound(Player player)
    {
        var config = SelectConfig();
        if (config is null)
        {
            return;
        }

        var result = PlayModeRound(player, SinglePlayerMode.VsCpu, config);
        if (result == GameStatus.Won)
        {
            _output.WriteLine("You beat the CPU!");
            return;
        }

        _output.WriteLine("CPU wins this round.");
    }

    private GameStatus PlayModeRound(Player player, SinglePlayerMode mode, GameConfig config, Func<char, IReadOnlySet<char>, string?>? guessGuard = null, RoundDisplayMode displayMode = RoundDisplayMode.Standard, char? firstFreeGuess = null, Func<string, bool>? answerPredicate = null)
    {
        var result = PlayRound(player, config, guessGuard, displayMode, firstFreeGuess, answerPredicate);
        if (result == GameStatus.Won)
        {
            _singlePlayerModeWins[mode] = _singlePlayerModeWins.GetValueOrDefault(mode) + 1;
        }

        return result;
    }

    private void RecordDailyCompletion(DateOnly completionDate)
    {
        _dailyCompletions++;
        if (_lastDailyCompletionDate is DateOnly lastDate && lastDate.AddDays(1) == completionDate)
        {
            _dailyStreak++;
        }
        else
        {
            _dailyStreak = 1;
        }

        _bestDailyStreak = Math.Max(_bestDailyStreak, _dailyStreak);
        _lastDailyCompletionDate = completionDate;
        _output.WriteLine($"Daily streak: {_dailyStreak} (best {_bestDailyStreak})");
    }

    private GameStatus PlayRound(Player player, GameConfig config, Func<char, IReadOnlySet<char>, string?>? guessGuard = null, RoundDisplayMode displayMode = RoundDisplayMode.Standard, char? firstFreeGuess = null, Func<string, bool>? answerPredicate = null)
    {
        config = ApplyDifficultyPreference(config);

        GameSession session;
        try
        {
            session = answerPredicate is null
                ? _gameService.StartGame(player, config)
                : _gameService.StartGame(player, config, answerPredicate);
        }
        catch (InvalidOperationException ex)
        {
            _output.WriteLine($"[Error] {ex.Message}");
            return GameStatus.Lost;
        }

        if (firstFreeGuess.HasValue && char.IsLetter(firstFreeGuess.Value))
        {
            _gameService.SubmitGuess(session.Id, firstFreeGuess.Value);
        }

        if (_showHints)
        {
            _output.WriteLine($"\nHint: {session.Hint}");
        }
        else
        {
            _output.WriteLine("\nHint: [disabled in settings]");
        }
        if (displayMode == RoundDisplayMode.CorrectCountOnly)
        {
            _output.WriteLine($"Correct letters: 0/{session.Answer.Length}  |  Guesses left: {session.RemainingGuesses}");
        }
        else
        {
            _output.WriteLine($"Word: {session.MaskedWord}  |  Guesses left: {session.RemainingGuesses}");
            _output.WriteLine("Guessed: (none)");
        }

        while (session.Status == GameStatus.InProgress)
        {
            _output.Write("\nGuess a letter: ");
            var input = _input.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input) || input.Length != 1 || !char.IsLetter(input[0]))
            {
                _output.WriteLine("Please enter a single letter.");
                continue;
            }

            if (guessGuard is not null)
            {
                var validationError = guessGuard(input[0], session.GuessedLetters);
                if (!string.IsNullOrWhiteSpace(validationError))
                {
                    _output.WriteLine(validationError);
                    continue;
                }
            }

            var result = _gameService.SubmitGuess(session.Id, input[0]);

            switch (result.Outcome)
            {
                case GuessOutcome.AlreadyGuessed:
                    _output.WriteLine($"  You already guessed '{char.ToUpperInvariant(result.Letter)}'.");
                    break;
                case GuessOutcome.Correct:
                    _output.WriteLine($"  ✓ '{char.ToUpperInvariant(result.Letter)}' is in the word!");
                    break;
                case GuessOutcome.Incorrect:
                    _output.WriteLine($"  ✗ '{char.ToUpperInvariant(result.Letter)}' is not in the word.");
                    break;
            }

            if (displayMode == RoundDisplayMode.CorrectCountOnly)
            {
                var correctCount = session.MaskedWord.Count(character => character != '_');
                _output.WriteLine($"  Correct letters: {correctCount}/{session.Answer.Length}  |  Guesses left: {session.RemainingGuesses}");
            }
            else
            {
                _output.WriteLine($"  Word: {session.MaskedWord}  |  Guesses left: {session.RemainingGuesses}");
                _output.WriteLine($"  Guessed: {string.Join(" ", session.GuessedLetters.Order())}");
            }
        }

        if (session.Status == GameStatus.Won)
        {
            int score = session.CalculateScore();
            _output.WriteLine($"\n🎉 You guessed '{session.Answer}'! +{score} points");
        }
        else
        {
            _output.WriteLine($"\n💀 Out of guesses! The word was '{session.Answer}'.");
        }

        _output.WriteLine($"  Total score: {player.Score}  |  Games won: {player.GamesWon}/{player.GamesPlayed}");

        // EndGame is called automatically by SubmitGuess once the session is
        // no longer InProgress, so no explicit call is needed here.
        return session.Status;
    }

    private void ShowLeaderboard()
    {
        var leaderboard = _scoreService.GetLeaderboard();
        _output.WriteLine("\n=== Leaderboard ===");

        if (leaderboard.Count == 0)
        {
            _output.WriteLine("No scores yet.");
            return;
        }

        for (int index = 0; index < Math.Min(leaderboard.Count, 5); index++)
        {
            var player = leaderboard[index];
            _output.WriteLine($"{index + 1}. {player.GamerTag,-16} {player.Score,4}");
        }
    }

    private T? PromptEnum<T>(string prompt, bool allowBack = false) where T : struct, Enum
    {
        var options = Enum.GetNames<T>();

        while (true)
        {
            _output.WriteLine(prompt + ":");
            for (int index = 0; index < options.Length; index++)
            {
                _output.WriteLine($"  {index + 1}) {options[index]}");
            }

            if (allowBack)
            {
                _output.WriteLine("  B) Back");
            }

            _output.Write("Choose an option: ");
            var input = _input.ReadLine()?.Trim() ?? string.Empty;

            if (allowBack && input.Equals("B", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (int.TryParse(input, out var optionNumber))
            {
                if (optionNumber >= 1 && optionNumber <= options.Length &&
                    Enum.TryParse<T>(options[optionNumber - 1], ignoreCase: true, out var optionValue))
                {
                    return optionValue;
                }
            }

            if (Enum.TryParse<T>(input, ignoreCase: true, out var value) && Enum.IsDefined(value))
                return value;

            var normalizedInput = NormalizeOptionKey(input);
            foreach (var option in options)
            {
                if (NormalizeOptionKey(option) == normalizedInput &&
                    Enum.TryParse<T>(option, ignoreCase: true, out var normalizedValue))
                {
                    return normalizedValue;
                }
            }

            _output.WriteLine($"  Invalid choice. Enter 1-{options.Length}, a name ({string.Join(", ", options)}){(allowBack ? ", or B for back." : ".")}");
        }
    }

    private string? PromptChoice(string prompt, IReadOnlyList<string> options, bool allowBack = false)
    {
        while (true)
        {
            _output.WriteLine(prompt + ":");
            for (int index = 0; index < options.Count; index++)
            {
                _output.WriteLine($"  {index + 1}) {options[index]}");
            }

            if (allowBack)
            {
                _output.WriteLine("  B) Back");
            }

            _output.Write("Choose an option: ");
            var input = _input.ReadLine()?.Trim() ?? string.Empty;

            if (allowBack && input.Equals("B", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (int.TryParse(input, out var optionNumber) &&
                optionNumber >= 1 &&
                optionNumber <= options.Count)
            {
                return options[optionNumber - 1];
            }

            var normalizedInput = NormalizeOptionKey(input);
            foreach (var option in options)
            {
                if (NormalizeOptionKey(option) == normalizedInput)
                {
                    return option;
                }
            }

            _output.WriteLine($"  Invalid choice. Enter 1-{options.Count}, an option name{(allowBack ? ", or B for back." : ".")}");
        }
    }

    private string GetModeDisplayName(SinglePlayerMode mode) =>
        mode switch
        {
            SinglePlayerMode.VsCpu => "Vs. CPU",
            SinglePlayerMode.WordHunt => "Word Hunt",
            SinglePlayerMode.WildDictionary => "Wild Dictionary",
            SinglePlayerMode.TimeAttack => "Time Attack",
            _ => mode.ToString()
        };

    private string GetMultiplayerModeDisplayName(MultiplayerMode mode) =>
        mode switch
        {
            MultiplayerMode.LocalCoOp => "Local Co-op",
            MultiplayerMode.LocalVs => "Local Vs",
            MultiplayerMode.HeadToHead => "Head to Head",
            MultiplayerMode.TwoVsTwo => "2v2",
            MultiplayerMode.WordsRoyale => "Words Royale",
            _ => mode.ToString()
        };

    private static string NormalizeOptionKey(string input) =>
        string.Concat(input.Where(char.IsLetterOrDigit)).ToUpperInvariant();

    private GameConfig ApplyDifficultyPreference(GameConfig config)
    {
        return _difficultyPreference switch
        {
            DifficultyPreference.Casual => new GameConfig
            {
                Difficulty = config.Difficulty,
                Category = config.Category,
                MaxIncorrectGuesses = config.MaxIncorrectGuesses + 1,
                BasePoints = config.BasePoints,
                BonusPerRemainingGuess = config.BonusPerRemainingGuess
            },
            DifficultyPreference.Competitive => new GameConfig
            {
                Difficulty = config.Difficulty,
                Category = config.Category,
                MaxIncorrectGuesses = Math.Max(2, config.MaxIncorrectGuesses - 1),
                BasePoints = config.BasePoints + 20,
                BonusPerRemainingGuess = config.BonusPerRemainingGuess + 5
            },
            _ => config
        };
    }

    private IReadOnlyList<AchievementStatus> GetAchievements(Player player) =>
    [
        new AchievementStatus("First Win", player.GamesWon >= 1),
        new AchievementStatus("Solo Explorer", _singlePlayerModePlays.Keys.Count >= 3),
        new AchievementStatus("Matchmaker", _lobbies.Count >= 1),
        new AchievementStatus("Hunter", _singlePlayerModePlays.TryGetValue(SinglePlayerMode.WordHunt, out var wordHuntPlays) && wordHuntPlays >= 1),
        new AchievementStatus("Purist", !_showHints),
        new AchievementStatus("Party Starter", _lobbies.Values.Any(lobby => lobby.Players.Count >= GetRequiredPlayers(lobby.Mode))),
        new AchievementStatus("Competitor", _matchesStarted >= 1),
        new AchievementStatus("Word Veteran", _singlePlayerModeWins.Values.Sum() >= 5),
        new AchievementStatus("Daily Devotee", _dailyStreak >= 3),
        new AchievementStatus("Iron Will", _bestSurvivalStreak >= 2)
    ];

    private enum MainMenuChoice
    {
        SinglePlayer = 1,
        Multiplayer = 2,
        Leaderboards = 3,
        Statistics = 4,
        Settings = 5,
        Achievements = 6,
        Credits = 7
    }

    private enum SinglePlayerMode
    {
        Classic = 1,
        Hard = 2,
        Daily = 3,
        Speedrun = 4,
        Alphabet = 5,
        TimeAttack = 6,
        Ghost = 7,
        VsCpu = 8,
        Survival = 9,
        WordHunt = 10,
        WildDictionary = 11
    }

    private enum MultiplayerMode
    {
        LocalCoOp = 1,
        LocalVs = 2,
        HeadToHead = 3,
        Showdown = 4,
        TwoVsTwo = 5,
        Tournament = 6,
        WordsRoyale = 7
    }

    private enum DifficultyPreference
    {
        Adaptive = 1,
        Casual = 2,
        Competitive = 3
    }

    private enum Theme
    {
        System = 1,
        Light = 2,
        Dark = 3
    }

    private enum RoundDisplayMode
    {
        Standard = 1,
        CorrectCountOnly = 2
    }

    private sealed record AchievementStatus(string Name, bool Unlocked);
    private sealed record Lobby(string Code, MultiplayerMode Mode, string HostGamerTag, List<string> Players);
}
