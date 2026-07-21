using Words.Core.Models;
using Words.Core.Services;
using System.Drawing.Drawing2D;
using System.Media;

namespace Words.Windows;

public sealed class WordleForm : Form
{
    private const int ContentWidth = 860;
    
    // UI Design System - "Adaptive Arena" color palette
    private static readonly Color AppBackColor = Color.FromArgb(10, 14, 26);        // Deep Navy #0A0E1A
    private static readonly Color CardBackColor = Color.FromArgb(18, 24, 38);       // Dark Panel #121826
    private static readonly Color CardBorderColor = Color.FromArgb(38, 48, 71);     // Muted border
    private static readonly Color TextPrimaryColor = Color.FromArgb(248, 250, 252); // Off-white #F8FAFC
    private static readonly Color TextMutedColor = Color.FromArgb(148, 163, 184);   // Steel Gray #94A3B8
    private static readonly Color AccentColor = Color.FromArgb(56, 189, 248);       // Cyan #38BDF8
    private static readonly Color TileBaseColor = Color.FromArgb(30, 41, 59);       // Tile base #1E293B
    private static readonly Color TileTextColor = Color.FromArgb(248, 250, 252);    // Tile text #F8FAFC
    private static readonly Color RoyaleAccent = Color.FromArgb(220, 38, 38);       // Crimson Red #DC2626
    private static readonly Color MegaAccent = Color.FromArgb(37, 99, 235);         // Royal Blue #2563EB
    
    // 4K optimized layout constants
    private const int HeaderHeight = 80;
    private const int ActionBarHeight = 120;

    private static readonly string StatsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Words",
        "demo-stats.json");

    private readonly WordleService _wordleService;
    private readonly DemoStatsStore _stats;
    private WordleSession _session = null!;
    private WordleGameMode _activeMode = WordleGameMode.Classic;
    private bool _roundRecorded;
    private int _secondsRemaining;
    private int _survivalStreak;
    private TimeAttackRun? _timeAttackRun;
    private CpuRaceRun? _cpuRaceRun;
    private RoyaleRun? _royaleRun;
    private MegaBoardRun? _megaBoardRun;
    private int _royaleBotPulse;
    private int _alphabetIndex;
    private int _armedClockBonusSeconds;
    private bool _shieldArmed;
    private bool _crownArmed;
    private int _freezeTicksRemaining;
    private int _royaleBotFreezeTicksRemaining;
    private int _lastLowTimeCueSecond = int.MaxValue;
    private readonly HashSet<int> _revealedPositions = [];

    private readonly TableLayoutPanel _board = new()
    {
        AutoSize = true,
        CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
        Margin = new Padding(0, 14, 0, 14)
    };
    private readonly TextBox _guessInput = new() { CharacterCasing = CharacterCasing.Upper, Width = 220 };
    private readonly ComboBox _modeSelector = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 190 };
    private readonly ComboBox _wordLengthSelector = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80 };
    private readonly ComboBox _cpuDifficultySelector = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
    private readonly ComboBox _megaBoardCountSelector = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80 };
    private readonly ComboBox _megaBoardFilterSelector = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
    private readonly ComboBox _megaBoardSortSelector = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
    private readonly Button _focusCriticalMegaBoard = new() { Text = "Focus Critical", AutoSize = true };
    private readonly Button _pitchSetup = new() { Text = "Pitch Setup", AutoSize = true };
    private readonly Button _submit = new() { Text = "Guess", AutoSize = true };
    private readonly Button _newGame = new() { Text = "New Game", AutoSize = true };
    private readonly Button _giveUp = new() { Text = "Give Up", AutoSize = true };
    private readonly Button _resetStats = new() { Text = "Reset Stats", AutoSize = true };
    private readonly Button _copyResult = new() { Text = "Copy Result", AutoSize = true, Enabled = false };
    private readonly Button _copyStats = new() { Text = "Copy Stats", AutoSize = true };
    private readonly Button _openStatsFolder = new() { Text = "Stats Folder", AutoSize = true };
    private readonly ComboBox _powerUpSelector = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
    private readonly ComboBox _vowelSelector = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 52 };
    private readonly Button _usePowerUp = new() { Text = "Use Power-Up", AutoSize = true };
    private readonly Label _commonality = new() { AutoSize = true, Margin = new Padding(0, 10, 0, 4), MaximumSize = new Size(ContentWidth, 0) };
    private readonly Label _modeHelp = new() { AutoSize = true, Margin = new Padding(0, 0, 0, 8), MaximumSize = new Size(ContentWidth, 0), ForeColor = Color.DimGray };
    private readonly Label _cpuRaceSummary = new() { AutoSize = true, Margin = new Padding(0, 0, 0, 8), MaximumSize = new Size(ContentWidth, 0), ForeColor = Color.DimGray, Visible = false };
    private readonly Label _status = new() { AutoSize = true, Margin = new Padding(0, 8, 0, 0), MaximumSize = new Size(ContentWidth, 0) };
    private readonly Label _roundSummary = new() { AutoSize = true, Margin = new Padding(0, 4, 0, 4), MaximumSize = new Size(ContentWidth, 0) };
    private readonly Label _wordInfo = new() { AutoSize = true, Margin = new Padding(0, 4, 0, 4), MaximumSize = new Size(ContentWidth, 0), ForeColor = Color.DimGray };
    private readonly Label _statsSummary = new() { AutoSize = true, Margin = new Padding(0, 10, 0, 4), MaximumSize = new Size(ContentWidth, 0) };
    private readonly Label _badges = new() { AutoSize = true, Margin = new Padding(0, 4, 0, 4), MaximumSize = new Size(ContentWidth, 0) };
    private readonly FlowLayoutPanel _megaBoardDashboard = new()
    {
        AutoSize = true,
        MaximumSize = new Size(ContentWidth, 0),
        Margin = new Padding(0, 0, 0, 8),
        Visible = false
    };
    private readonly ListBox _recentWords = new() { Width = ContentWidth, Height = 140 };
    
    // Game Card (Phase 1/2)
    private Panel? _gameCard;
    
    // Royale Mode UI Panels (Phase 2 - Redesigned)
    private readonly Panel _royaleLeftPanel = new()
    {
        Width = 280,
        BackColor = CardBackColor,
        Visible = false,
        AutoScroll = true,
        Padding = new Padding(16)
    };
    private readonly Panel _royaleRightPanel = new()
    {
        Width = 320,
        BackColor = CardBackColor,
        Visible = false,
        Padding = new Padding(20)
    };
    private readonly Queue<string> _royaleEvents = new();
    private const int MaxRoyaleEvents = 12;
    
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
    private Label[,] _tiles = new Label[0, 0];
    private readonly Dictionary<char, Button> _keyboardButtons = [];
    private readonly WordleKeyboardState _keyboardState = new();
    private readonly Dictionary<MusicEnergy, string[]> _tracksByEnergy;
    private readonly Dictionary<string, SoundPlayer[]> _sfxPlayers;
    private readonly Random _sfxRandom = new();
    private SoundPlayer? _musicPlayer;
    private string? _currentMusicPath;

    public WordleForm(WordleService wordleService)
    {
        _wordleService = wordleService ?? throw new ArgumentNullException(nameof(wordleService));
        _stats = DemoStatsStore.Load(StatsPath);

        Text = "Words";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1920, 1080);  // Minimum 1080p
        ClientSize = new Size(3840, 2160);    // Target 4K
        BackColor = AppBackColor;
        ForeColor = TextPrimaryColor;
        WindowState = FormWindowState.Maximized;
        KeyPreview = true;
        AutoScaleMode = AutoScaleMode.Dpi;    // 4K DPI scaling
        _tracksByEnergy = BuildMusicLibrary();
        _sfxPlayers = BuildSfxPlayers();

        BuildLayout();
        RestoreDemoSettings();
        _session = StartConfiguredSession();
        BindEvents();
        ResetBoard();
    }

    private void BuildLayout()
    {
        // Master Grid: Header / Stage / Action Bar (4K optimized)
        var masterGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = AppBackColor,
            Padding = new Padding(0)
        };
        masterGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        masterGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, HeaderHeight));      // Header: 80px fixed
        masterGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));              // Stage: flex
        masterGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, ActionBarHeight));  // Action Bar: 120px fixed
        Controls.Add(masterGrid);

        // HEADER BAR (Row 0)
        var header = BuildHeader();
        masterGrid.Controls.Add(header, 0, 0);

        // MAIN STAGE (Row 1) - Scrollable content area
        var stage = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(48, 24, 48, 24),  // 4K-optimized padding
            BackColor = AppBackColor
        };
        masterGrid.Controls.Add(stage, 0, 1);

        // Build stage content
        stage.Controls.Add(BuildFeatureChipsRow());

        var modeRow = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 0, 0, 8) };
        modeRow.Controls.Add(new Label { Text = "Mode", AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
        _modeSelector.Items.Add(new ModeOption("Classic", WordleGameMode.Classic));
        _modeSelector.Items.Add(new ModeOption("Hard", WordleGameMode.Hard));
        _modeSelector.Items.Add(new ModeOption("Daily", WordleGameMode.Daily));
        _modeSelector.Items.Add(new ModeOption("Alphabet", WordleGameMode.Alphabet));
        _modeSelector.Items.Add(new ModeOption("Timed", WordleGameMode.Timed));
        _modeSelector.Items.Add(new ModeOption("Ghost", WordleGameMode.Ghost));
        _modeSelector.Items.Add(new ModeOption("Vs. CPU", WordleGameMode.VsCpu));
        _modeSelector.Items.Add(new ModeOption("Words Royale", WordleGameMode.WordsRoyale));
        _modeSelector.Items.Add(new ModeOption("Mega Board", WordleGameMode.MegaBoard));
        _modeSelector.Items.Add(new ModeOption("Survival", WordleGameMode.Survival));
        _modeSelector.Items.Add(new ModeOption("Rare Hunt", WordleGameMode.RareHunt));
        _modeSelector.Items.Add(new ModeOption("Discovery", WordleGameMode.Discovery));
        _modeSelector.Items.Add(new ModeOption("Wild Dictionary", WordleGameMode.WildDictionary));
        modeRow.Controls.Add(_modeSelector);
        modeRow.Controls.Add(new Label { Text = "Length", AutoSize = true, Padding = new Padding(14, 7, 0, 0) });
        for (var length = 4; length <= 20; length++)
            _wordLengthSelector.Items.Add(length);
        modeRow.Controls.Add(_wordLengthSelector);
        modeRow.Controls.Add(new Label { Text = "CPU", AutoSize = true, Padding = new Padding(14, 7, 0, 0) });
        _cpuDifficultySelector.Items.Add(new CpuDifficultyOption("Casual", TargetWords: 5, CpuSecondsPerWord: 22, CpuPersona: "Steady"));
        _cpuDifficultySelector.Items.Add(new CpuDifficultyOption("Standard", TargetWords: 5, CpuSecondsPerWord: 18, CpuPersona: "Sprinter"));
        _cpuDifficultySelector.Items.Add(new CpuDifficultyOption("Elite", TargetWords: 7, CpuSecondsPerWord: 14, CpuPersona: "Closer"));
        _cpuDifficultySelector.Items.Add(new CpuDifficultyOption("Chaos", TargetWords: 10, CpuSecondsPerWord: 16, CpuPersona: "Chaos"));
        _cpuDifficultySelector.SelectedItem = _cpuDifficultySelector.Items[1];
        modeRow.Controls.Add(_cpuDifficultySelector);
        modeRow.Controls.Add(new Label { Text = "Boards", AutoSize = true, Padding = new Padding(14, 7, 0, 0) });
        foreach (var count in new[] { 4, 9, 16, 25, 50, 100 })
            _megaBoardCountSelector.Items.Add(count);
        _megaBoardCountSelector.SelectedItem = 100;
        modeRow.Controls.Add(_megaBoardCountSelector);
        modeRow.Controls.Add(new Label { Text = "View", AutoSize = true, Padding = new Padding(14, 7, 0, 0) });
        foreach (var filter in new[] { "All", "Active", "Danger", "Solved", "Lost" })
            _megaBoardFilterSelector.Items.Add(filter);
        _megaBoardFilterSelector.SelectedItem = "All";
        modeRow.Controls.Add(_megaBoardFilterSelector);
        modeRow.Controls.Add(new Label { Text = "Sort", AutoSize = true, Padding = new Padding(14, 7, 0, 0) });
        foreach (var sort in new[] { "Number", "Smart", "Danger", "Attempts", "Status" })
            _megaBoardSortSelector.Items.Add(sort);
        _megaBoardSortSelector.SelectedItem = "Smart";
        modeRow.Controls.Add(_megaBoardSortSelector);
        modeRow.Controls.Add(_focusCriticalMegaBoard);
        modeRow.Controls.Add(_pitchSetup);

        var powerRow = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 0, 0, 6), WrapContents = true, MaximumSize = new Size(ContentWidth, 0) };
        powerRow.Controls.Add(new Label { Text = "Power-Ups", AutoSize = true, Padding = new Padding(0, 8, 8, 0) });
        powerRow.Controls.Add(_powerUpSelector);
        foreach (var vowel in new[] { 'A', 'E', 'I', 'O', 'U' })
            _vowelSelector.Items.Add(vowel);
        _vowelSelector.SelectedItem = 'A';
        powerRow.Controls.Add(_vowelSelector);
        powerRow.Controls.Add(_usePowerUp);

        var setupCard = BuildCard();
        setupCard.MaximumSize = new Size(ContentWidth, 0);
        setupCard.Controls.Add(_commonality);
        setupCard.Controls.Add(modeRow);
        setupCard.Controls.Add(powerRow);
        setupCard.Controls.Add(_modeHelp);
        setupCard.Controls.Add(_cpuRaceSummary);
        stage.Controls.Add(setupCard);

        // Game Area - Board card + conditional Royale panels
        var gameAreaContainer = BuildGameAreaControls();
        stage.Controls.Add(gameAreaContainer);

        var recentWordsLabel = new Label
        {
            Text = "Recent words",
            AutoSize = true,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Margin = new Padding(0, 8, 0, 2)
        };
        _recentWords.Width = ContentWidth - 28;
        var progressCard = BuildCard();
        progressCard.MaximumSize = new Size(ContentWidth, 0);
        progressCard.Controls.Add(_statsSummary);
        progressCard.Controls.Add(_badges);
        progressCard.Controls.Add(recentWordsLabel);
        progressCard.Controls.Add(_recentWords);
        stage.Controls.Add(progressCard);

        ApplyControlTheme(setupCard);
        ApplyControlTheme(_gameCard!);
        ApplyControlTheme(progressCard);

        // ACTION BAR (Row 2)
        var actionBar = BuildActionBar();
        masterGrid.Controls.Add(actionBar, 0, 2);
    }

    private static FlowLayoutPanel BuildCard() =>
        new()
        {
            AutoSize = true,
            MaximumSize = new Size(ContentWidth, 0),
            WrapContents = false,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(14, 12, 14, 12),
            Margin = new Padding(0, 0, 0, 12),
            BackColor = CardBackColor
        };

    private Panel BuildHeader()
    {
        // HEADER BAR - Fixed 80px, dark translucent with neon accent
        var header = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(218, 0, 0, 0),  // rgba(0,0,0,0.85)
            Height = HeaderHeight
        };

        var headerContent = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(32, 0, 32, 0)
        };

        // Logo/Title (Left)
        var logo = new Label
        {
            Text = "WORDS",
            AutoSize = true,
            Font = new Font("Segoe UI", 28F, FontStyle.Bold),
            ForeColor = AccentColor,
            Padding = new Padding(0, 18, 40, 0)
        };
        headerContent.Controls.Add(logo);

        // Mode display (Center-ish)
        var modeLabel = new Label
        {
            Text = "Mode: Classic | Rank: --",
            AutoSize = true,
            Font = new Font("Segoe UI", 14F, FontStyle.Regular),
            ForeColor = TextPrimaryColor,
            Padding = new Padding(0, 28, 40, 0)
        };
        headerContent.Controls.Add(modeLabel);

        // Timer (Right side - will be updated dynamically)
        var timerLabel = new Label
        {
            Text = "",
            AutoSize = true,
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            ForeColor = AccentColor,
            Padding = new Padding(0, 24, 0, 0)
        };
        headerContent.Controls.Add(timerLabel);

        header.Controls.Add(headerContent);

        // Bottom accent line
        header.Paint += (_, e) =>
        {
            using var pen = new Pen(AccentColor, 2);
            e.Graphics.DrawLine(pen, 0, HeaderHeight - 2, header.Width, HeaderHeight - 2);
        };

        return header;
    }

    private Panel BuildActionBar()
    {
        // ACTION BAR - Fixed 120px at bottom
        var actionBar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(218, 0, 0, 0),  // rgba(0,0,0,0.85)
            Height = ActionBarHeight
        };

        var actionContent = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(48, 20, 48, 20)
        };

        // Primary action row
        actionContent.Controls.Add(new Label { Text = "Guess", AutoSize = true, Padding = new Padding(0, 10, 8, 0), Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = TextPrimaryColor });
        _guessInput.Font = new Font("Segoe UI", 14F);
        _guessInput.Width = 280;
        actionContent.Controls.Add(_guessInput);
        _submit.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        actionContent.Controls.Add(_submit);
        _newGame.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        actionContent.Controls.Add(_newGame);
        _giveUp.Font = new Font("Segoe UI", 11F);
        actionContent.Controls.Add(_giveUp);
        _copyResult.Font = new Font("Segoe UI", 11F);
        actionContent.Controls.Add(_copyResult);
        _copyStats.Font = new Font("Segoe UI", 11F);
        actionContent.Controls.Add(_copyStats);
        _openStatsFolder.Font = new Font("Segoe UI", 11F);
        actionContent.Controls.Add(_openStatsFolder);
        _resetStats.Font = new Font("Segoe UI", 11F);
        actionContent.Controls.Add(_resetStats);

        actionBar.Controls.Add(actionContent);

        // Top accent line
        actionBar.Paint += (_, e) =>
        {
            using var pen = new Pen(CardBorderColor, 1);
            e.Graphics.DrawLine(pen, 0, 0, actionBar.Width, 0);
        };

        return actionBar;
    }

    private Control BuildGameAreaControls()
    {
        // Build Royale panels
        BuildRoyaleRankPanel();
        BuildRoyaleLiveFeed();
        
        // Build main game card
        _gameCard = BuildCard();
        _gameCard.MaximumSize = new Size(ContentWidth, 0);
        _gameCard.Controls.Add(_megaBoardDashboard);
        _gameCard.Controls.Add(BuildBoardFrame(ContentWidth));
        _gameCard.Controls.Add(BuildKeyboard());
        _gameCard.Controls.Add(_status);
        _gameCard.Controls.Add(_roundSummary);
        _gameCard.Controls.Add(_wordInfo);
        
        // Container with horizontal layout for Royale mode
        var container = new FlowLayoutPanel
        {
            AutoSize = true,
            MaximumSize = new Size(3600, 0),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0)
        };
        
        container.Controls.Add(_royaleRankPanel);
        container.Controls.Add(_gameCard);
        container.Controls.Add(_royaleLiveFeed);
        
        return container;
    }

    private void BuildRoyaleRankPanel()
    {
        _royaleRankPanel.Controls.Clear();
        _royaleRankPanel.Padding = new Padding(16);
        _royaleRankPanel.BorderStyle = BorderStyle.FixedSingle;

        var header = new Label
        {
            Text = "LEADERBOARD",
            AutoSize = true,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = RoyaleAccent,
            Padding = new Padding(0, 0, 0, 12)
        };
        _royaleRankPanel.Controls.Add(header);
        header.Dock = DockStyle.Top;

        var rankList = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = CardBackColor
        };
        _royaleRankPanel.Controls.Add(rankList);
    }

    private void BuildRoyaleLiveFeed()
    {
        _royaleLiveFeed.BorderStyle = BorderStyle.FixedSingle;
        _royaleLiveFeed.DrawMode = DrawMode.OwnerDrawFixed;
        _royaleLiveFeed.ItemHeight = 45;
        _royaleLiveFeed.SelectionMode = SelectionMode.None;
        
        _royaleLiveFeed.DrawItem += (_, e) =>
        {
            if (e.Index < 0 || e.Index >= _royaleLiveFeed.Items.Count)
                return;

            e.DrawBackground();
            var item = _royaleLiveFeed.Items[e.Index].ToString() ?? "";
            
            // Color-code events: Green (solve), Red (elimination), Blue (player), White (default)
            var color = item.Contains("solved") || item.Contains("+") ? Color.LimeGreen :
                       item.Contains("eliminated") ? RoyaleAccent :
                       item.Contains("You") ? AccentColor :
                       TextPrimaryColor;

            using var brush = new SolidBrush(color);
            using var font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            var rect = new Rectangle(e.Bounds.X + 8, e.Bounds.Y + 4, e.Bounds.Width - 16, e.Bounds.Height - 8);
            e.Graphics.DrawString(item, font, brush, rect);
        };
    }

    private void UpdateRoyaleRankPanel()
    {
        if (_royaleRun == null || !_royaleRankPanel.Visible)
            return;

        var rankList = _royaleRankPanel.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
        if (rankList == null)
            return;

        rankList.SuspendLayout();
        rankList.Controls.Clear();

        // Show top 10 + player if not in top 10
        var ranked = _royaleRun.Entrants
            .Where(e => !e.IsEliminated)
            .OrderByDescending(e => e.WordsSolved)
            .ThenByDescending(e => e.SecondsRemaining)
            .ToArray();

        var playerName = "You";
        var top10 = ranked.Take(10).ToList();
        var playerRank = Array.IndexOf(ranked, ranked.FirstOrDefault(e => e.Name == playerName)) + 1;

        foreach (var (entrant, index) in top10.Select((e, i) => (e, i)))
        {
            var isPlayer = entrant.Name == playerName;
            var rankLabel = new Label
            {
                Text = $"#{index + 1}  {entrant.Name}\n{entrant.WordsSolved} solved | {entrant.SecondsRemaining}s",
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, isPlayer ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = isPlayer ? AccentColor : TextPrimaryColor,
                Padding = new Padding(4),
                MaximumSize = new Size(280, 0)
            };
            rankList.Controls.Add(rankLabel);
        }

        // Add player if outside top 10
        if (playerRank > 10 && playerRank <= ranked.Length)
        {
            var playerEntrant = ranked[playerRank - 1];
            rankList.Controls.Add(new Label
            {
                Text = "...",
                AutoSize = true,
                ForeColor = TextMutedColor,
                Padding = new Padding(4)
            });
            rankList.Controls.Add(new Label
            {
                Text = $"#{playerRank}  {playerEntrant.Name}\n{playerEntrant.WordsSolved} solved | {playerEntrant.SecondsRemaining}s",
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = AccentColor,
                Padding = new Padding(4),
                MaximumSize = new Size(280, 0)
            });
        }

        // Elimination zone indicator
        var dangerCount = _royaleRun.GetEliminationZoneCount(15);
        if (dangerCount > 0)
        {
            rankList.Controls.Add(new Label
            {
                Text = $"\n⚠ ELIMINATION ZONE\n{dangerCount} player{(dangerCount == 1 ? "" : "s")} under 15s",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = RoyaleAccent,
                Padding = new Padding(4, 12, 4, 4),
                MaximumSize = new Size(280, 0)
            });
        }

        rankList.ResumeLayout();
    }

    private void AddRoyaleEvent(string eventText)
    {
        _royaleEvents.Enqueue(eventText);
        while (_royaleEvents.Count > MaxRoyaleEvents)
            _royaleEvents.Dequeue();

        _royaleLiveFeed.BeginUpdate();
        _royaleLiveFeed.Items.Clear();
        foreach (var ev in _royaleEvents.Reverse())
            _royaleLiveFeed.Items.Add(ev);
        _royaleLiveFeed.EndUpdate();
    }

    private Control BuildBoardFrame(int width)
    {
        var frame = new Panel
        {
            Width = width,
            Height = 950,  // 4K-optimized height
            Padding = new Padding(24),
            Margin = new Padding(0, 4, 0, 16),
            BackColor = CardBackColor
        };
        frame.Paint += (_, e) =>
        {
            // Mode-specific accent border
            var accentColor = _activeMode == WordleGameMode.WordsRoyale ? RoyaleAccent :
                             _activeMode == WordleGameMode.MegaBoard ? MegaAccent :
                             AccentColor;
            using var border = new Pen(accentColor, 3);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawRectangle(border, 1, 1, frame.Width - 3, frame.Height - 3);
        };
        frame.Controls.Add(_board);
        _board.Location = new Point(24, 24);
        _board.BackColor = frame.BackColor;
        frame.Resize += (_, _) =>
        {
            _board.Left = Math.Max(24, (frame.ClientSize.Width - _board.Width) / 2);
        };
        return frame;
    }

    private void ApplyControlTheme(Control root)
    {
        foreach (Control control in root.Controls)
        {
            if (control is Label label)
            {
                label.ForeColor = label.ForeColor == Color.DimGray ? TextMutedColor : TextPrimaryColor;
            }
            else if (control is Button button)
            {
                var isKeyboardButton = button.Tag is char || button.Text is "Enter" or "Back";
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = CardBorderColor;
                button.FlatAppearance.BorderSize = 1;
                button.BackColor = button == _pitchSetup || button == _submit
                    ? AccentColor
                    : Color.FromArgb(34, 43, 65);
                button.ForeColor = Color.White;
                button.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                button.Padding = isKeyboardButton ? new Padding(0) : new Padding(8, 4, 8, 4);
                button.MinimumSize = isKeyboardButton ? Size.Empty : new Size(108, 38);
            }
            else if (control is ComboBox combo)
            {
                combo.BackColor = Color.FromArgb(32, 40, 60);
                combo.ForeColor = TextPrimaryColor;
                combo.FlatStyle = FlatStyle.Flat;
                combo.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            }
            else if (control is TextBox textBox)
            {
                textBox.BackColor = Color.FromArgb(32, 40, 60);
                textBox.ForeColor = TextPrimaryColor;
                textBox.BorderStyle = BorderStyle.FixedSingle;
                textBox.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            }
            else if (control is ListBox listBox)
            {
                listBox.BackColor = Color.FromArgb(18, 24, 38);
                listBox.ForeColor = TextPrimaryColor;
                listBox.BorderStyle = BorderStyle.FixedSingle;
                listBox.Font = new Font("Consolas", 9, FontStyle.Regular);
            }
            else if (control is FlowLayoutPanel flow && flow != _megaBoardDashboard)
            {
                flow.BackColor = CardBackColor;
            }
            else if (control is TableLayoutPanel table && table != _board)
            {
                table.BackColor = CardBackColor;
            }

            if (control.HasChildren)
                ApplyControlTheme(control);
        }
    }

    private Control BuildHeroBanner()
    {
        var banner = new Panel
        {
            Width = ContentWidth,
            Height = 158,
            Margin = new Padding(0, 0, 0, 10)
        };
        banner.Paint += (_, e) =>
        {
            var graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var backgroundBrush = new LinearGradientBrush(
                new Rectangle(0, 0, banner.Width, banner.Height),
                Color.FromArgb(37, 61, 132),
                Color.FromArgb(121, 31, 99),
                30f);
            graphics.FillRectangle(backgroundBrush, 0, 0, banner.Width, banner.Height);

            using var framePen = new Pen(Color.FromArgb(220, 240, 255), 2);
            graphics.DrawRectangle(framePen, 1, 1, banner.Width - 3, banner.Height - 3);
            DrawHeroTile(graphics, "W", 16, 18, Color.SeaGreen);
            DrawHeroTile(graphics, "O", 66, 18, Color.Goldenrod);
            DrawHeroTile(graphics, "R", 116, 18, Color.SlateGray);
            DrawHeroTile(graphics, "D", 166, 18, Color.SteelBlue);
            DrawHeroTile(graphics, "S", 216, 18, Color.MediumPurple);

            using var titleFont = new Font("Segoe UI", 22, FontStyle.Bold);
            using var subtitleFont = new Font("Segoe UI", 10, FontStyle.Regular);
            using var calloutFont = new Font("Segoe UI", 9, FontStyle.Bold);
            graphics.DrawString("Ultimate Word Game Demo", titleFont, Brushes.White, new PointF(16, 74));
            graphics.DrawString("Royale • Mega Board • Time Attack • Vs. CPU", subtitleFont, Brushes.GhostWhite, new PointF(18, 114));
            graphics.DrawString("PITCH BUILD", calloutFont, Brushes.WhiteSmoke, new PointF(404, 16));
        };
        return banner;
    }

    private static void DrawHeroTile(Graphics graphics, string letter, int x, int y, Color color)
    {
        using var shadowBrush = new SolidBrush(Color.FromArgb(70, 0, 0, 0));
        graphics.FillRectangle(shadowBrush, x + 2, y + 3, 42, 42);
        using var brush = new LinearGradientBrush(new Rectangle(x, y, 42, 42), ControlPaint.Light(color, 0.15f), color, 90f);
        graphics.FillRectangle(brush, x, y, 42, 42);
        using var borderPen = new Pen(Color.FromArgb(245, 250, 255), 1);
        graphics.DrawRectangle(borderPen, x, y, 42, 42);
        using var font = new Font("Segoe UI", 16, FontStyle.Bold);
        graphics.DrawString(letter, font, Brushes.White, new PointF(x + 10, y + 6));
    }

    private Control BuildFeatureChipsRow()
    {
        var chips = new FlowLayoutPanel
        {
            AutoSize = true,
            MaximumSize = new Size(ContentWidth, 0),
            Margin = new Padding(0, 0, 0, 8)
        };
        foreach (var chip in new[]
                 {
                     "99-Entrant Royale",
                     "100-Board Mega",
                     "Combo Time Attack",
                     "Persona CPU Race",
                     "Power-Ups + Badges"
                 })
        {
            chips.Controls.Add(new Label
            {
                AutoSize = true,
                Text = chip,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(58, 79, 136),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Padding = new Padding(8, 4, 8, 4),
                Margin = new Padding(0, 0, 6, 6)
            });
        }
        return chips;
    }

    private Control BuildKeyboard()
    {
        var keyboard = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0, 0, 0, 10)
        };

        foreach (var row in new[] { "QWERTYUIOP", "ASDFGHJKL", "ZXCVBNM" })
        {
            var panel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 2, 0, 2)
            };

            foreach (var letter in row)
            {
                var button = new Button
                {
                    Text = letter.ToString(),
                    Width = 42,
                    Height = 36,
                    Margin = new Padding(2),
                    Tag = letter
                };
                button.Click += (_, _) => AddKeyboardLetter(letter);
                _keyboardButtons[letter] = button;
                panel.Controls.Add(button);
            }

            if (row == "ZXCVBNM")
            {
                var enter = new Button
                {
                    Text = "Enter",
                    Width = 64,
                    Height = 36,
                    Margin = new Padding(2)
                };
                enter.Click += (_, _) => SubmitGuess();
                panel.Controls.Add(enter);

                var backspace = new Button
                {
                    Text = "Back",
                    Width = 64,
                    Height = 36,
                    Margin = new Padding(2)
                };
                backspace.Click += (_, _) => BackspaceGuess();
                panel.Controls.Add(backspace);
            }

            keyboard.Controls.Add(panel);
        }

        return keyboard;
    }

    private void BindEvents()
    {
        _submit.Click += (_, _) => SubmitGuess();
        _newGame.Click += (_, _) => StartNewGame();
        _giveUp.Click += (_, _) => GiveUp();
        _copyResult.Click += (_, _) => CopyResult();
        _copyStats.Click += (_, _) => CopyStats();
        _openStatsFolder.Click += (_, _) => OpenStatsFolder();
        _resetStats.Click += (_, _) => ResetStats();
        _usePowerUp.Click += (_, _) => UseSelectedPowerUp();
        _modeSelector.SelectedIndexChanged += (_, _) => UpdateModeHelp();
        _modeSelector.SelectedIndexChanged += (_, _) => UpdateMegaBoardControls();
        _modeSelector.SelectedIndexChanged += (_, _) => UpdateCpuRaceControls();
        _modeSelector.SelectedIndexChanged += (_, _) => ClearTransientHudText();
        _modeSelector.SelectedIndexChanged += (_, _) => TryPlayMusicForMode(GetSelectedMode());
        _modeSelector.SelectedIndexChanged += (_, _) => PlaySfx("mode-switch");
        _cpuDifficultySelector.SelectedIndexChanged += (_, _) => UpdateModeHelp();
        _megaBoardCountSelector.SelectedIndexChanged += (_, _) => UpdateModeHelp();
        _megaBoardFilterSelector.SelectedIndexChanged += (_, _) => RenderMegaBoardDashboard();
        _megaBoardSortSelector.SelectedIndexChanged += (_, _) => RenderMegaBoardDashboard();
        _focusCriticalMegaBoard.Click += (_, _) => FocusCriticalMegaBoard();
        _pitchSetup.Click += (_, _) => ApplyPitchSetup();
        _timer.Tick += (_, _) => TickTimer();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.F11)
            {
                ToggleFullscreen();
                e.Handled = true;
            }
        };
        _guessInput.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                SubmitGuess();
                e.SuppressKeyPress = true;
            }
        };
    }

    private void ToggleFullscreen()
    {
        if (FormBorderStyle == FormBorderStyle.None)
        {
            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState = FormWindowState.Maximized;
            return;
        }

        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Normal;
        Bounds = Screen.FromControl(this).Bounds;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _musicPlayer?.Stop();
        _musicPlayer?.Dispose();
        _musicPlayer = null;
        foreach (var players in _sfxPlayers.Values)
        {
            foreach (var player in players)
                player.Dispose();
        }
        base.OnFormClosed(e);
    }

    private void ClearTransientHudText()
    {
        _roundSummary.Text = string.Empty;
        _wordInfo.Text = string.Empty;
    }

    private void StartNewGame()
    {
        if (GetSelectedMode() != WordleGameMode.Survival || _session.Status == GameStatus.Lost)
            _survivalStreak = 0;
        _timeAttackRun = null;
        _cpuRaceRun = null;
        _royaleRun = null;
        _megaBoardRun = null;
        if (GetSelectedMode() != WordleGameMode.Alphabet || _session.Status == GameStatus.Lost || _alphabetIndex >= 26)
            _alphabetIndex = 0;
        _armedClockBonusSeconds = 0;
        _shieldArmed = false;
        _crownArmed = false;
        _freezeTicksRemaining = 0;
        _royaleBotFreezeTicksRemaining = 0;

        _session = StartConfiguredSession();
        _lastLowTimeCueSecond = int.MaxValue;
        PlaySfx("new-game");
        ResetBoard();
    }

    private void ResetBoard()
    {
        _timer.Stop();
        BuildBoard(_session.MaxAttempts, _session.WordLength);
        _keyboardState.Clear();
        _roundRecorded = false;
        foreach (var button in _keyboardButtons.Values)
        {
            button.Enabled = true;
            button.BackColor = SystemColors.Control;
            button.ForeColor = SystemColors.ControlText;
        }

        for (var row = 0; row < _tiles.GetLength(0); row++)
        {
            for (var column = 0; column < _tiles.GetLength(1); column++)
            {
                _tiles[row, column].Text = string.Empty;
                _tiles[row, column].BackColor = TileBaseColor;
                _tiles[row, column].ForeColor = TileTextColor;
            }
        }

        RenderExistingGuesses();
        RenderMegaBoardDashboard();
        _guessInput.Enabled = true;
        _submit.Enabled = true;
        _giveUp.Enabled = true;
        _copyResult.Enabled = false;
        _modeSelector.Enabled = true;
        _wordLengthSelector.Enabled = _activeMode != WordleGameMode.Survival;
        _megaBoardCountSelector.Enabled = _activeMode == WordleGameMode.MegaBoard;
        _cpuDifficultySelector.Enabled = _activeMode == WordleGameMode.VsCpu;
        _cpuRaceSummary.Visible = _activeMode == WordleGameMode.VsCpu;
        _megaBoardDashboard.Visible = _activeMode == WordleGameMode.MegaBoard;
        _royaleRankPanel.Visible = _activeMode == WordleGameMode.WordsRoyale;
        _royaleLiveFeed.Visible = _activeMode == WordleGameMode.WordsRoyale;
        _newGame.Text = _activeMode == WordleGameMode.Survival && _survivalStreak > 0 ? "Restart Survival" : "New Game";
        _guessInput.MaxLength = _session.WordLength;
        _guessInput.Clear();
        _guessInput.Focus();
        _roundSummary.Text = string.Empty;
        _wordInfo.Text = string.Empty;
        if (_activeMode == WordleGameMode.Alphabet)
            RenderAlphabetFreeLetter();
        RenderRevealedPowerUpClues();
        var metadata = _wordleService.GetWordMetadata(_session.Answer);
        _commonality.Text = $"Answer rarity: {metadata.Label}";
        _commonality.ForeColor = GetCommonalityColor(metadata.Commonality);
        TryPlayMusicForMode(_activeMode);
        if (_activeMode == WordleGameMode.Timed)
        {
            _timeAttackRun ??= new TimeAttackRun();
            _secondsRemaining = _timeAttackRun.SecondsRemaining;
            _timer.Start();
        }
        if (_activeMode == WordleGameMode.VsCpu)
        {
            var profile = GetSelectedCpuDifficulty();
            _cpuRaceRun ??= new CpuRaceRun(targetWords: profile.TargetWords, cpuSecondsPerWord: profile.CpuSecondsPerWord, cpuPersona: profile.CpuPersona);
            _cpuRaceSummary.Text = BuildCpuRaceLiveSummary();
            _cpuRaceSummary.ForeColor = GetCpuRaceColor();
            _timer.Start();
        }
        if (_activeMode == WordleGameMode.WordsRoyale)
        {
            _royaleRun ??= new RoyaleRun(["You"]);
            _royaleBotPulse = 0;
            _royaleEvents.Clear();
            AddRoyaleEvent($"Battle Royale begins! {_royaleRun.AliveCount} players");
            UpdateRoyaleRankPanel();
            _timer.Start();
        }
        SetLiveStatus();
        UpdateModeHelp();
        RefreshStats();
    }

    private void TryPlayMusicForMode(WordleGameMode mode)
    {
        var profile = ModeMusicProfileService.GetProfile(mode);
        if (!_tracksByEnergy.TryGetValue(profile.Energy, out var tracks) || tracks.Length == 0)
            return;

        var trackPath = tracks[Math.Abs((int)mode) % tracks.Length];
        if (string.Equals(_currentMusicPath, trackPath, StringComparison.OrdinalIgnoreCase))
            return;

        _musicPlayer?.Stop();
        _musicPlayer?.Dispose();
        _musicPlayer = null;

        try
        {
            _musicPlayer = new SoundPlayer(trackPath);
            _musicPlayer.LoadAsync();
            _musicPlayer.PlayLooping();
            _currentMusicPath = trackPath;
        }
        catch (Exception ex) when (ex is InvalidOperationException or FileNotFoundException)
        {
            _currentMusicPath = null;
        }
    }

    private static Dictionary<MusicEnergy, string[]> BuildMusicLibrary()
    {
        var assetsPath = Path.Combine(AppContext.BaseDirectory, "Assets");
        if (!Directory.Exists(assetsPath))
            return [];

        var wavFiles = Directory.GetFiles(assetsPath, "*.wav", SearchOption.TopDirectoryOnly);
        return wavFiles
            .Where(path =>
            {
                var fileName = Path.GetFileName(path);
                return fileName.StartsWith("Chill", StringComparison.OrdinalIgnoreCase)
                    || fileName.StartsWith("Metal", StringComparison.OrdinalIgnoreCase);
            })
            .GroupBy(path => Path.GetFileName(path).StartsWith("Chill", StringComparison.OrdinalIgnoreCase)
                ? MusicEnergy.Chill
                : MusicEnergy.Metal)
            .ToDictionary(group => group.Key, group => group.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private static Dictionary<string, SoundPlayer[]> BuildSfxPlayers()
    {
        var assetsPath = Path.Combine(AppContext.BaseDirectory, "Assets");
        if (!Directory.Exists(assetsPath))
            return [];

        var sfxFiles = Directory.GetFiles(assetsPath, "Sfx - *.wav", SearchOption.TopDirectoryOnly);
        var grouped = sfxFiles
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .GroupBy(file =>
            {
                var cue = Path.GetFileNameWithoutExtension(file)["Sfx - ".Length..];
                var variant = cue.IndexOf('-');
                return variant > 0 && int.TryParse(cue[(variant + 1)..], out _)
                    ? cue[..variant]
                    : cue;
            }, StringComparer.OrdinalIgnoreCase);
        var players = new Dictionary<string, SoundPlayer[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in grouped)
        {
            players[group.Key] = group
                .Select(file =>
                {
                    var player = new SoundPlayer(file);
                    player.LoadAsync();
                    return player;
                })
                .ToArray();
        }

        return players;
    }

    private void PlaySfx(string cue)
    {
        if (!_sfxPlayers.TryGetValue(cue, out var players) || players.Length == 0)
            return;

        var player = players.Length == 1 ? players[0] : players[_sfxRandom.Next(players.Length)];
        try
        {
            player.Play();
        }
        catch (InvalidOperationException)
        {
            System.Diagnostics.Debug.WriteLine($"Unable to play SFX cue '{cue}'.");
        }
    }

    private void SubmitGuess()
    {
        try
        {
            if (_activeMode == WordleGameMode.MegaBoard)
            {
                SubmitMegaBoardGuess();
                return;
            }

            var result = _wordleService.SubmitGuess(_session, _guessInput.Text, _activeMode);
            PlaySfx(result.IsCorrect ? "word-solved" : "guess-submit");
            RenderGuess(result, _session.Guesses.Count - 1);
            if (_activeMode != WordleGameMode.Ghost)
                UpdateKeyboard(result);
            _guessInput.Clear();

            if (_session.Status == GameStatus.Won)
            {
                if (_activeMode == WordleGameMode.Timed)
                {
                    ContinueTimeAttackAfterSolved();
                    return;
                }
                if (_activeMode == WordleGameMode.VsCpu)
                {
                    ContinueCpuRaceAfterSolved();
                    return;
                }
                if (_activeMode == WordleGameMode.WordsRoyale)
                {
                    ContinueRoyaleAfterSolved();
                    return;
                }
                if (_activeMode == WordleGameMode.Alphabet)
                {
                    ContinueAlphabetAfterSolved();
                    return;
                }

                EndGame($"Solved in {_session.Guesses.Count}/{_session.MaxAttempts}!");
            }
            else if (_session.Status == GameStatus.Lost)
            {
                if (_activeMode == WordleGameMode.Timed)
                {
                    ContinueTimeAttackAfterFailed($"Failed word: {_session.Answer}. -{TimeAttackRun.DefaultFailurePenaltySeconds}s.");
                    return;
                }
                if (_activeMode == WordleGameMode.WordsRoyale)
                {
                    ContinueRoyaleAfterFailed($"Failed word: {_session.Answer}. -{RoyaleRun.DefaultFailurePenaltySeconds}s.");
                    return;
                }

                EndGame($"Out of tries. The word was {_session.Answer}.");
            }
            else
            {
                SetLiveStatus();
            }
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            _status.Text = ex.Message;
            _status.ForeColor = Color.Firebrick;
            PlaySfx("ui-error");
        }
    }

    private void RenderGuess(WordleGuessResult result, int row)
    {
        for (var column = 0; column < result.Guess.Length; column++)
        {
            var tile = _tiles[row, column];
            tile.Text = result.Guess[column].ToString();
            tile.ForeColor = Color.White;
            tile.BackColor = _activeMode == WordleGameMode.Ghost
                ? Color.DimGray
                : GetTileColor(result.Tiles[column]);
        }

        if (_activeMode == WordleGameMode.Ghost && !result.IsCorrect)
            _roundSummary.Text = BuildGhostFeedback(result);
    }

    private void UpdateKeyboard(WordleGuessResult result)
    {
        if (_activeMode == WordleGameMode.MegaBoard && _megaBoardRun is not null)
        {
            RefreshMegaBoardKeyboard();
            return;
        }

        for (var index = 0; index < result.Guess.Length; index++)
        {
            var letter = result.Guess[index];
            var state = result.Tiles[index];
            if (!_keyboardState.Set(letter, state))
                continue;

            var button = _keyboardButtons[letter];
            button.BackColor = GetTileColor(state);
            button.ForeColor = Color.White;
        }
    }

    private void RefreshMegaBoardKeyboard()
    {
        if (_megaBoardRun is null)
            return;

        _keyboardState.Clear();
        foreach (var pair in _megaBoardRun.BuildAggregateKeyboardState())
        {
            _keyboardState.Set(pair.Key, pair.Value);
            if (_keyboardButtons.TryGetValue(pair.Key, out var button))
            {
                button.BackColor = GetTileColor(pair.Value);
                button.ForeColor = Color.White;
            }
        }
    }

    private void RenderExistingGuesses()
    {
        for (var row = 0; row < _session.Guesses.Count; row++)
        {
            var result = _session.Guesses[row];
            RenderGuess(result, row);
            if (_activeMode != WordleGameMode.Ghost)
                UpdateKeyboard(result);
        }
    }

    private void RenderMegaBoardDashboard()
    {
        _megaBoardDashboard.Controls.Clear();
        if (_activeMode != WordleGameMode.MegaBoard || _megaBoardRun is null)
            return;

        var focus = _megaBoardRun.FocusBoard;
        foreach (var (board, index) in GetOrderedMegaBoardEntries())
        {
            if (!ShouldShowMegaBoard(board))
                continue;

            var isFocus = ReferenceEquals(board, focus);
            var label = new Label
            {
                Text = (index + 1).ToString(),
                Width = _megaBoardRun.BoardCount > 50 ? 42 : 54,
                Height = 24,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(1),
                BackColor = GetMegaBoardStatusColor(board, isFocus),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8, isFocus ? FontStyle.Bold : FontStyle.Regular),
                Tag = BuildMegaBoardPreview(index, board)
            };
            label.MouseEnter += (_, _) => _roundSummary.Text = label.Tag?.ToString() ?? string.Empty;
            label.Click += (_, _) => FocusMegaBoard(board);
            _megaBoardDashboard.Controls.Add(label);
        }
    }

    private static Color GetMegaBoardStatusColor(WordleSession board, bool isFocus)
    {
        if (isFocus)
            return Color.RoyalBlue;
        return board.Status switch
        {
            GameStatus.Won => Color.SeaGreen,
            GameStatus.Lost => Color.Firebrick,
            GameStatus.InProgress when board.RemainingAttempts <= 1 => Color.OrangeRed,
            GameStatus.InProgress when board.RemainingAttempts <= 2 => Color.DarkOrange,
            _ => Color.DimGray
        };
    }

    private string BuildMegaBoardPreview(int index, WordleSession board)
    {
        if (_megaBoardRun is null)
            return string.Empty;

        var pattern = _megaBoardRun.BuildKnownPattern(board);
        return $"Board #{index + 1}: {board.Status}, {board.RemainingAttempts} tries left, pattern {pattern}";
    }

    private string BuildMegaBoardLiveSummary()
    {
        if (_megaBoardRun is null)
            return "Mega Board has not started.";

        var visible = _megaBoardRun.Boards.Count(ShouldShowMegaBoard);
        return $"Mega Board: {_megaBoardRun.SolvedCount}/{_megaBoardRun.BoardCount} solved, {_megaBoardRun.LostCount} lost, {_megaBoardRun.DangerCount} in danger, score {_megaBoardRun.CalculateScore()}. View {GetSelectedMegaBoardFilter()}: {visible}/{_megaBoardRun.BoardCount}; sort {GetSelectedMegaBoardSort()}. Focus #{GetMegaBoardFocusIndex() + 1}: {_megaBoardRun.BuildKnownPattern(_megaBoardRun.FocusBoard)}. {BuildMegaBoardDangerReport()}";
    }

    private string BuildMegaBoardDangerReport()
    {
        if (_megaBoardRun is null || _megaBoardRun.DangerCount == 0)
            return "No critical boards yet.";

        var entries = _megaBoardRun.GetEndangeredBoards(3)
            .Select(board => $"#{GetMegaBoardIndex(board) + 1} ({board.RemainingAttempts} left: {_megaBoardRun.BuildKnownPattern(board)})");
        return $"Critical: {string.Join("; ", entries)}.";
    }

    private IEnumerable<(WordleSession Board, int Index)> GetOrderedMegaBoardEntries()
    {
        if (_megaBoardRun is null)
            return [];

        var entries = _megaBoardRun.Boards.Select((board, index) => (Board: board, Index: index));
        return GetSelectedMegaBoardSort() switch
        {
            "Smart" => entries
                .OrderByDescending(entry => ReferenceEquals(entry.Board, _megaBoardRun.FocusBoard))
                .ThenByDescending(entry => CountKnownCorrectTiles(entry.Board))
                .ThenBy(entry => entry.Board.RemainingAttempts)
                .ThenBy(entry => entry.Index),
            "Danger" => entries
                .OrderBy(entry => entry.Board.Status == GameStatus.InProgress ? 0 : 1)
                .ThenBy(entry => entry.Board.RemainingAttempts)
                .ThenByDescending(entry => CountKnownCorrectTiles(entry.Board))
                .ThenBy(entry => entry.Index),
            "Attempts" => entries
                .OrderByDescending(entry => entry.Board.Guesses.Count)
                .ThenBy(entry => entry.Board.RemainingAttempts)
                .ThenBy(entry => entry.Index),
            "Status" => entries
                .OrderBy(entry => entry.Board.Status)
                .ThenBy(entry => entry.Index),
            _ => entries
        };
    }

    private static int CountKnownCorrectTiles(WordleSession board) =>
        board.Guesses.Sum(guess => guess.Tiles.Count(tile => tile == WordleTileState.Correct));

    private bool ShouldShowMegaBoard(WordleSession board) => GetSelectedMegaBoardFilter() switch
    {
        "Active" => board.Status == GameStatus.InProgress,
        "Danger" => board.Status == GameStatus.InProgress && board.RemainingAttempts <= 2,
        "Solved" => board.Status == GameStatus.Won,
        "Lost" => board.Status == GameStatus.Lost,
        _ => true
    };

    private void FocusMegaBoard(WordleSession board)
    {
        if (_activeMode != WordleGameMode.MegaBoard || _megaBoardRun is null)
            return;

        _session = board;
        PlaySfx("mega-focus");
        ResetBoard();
        _roundSummary.Text = BuildMegaBoardPreview(GetMegaBoardFocusIndex(), board);
    }

    private void FocusCriticalMegaBoard()
    {
        if (_activeMode != WordleGameMode.MegaBoard || _megaBoardRun is null)
            return;

        var board = _megaBoardRun.GetEndangeredBoards(1).FirstOrDefault();
        if (board is null)
        {
            PlaySfx("ui-error");
            _roundSummary.Text = "No active Mega Board needs attention.";
            return;
        }

        PlaySfx("critical-focus");
        FocusMegaBoard(board);
        _roundSummary.Text = $"Critical focus: {BuildMegaBoardPreview(GetMegaBoardFocusIndex(), board)}";
    }

    private void UpdateMegaBoardControls()
    {
        var isMega = GetSelectedMode() == WordleGameMode.MegaBoard;
        _megaBoardCountSelector.Enabled = isMega;
        _megaBoardFilterSelector.Enabled = isMega;
        _megaBoardSortSelector.Enabled = isMega;
        _focusCriticalMegaBoard.Enabled = isMega;
        _megaBoardDashboard.Visible = isMega;
    }

    private void UpdateCpuRaceControls()
    {
        var isCpuRace = GetSelectedMode() == WordleGameMode.VsCpu;
        _cpuDifficultySelector.Enabled = isCpuRace;
        _cpuRaceSummary.Visible = isCpuRace;
        if (isCpuRace)
            _cpuRaceSummary.Text = BuildCpuRaceLiveSummary();
    }

    private void ApplyPitchSetup()
    {
        _survivalStreak = 0;
        _timeAttackRun = null;
        _cpuRaceRun = null;
        _royaleRun = null;
        _megaBoardRun = null;
        _alphabetIndex = 0;
        _armedClockBonusSeconds = 0;
        _shieldArmed = false;
        _crownArmed = false;
        _freezeTicksRemaining = 0;
        _royaleBotFreezeTicksRemaining = 0;

        _modeSelector.SelectedItem = _modeSelector.Items.OfType<ModeOption>().First(option => option.Mode == WordleGameMode.WordsRoyale);
        _wordLengthSelector.SelectedItem = 5;
        _cpuDifficultySelector.SelectedItem = _cpuDifficultySelector.Items.OfType<CpuDifficultyOption>().First(option => option.Label == "Elite");
        _megaBoardCountSelector.SelectedItem = 100;
        _megaBoardFilterSelector.SelectedItem = "Danger";
        _megaBoardSortSelector.SelectedItem = "Smart";
        UpdateModeHelp();
        UpdateMegaBoardControls();
        UpdateCpuRaceControls();
        StartNewGame();
        PlaySfx("setup-ready");
        _roundSummary.Text = "Pitch setup ready: Words Royale (5 letters) is live. CPU and Mega Board showcase presets are preloaded for quick mode swaps.";
    }

    private void SubmitMegaBoardGuess()
    {
        _megaBoardRun ??= _wordleService.StartMegaBoardRun(boardCount: GetSelectedMegaBoardCount(), wordLength: _session.WordLength, maxAttempts: _session.MaxAttempts);
        var focusedBoard = _megaBoardRun.FocusBoard;
        _wordleService.SubmitGuess(_megaBoardRun, _guessInput.Text);
        var focusedResult = focusedBoard.Guesses.Last();
        RenderGuess(focusedResult, focusedBoard.Guesses.Count - 1);
        UpdateKeyboard(focusedResult);
        RenderMegaBoardDashboard();
        _guessInput.Clear();

        if (_megaBoardRun.IsComplete)
        {
            _session = _megaBoardRun.FocusBoard;
            EndGame(_megaBoardRun.IsPerfectClear
                ? $"Mega Board clear! Solved all {_megaBoardRun.BoardCount} boards."
                : $"Mega Board complete: {_megaBoardRun.SolvedCount}/{_megaBoardRun.BoardCount} solved.");
            return;
        }

        if (focusedBoard.Status != GameStatus.InProgress)
        {
            _session = _megaBoardRun.FocusBoard;
            ResetBoard();
            _roundSummary.Text = $"Smart focus: {BuildMegaBoardPreview(GetMegaBoardFocusIndex(), _session)}";
            return;
        }

        SetLiveStatus();
        _roundSummary.Text = BuildMegaBoardLiveSummary();
    }

    private static string BuildGhostFeedback(WordleGuessResult result)
    {
        var exact = result.Tiles.Count(tile => tile == WordleTileState.Correct);
        var present = result.Tiles.Count(tile => tile == WordleTileState.Present);
        return $"Ghost feedback for {result.Guess}: {exact} exact, {present} present somewhere.";
    }

    private void AddKeyboardLetter(char letter)
    {
        if (!_guessInput.Enabled || _guessInput.TextLength >= _guessInput.MaxLength)
            return;

        _guessInput.Text += letter;
        PlaySfx("key-tap");
        _guessInput.SelectionStart = _guessInput.TextLength;
        _guessInput.Focus();
    }

    private void BackspaceGuess()
    {
        if (!_guessInput.Enabled || _guessInput.TextLength == 0)
            return;

        _guessInput.Text = _guessInput.Text[..^1];
        PlaySfx("backspace");
        _guessInput.SelectionStart = _guessInput.TextLength;
        _guessInput.Focus();
    }

    private void EndGame(string message)
    {
        var metadata = _wordleService.GetWordMetadata(_session.Answer);
        _timer.Stop();
        RecordRound(metadata);
        _status.Text = message;
        PlaySfx(_session.Status == GameStatus.Won ? "game-win" : "game-lose");
        if (_session.Status == GameStatus.Lost)
            _status.Text += $" Rarity: {metadata.Label}.";
        _roundSummary.Text = _activeMode == WordleGameMode.WordsRoyale && _royaleRun is not null
            ? BuildRoyaleSummary()
            : _activeMode == WordleGameMode.VsCpu && _cpuRaceRun is not null && _cpuRaceRun.IsComplete
                ? CpuRaceResultFormatter.BuildCeremonyText(_cpuRaceRun)
            : _activeMode == WordleGameMode.Timed && _timeAttackRun is not null
                ? TimeAttackResultFormatter.BuildCeremonyText(_timeAttackRun)
            : BuildRoundSummary(metadata);
        _wordInfo.Text = BuildWordInfo(metadata);
        if (_activeMode == WordleGameMode.WordsRoyale && _royaleRun is not null)
        {
            var ceremonyColor = GetRoyalePlacementColor(_royaleRun.GetPlacement("You"));
            _status.ForeColor = ceremonyColor;
            _roundSummary.ForeColor = ceremonyColor;
        }
        else if (_activeMode == WordleGameMode.Timed && _timeAttackRun is not null)
        {
            var ceremonyColor = GetTimeAttackCeremonyColor(_timeAttackRun);
            _status.ForeColor = ceremonyColor;
            _roundSummary.ForeColor = ceremonyColor;
        }
        else
        {
            _status.ForeColor = _session.Status == GameStatus.Won ? Color.DarkGreen : Color.Firebrick;
        }
        _guessInput.Enabled = false;
        _submit.Enabled = false;
        _giveUp.Enabled = false;
        _copyResult.Enabled = true;
        _modeSelector.Enabled = true;
        _wordLengthSelector.Enabled = _activeMode != WordleGameMode.Survival;
        if (_activeMode == WordleGameMode.Survival && _session.Status == GameStatus.Won)
            _newGame.Text = "Continue Survival";
        if (_activeMode == WordleGameMode.Timed && _timeAttackRun is { IsOver: false })
            _newGame.Text = "Restart Time Attack";
        if (_activeMode == WordleGameMode.WordsRoyale)
            _newGame.Text = "New Royale";
        RefreshStats();
    }

    private void GiveUp()
    {
        if (_session.Status != GameStatus.InProgress)
            return;

        PlaySfx("round-failed");
        _session.Expire();
        if (_activeMode == WordleGameMode.Timed)
        {
            ContinueTimeAttackAfterFailed($"Skipped word: {_session.Answer}. -{TimeAttackRun.DefaultFailurePenaltySeconds}s.");
            return;
        }
        if (_activeMode == WordleGameMode.WordsRoyale)
        {
            ContinueRoyaleAfterFailed($"Skipped word: {_session.Answer}. -{RoyaleRun.DefaultFailurePenaltySeconds}s.");
            return;
        }
        if (_activeMode == WordleGameMode.MegaBoard && _megaBoardRun is not null)
        {
            foreach (var board in _megaBoardRun.Boards.Where(board => board.Status == GameStatus.InProgress))
                board.Expire();
            EndGame($"Mega Board revealed. Solved {_megaBoardRun.SolvedCount}/{_megaBoardRun.BoardCount} boards.");
            return;
        }

        EndGame($"Revealed. The word was {_session.Answer}.");
    }

    private string BuildRoundSummary(WordMetadata metadata)
    {
        var result = _session.Status == GameStatus.Won ? "Win" : "Loss";
        if (_activeMode == WordleGameMode.MegaBoard && _megaBoardRun is not null)
            return $"Mega Board: {_megaBoardRun.SolvedCount}/{_megaBoardRun.BoardCount} solved | Lost: {_megaBoardRun.LostCount} | Danger: {_megaBoardRun.DangerCount} | Length: {_session.WordLength} | Score: {_megaBoardRun.CalculateScore()}";

        var score = DemoStatsStore.CalculateRoundScore(_session, _activeMode, metadata);
        return $"Round: {result} | Answer: {_session.Answer} | Mode: {_activeMode} | Length: {_session.WordLength} | Rarity: {metadata.Label} | Attempts: {_session.Guesses.Count}/{_session.MaxAttempts} | Score: {score}";
    }

    private void ResetStats()
    {
        _stats.Clear();
        RestoreDemoSettings();
        _stats.Save(StatsPath);
        PlaySfx("stats-reset");
        RefreshStats();
        _roundSummary.Text = "Stats reset.";
        _wordInfo.Text = string.Empty;
    }

    private static string BuildWordInfo(WordMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata.Description))
            return $"Word info: {metadata.Text} is marked {metadata.Label}.";

        return $"Word info: {metadata.Text} - {metadata.Description}";
    }

    private void CopyResult()
    {
        if (_session.Guesses.Count == 0)
            return;

        Clipboard.SetText(BuildShareText());
        PlaySfx("clipboard");
        _roundSummary.Text = "Copied result to clipboard.";
    }

    private void CopyStats()
    {
        Clipboard.SetText(BuildStatsExport());
        PlaySfx("clipboard");
        _roundSummary.Text = "Copied stats to clipboard.";
    }

    private void OpenStatsFolder()
    {
        var directory = Path.GetDirectoryName(StatsPath);
        if (string.IsNullOrWhiteSpace(directory))
            return;

        Directory.CreateDirectory(directory);
        PlaySfx("open-folder");
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = directory,
            UseShellExecute = true
        });
    }

    private string BuildStatsExport()
    {
        var winRate = _stats.GamesPlayed == 0 ? 0 : (int)Math.Round((double)_stats.Wins / _stats.GamesPlayed * 100);
        var lines = new List<string>
        {
            "Words demo stats",
            $"Played: {_stats.GamesPlayed}",
            $"Wins: {_stats.Wins}",
            $"Losses: {_stats.Losses}",
            $"Win rate: {winRate}%",
            $"Total score: {_stats.TotalScore}",
            $"Best round: {_stats.BestRoundScore}",
            $"Current streak: {_stats.CurrentStreak}",
            $"Best streak: {_stats.BestStreak}",
            $"Best survival: {_stats.BestSurvivalStreak}",
            $"Time Attack runs: {_stats.TimeAttackRuns}",
            $"Best Time Attack score: {_stats.BestTimeAttackScore}",
            $"Best Time Attack solved: {_stats.BestTimeAttackSolved}",
            $"Best Time Attack combo: {_stats.BestTimeAttackCombo}",
            $"Royale runs: {_stats.RoyaleRuns}",
            $"Best Royale placement: {BuildRoyalePlacementText()}",
            $"Royale wins: {_stats.RoyaleWins}",
            $"Royale top-10s: {_stats.RoyaleTopTenFinishes}",
            $"Best Royale solved: {_stats.BestRoyaleSolved}",
            $"Mega Board runs: {_stats.MegaBoardRuns}",
            $"Best Mega Board clears: {_stats.BestMegaBoardClears}",
            $"Best Mega Board score: {_stats.BestMegaBoardScore}",
            $"Mega Board perfect clears: {_stats.MegaBoardPerfectClears}",
            $"Mega Board 100-board clears: {_stats.MegaBoardCenturionClears}",
            $"Power-ups: {BuildPowerUpInventoryText()}",
            $"Perfect games: {_stats.PerfectGames}",
            $"Daily wins: {_stats.DailyWins.Count}",
            $"Words discovered: {_stats.DiscoveredWords.Count}",
            BuildTopStatsText(),
            BuildBadgeText()
        };

        if (_stats.RecentWords.Count > 0)
        {
            lines.Add("Recent words:");
            lines.AddRange(_stats.RecentWords.Take(10).Select(word =>
            {
                var result = word.Won ? "Win" : "Loss";
                return $"- {word.Text} ({word.Length}) {word.Rarity} {word.Mode} {result} {word.AttemptsUsed} guesses {word.Score} pts";
            }));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private string BuildShareText()
    {
        if (_activeMode == WordleGameMode.WordsRoyale && _royaleRun is not null)
            return BuildRoyaleShareText();
        if (_activeMode == WordleGameMode.VsCpu && _cpuRaceRun is not null)
            return CpuRaceResultFormatter.BuildShareText(_cpuRaceRun);
        if (_activeMode == WordleGameMode.Timed && _timeAttackRun is not null)
            return TimeAttackResultFormatter.BuildShareText(_timeAttackRun);
        if (_activeMode == WordleGameMode.MegaBoard && _megaBoardRun is not null)
            return BuildMegaBoardShareText();

        var result = _session.Status == GameStatus.Won
            ? $"{_session.Guesses.Count}/{_session.MaxAttempts}"
            : $"X/{_session.MaxAttempts}";
        var lines = new List<string>
        {
            $"Words {_activeMode} {_session.WordLength} {result} - {DemoStatsStore.CalculateRoundScore(_session, _activeMode, _wordleService.GetWordMetadata(_session.Answer))} pts"
        };

        foreach (var guess in _session.Guesses)
            lines.Add(string.Concat(guess.Tiles.Select(GetShareEmoji)));

        return string.Join(Environment.NewLine, lines);
    }

    private string BuildRoyaleShareText()
    {
        if (_royaleRun is null)
            return "Words Royale";

        return RoyaleResultFormatter.BuildShareText(_royaleRun, "You");
    }

    private string BuildMegaBoardShareText()
    {
        if (_megaBoardRun is null)
            return "Words Mega Board";

        var status = _megaBoardRun.IsPerfectClear ? "PERFECT CLEAR" : $"{_megaBoardRun.SolvedCount}/{_megaBoardRun.BoardCount} cleared";
        return string.Join(Environment.NewLine,
            $"Words Mega Board {_megaBoardRun.BoardCount}x{_session.WordLength} - {status}",
            $"Score: {_megaBoardRun.CalculateScore()}",
            $"Lost: {_megaBoardRun.LostCount} | Danger: {_megaBoardRun.DangerCount}",
            $"Focus pattern: {_megaBoardRun.BuildKnownPattern(_megaBoardRun.FocusBoard)}",
            BuildMegaBoardDangerReport());
    }

    private static string GetShareEmoji(WordleTileState state) => state switch
    {
        WordleTileState.Correct => "🟩",
        WordleTileState.Present => "🟨",
        _ => "⬛"
    };

    private void RecordRound(WordMetadata metadata)
    {
        if (_roundRecorded)
            return;

        if (_activeMode == WordleGameMode.MegaBoard && _megaBoardRun is not null)
        {
            _stats.RecordMegaBoardRun(_megaBoardRun);
            _stats.Save(StatsPath);
            _roundRecorded = true;
            return;
        }

        _stats.RecordRound(_session, _activeMode, metadata);
        if (_activeMode == WordleGameMode.Survival)
        {
            if (_session.Status == GameStatus.Won)
                _survivalStreak++;

            _stats.RecordSurvivalRun(_survivalStreak);
        }
        if (_activeMode == WordleGameMode.Timed && _timeAttackRun is not null && _timeAttackRun.IsOver)
            _stats.RecordTimeAttackRun(_timeAttackRun);
        if (_activeMode == WordleGameMode.WordsRoyale && _royaleRun is not null)
            _stats.RecordRoyaleRun(_royaleRun, "You");
        _stats.Save(StatsPath);
        _roundRecorded = true;
    }

    private void RefreshStats()
    {
        var winRate = _stats.GamesPlayed == 0 ? 0 : (int)Math.Round((double)_stats.Wins / _stats.GamesPlayed * 100);
        var raritySummary = _stats.DiscoveredByRarity.Count == 0
            ? "no rarity data yet"
            : string.Join(", ", _stats.DiscoveredByRarity.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}: {pair.Value}"));
        var selectedLength = GetSelectedWordLength();
        var discoveredForLength = _stats.DiscoveredWords.Count(word => word.Length == selectedLength);
        var totalForLength = _wordleService.GetWordsByLength(selectedLength).Count;
        var metadataCoverage = _wordleService.GetMetadataCoverage(selectedLength);
        var collectionPercent = totalForLength == 0 ? 0 : discoveredForLength * 100.0 / totalForLength;
        var guessDistribution = _stats.WinsByAttemptCount.Count == 0
            ? "no wins yet"
            : string.Join(", ", _stats.WinsByAttemptCount.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}: {pair.Value}"));
        var topStats = BuildTopStatsText();
        var timeAttackText = $"Time Attack runs {_stats.TimeAttackRuns}, best score {_stats.BestTimeAttackScore}, solved {_stats.BestTimeAttackSolved}, combo x{_stats.BestTimeAttackCombo}";
        var royaleText = $"Royale runs {_stats.RoyaleRuns}, best {BuildRoyalePlacementText()}, wins {_stats.RoyaleWins}, top-10s {_stats.RoyaleTopTenFinishes}, best solved {_stats.BestRoyaleSolved}";
        var megaText = $"Mega Board runs {_stats.MegaBoardRuns}, best {_stats.BestMegaBoardClears} clears, best score {_stats.BestMegaBoardScore}, perfect {_stats.MegaBoardPerfectClears}, 100-board clears {_stats.MegaBoardCenturionClears}";
        var powerUps = BuildPowerUpInventoryText();
        _statsSummary.Text =
            $"Stats: {_stats.GamesPlayed} played, {_stats.Wins} wins, {_stats.Losses} losses, {winRate}% win rate, score {_stats.TotalScore}, best round {_stats.BestRoundScore}, perfect {_stats.PerfectGames}, daily wins {_stats.DailyWins.Count}, streak {_stats.CurrentStreak}, best {_stats.BestStreak}, survival best {_stats.BestSurvivalStreak}, {timeAttackText}, {royaleText}, {megaText}, power-ups [{powerUps}], distribution [{guessDistribution}], {_stats.DiscoveredWords.Count} words discovered ({discoveredForLength}/{totalForLength} length-{selectedLength}, {collectionPercent:0.00}%; metadata {metadataCoverage.TaggedWords}/{metadataCoverage.TotalWords}, {metadataCoverage.TaggedPercent:0.00}% tagged; {raritySummary}). {topStats}";
        _badges.Text = BuildBadgeText();

        _recentWords.BeginUpdate();
        _recentWords.Items.Clear();
        foreach (var word in _stats.RecentWords.Take(12))
        {
            var result = word.Won ? "Win" : "Loss";
            _recentWords.Items.Add($"{word.Text} ({word.Length}) - {word.Rarity} - {word.Mode} - {result} in {word.AttemptsUsed} - {word.Score} pts");
        }
        _recentWords.EndUpdate();
        RefreshPowerUpSelector();
    }

    private void RefreshPowerUpSelector()
    {
        var selected = _powerUpSelector.SelectedItem is PowerUpOption option ? option.Type : (PowerUpType?)null;
        _powerUpSelector.Items.Clear();
        foreach (var pair in _stats.PowerUps.OrderBy(pair => pair.Key))
        {
            if (!Enum.TryParse<PowerUpType>(pair.Key, out var type) || pair.Value <= 0)
                continue;

            _powerUpSelector.Items.Add(new PowerUpOption(type, pair.Value));
        }

        _powerUpSelector.Enabled = _powerUpSelector.Items.Count > 0 && _session.Status == GameStatus.InProgress;
        _usePowerUp.Enabled = _powerUpSelector.Enabled;
        _powerUpSelector.SelectedItem = _powerUpSelector.Items
            .OfType<PowerUpOption>()
            .FirstOrDefault(option => option.Type == selected)
            ?? (_powerUpSelector.Items.Count > 0 ? _powerUpSelector.Items[0] : null);
    }

    private void UseSelectedPowerUp()
    {
        if (_session.Status != GameStatus.InProgress || _powerUpSelector.SelectedItem is not PowerUpOption option)
            return;
        if (!_stats.TrySpendPowerUp(option.Type))
        {
            RefreshPowerUpSelector();
            return;
        }

        var result = option.Type switch
        {
            PowerUpType.Bomb => UseBombPowerUp(),
            PowerUpType.PingPongPaddle => UsePingPongPaddlePowerUp(),
            PowerUpType.FirstLetter => UseRevealPositionPowerUp(PowerUpEffectService.UseFirstLetter(_session.Answer)),
            PowerUpType.Reveal => UseRevealPositionPowerUp(UseNextRevealPowerUp()),
            PowerUpType.Clock => ArmClockPowerUp(),
            PowerUpType.Shield => ArmShieldPowerUp(),
            PowerUpType.Crown => ArmCrownPowerUp(),
            PowerUpType.Freeze => ArmFreezePowerUp(),
            PowerUpType.Cleanse => CleansePowerUps(),
            PowerUpType.Reroll => UseRerollPowerUp(),
            _ => new PowerUpUseResult(option.Type, $"{option.Type} is not wired yet.", [])
        };

        PlaySfx("powerup-use");
        _roundSummary.Text = result.Message;
        _stats.Save(StatsPath);
        RenderMegaBoardDashboard();
        RefreshStats();
    }

    private PowerUpUseResult UseBombPowerUp()
    {
        if (_activeMode == WordleGameMode.WordsRoyale && _royaleRun is not null)
            return UseRoyaleBombPowerUp();
        if (_activeMode == WordleGameMode.MegaBoard && _megaBoardRun is not null)
            return UseMegaBoardBombPowerUp();

        var result = PowerUpEffectService.UseBomb(_session.Answer, _keyboardState.Letters.Keys);
        foreach (var letter in result.Letters)
        {
            if (_keyboardButtons.TryGetValue(letter, out var button))
            {
                button.Enabled = false;
                button.BackColor = Color.DarkSlateGray;
                button.ForeColor = Color.White;
            }
        }

        return result;
    }

    private PowerUpUseResult UseRoyaleBombPowerUp()
    {
        if (_royaleRun is null)
            return UseBombPowerUp();

        var targets = _royaleRun.SabotageLeaders("You", count: 5, penaltySeconds: 10);
        var targetText = targets.Count == 0
            ? "no rival leaders"
            : string.Join(", ", targets.Select(target => target.Name));
        return new PowerUpUseResult(
            PowerUpType.Bomb,
            $"Royale Bomb hit {targetText} for -10s each. {BuildRoyaleLiveSummary()}",
            []);
    }

    private PowerUpUseResult UseMegaBoardBombPowerUp()
    {
        if (_megaBoardRun is null)
            return UseBombPowerUp();

        var guessed = _keyboardState.Letters.Keys.Select(char.ToUpperInvariant).ToHashSet();
        var activeAnswerLetters = _megaBoardRun.Boards
            .Where(board => board.Status == GameStatus.InProgress)
            .SelectMany(board => board.Answer)
            .Select(char.ToUpperInvariant)
            .ToHashSet();
        var removed = Enumerable.Range('A', 26)
            .Select(value => (char)value)
            .Where(letter => !activeAnswerLetters.Contains(letter) && !guessed.Contains(letter))
            .Take(5)
            .ToArray();

        foreach (var letter in removed)
        {
            if (_keyboardButtons.TryGetValue(letter, out var button))
            {
                button.Enabled = false;
                button.BackColor = Color.DarkSlateGray;
                button.ForeColor = Color.White;
            }
        }

        return new PowerUpUseResult(
            PowerUpType.Bomb,
            removed.Length == 0
                ? "Mega Bomb found no letters safe across every active board."
                : $"Mega Bomb removed letters safe across all active boards: {string.Join(", ", removed)}.",
            removed);
    }

    private PowerUpUseResult UsePingPongPaddlePowerUp()
    {
        var vowel = GetSelectedPingVowel();
        if (_activeMode != WordleGameMode.MegaBoard || _megaBoardRun is null)
            return PowerUpEffectService.UsePingPongPaddle(_session.Answer, vowel);

        var matches = _megaBoardRun.Boards.Count(board =>
            board.Status == GameStatus.InProgress && board.Answer.Contains(vowel));
        return new PowerUpUseResult(
            PowerUpType.PingPongPaddle,
            $"{vowel} appears on {matches}/{_megaBoardRun.Boards.Count(board => board.Status == GameStatus.InProgress)} active Mega Boards.",
            [vowel],
            Letter: vowel);
    }

    private PowerUpUseResult UseRevealPositionPowerUp(PowerUpUseResult result)
    {
        if (result.Position is int position)
        {
            _revealedPositions.Add(position);
            RenderRevealedPowerUpClues();
        }

        return result;
    }

    private PowerUpUseResult UseNextRevealPowerUp()
    {
        var normalized = _session.Answer.ToUpperInvariant();
        var position = Enumerable.Range(0, normalized.Length)
            .FirstOrDefault(index => !_revealedPositions.Contains(index));
        return UseRevealPositionPowerUp(new PowerUpUseResult(
            PowerUpType.Reveal,
            $"Reveal: position {position + 1} is {normalized[position]}.",
            [normalized[position]],
            Position: position,
            Letter: normalized[position]));
    }

    private PowerUpUseResult ArmClockPowerUp()
    {
        var result = PowerUpEffectService.UseClock();
        _armedClockBonusSeconds += result.TimeDeltaSeconds;
        return result with { Message = $"Clock armed: next Time Attack or Royale solve grants +{_armedClockBonusSeconds}s extra." };
    }

    private PowerUpUseResult ArmShieldPowerUp()
    {
        _shieldArmed = true;
        return PowerUpEffectService.UseShield() with { Message = "Shield armed: the next Time Attack or Royale fail/skip penalty is blocked." };
    }

    private PowerUpUseResult ArmCrownPowerUp()
    {
        _crownArmed = true;
        return PowerUpEffectService.UseCrown() with { Message = "Crown armed: in Royale, lethal timer drain revives you with 10 seconds." };
    }

    private PowerUpUseResult ArmFreezePowerUp()
    {
        if (_activeMode == WordleGameMode.WordsRoyale)
        {
            _royaleBotFreezeTicksRemaining += 10;
            return new PowerUpUseResult(PowerUpType.Freeze, $"Royale Freeze active: bots stop solving/failing for {_royaleBotFreezeTicksRemaining}s while your timer keeps draining.", []);
        }

        _freezeTicksRemaining += 10;
        return new PowerUpUseResult(PowerUpType.Freeze, $"Freeze active: timers pause for {_freezeTicksRemaining}s.", []);
    }

    private PowerUpUseResult CleansePowerUps()
    {
        _shieldArmed = false;
        _crownArmed = false;
        _armedClockBonusSeconds = 0;
        _freezeTicksRemaining = 0;
        _royaleBotFreezeTicksRemaining = 0;
        return new PowerUpUseResult(PowerUpType.Cleanse, "Cleanse used: all armed boosts/debuff slots are clear.", []);
    }

    private PowerUpUseResult UseRerollPowerUp()
    {
        PlaySfx("reroll");
        if (_activeMode == WordleGameMode.MegaBoard && _megaBoardRun is not null)
        {
            var oldBoard = _session;
            var replacement = _wordleService.StartSession(WordleGameMode.MegaBoard, oldBoard.WordLength, oldBoard.MaxAttempts);
            _megaBoardRun.ReplaceBoard(oldBoard, replacement);
            _session = replacement;
            _revealedPositions.Clear();
            ResetBoard();
            return new PowerUpUseResult(PowerUpType.Reroll, $"Reroll used: Mega Board #{GetMegaBoardFocusIndex() + 1} swapped to a fresh word.", []);
        }

        _session = StartConfiguredSession();
        _revealedPositions.Clear();
        ResetBoard();
        return new PowerUpUseResult(PowerUpType.Reroll, "Reroll used: new word loaded.", []);
    }

    private char GetSelectedPingVowel() =>
        _vowelSelector.SelectedItem is char vowel ? vowel : 'A';

    private string BuildTopStatsText()
    {
        var topMode = _stats.WinsByMode
            .OrderByDescending(pair => pair.Value)
            .FirstOrDefault();
        var topLength = _stats.WinsByLength
            .OrderByDescending(pair => pair.Value)
            .FirstOrDefault();

        var modeText = topMode.Key is null ? "none" : $"{topMode.Key} ({topMode.Value})";
        var lengthText = topLength.Key == 0 ? "none" : $"{topLength.Key} letters ({topLength.Value})";
        return $"Favorites: mode {modeText}; length {lengthText}.";
    }

    private string BuildBadgeText()
    {
        var badges = new List<string>();
        if (_stats.GamesPlayed >= 1)
            badges.Add("First Steps");
        if (_stats.Wins >= 1)
            badges.Add("First Win");
        if (_stats.BestStreak >= 3)
            badges.Add("Hot Streak");
        if (_stats.BestSurvivalStreak >= 3)
            badges.Add("Survivor");
        if (_stats.DiscoveredWords.Count >= 10)
            badges.Add("Word Collector");
        if (_stats.DiscoveredByRarity.GetValueOrDefault("Wild Dictionary") >= 1)
            badges.Add("Dictionary Goblin");
        if (_stats.WinsByLength.Keys.Any(length => length >= 10))
            badges.Add("Long Word Slayer");
        if (_stats.WinsByMode.ContainsKey(WordleGameMode.Timed.ToString()))
            badges.Add("Beat the Clock");
        if (_stats.BestTimeAttackCombo >= 5)
            badges.Add("Combo Runner");
        if (_stats.BestTimeAttackCombo >= 10)
            badges.Add("Time Attack Inferno");
        if (_stats.BestTimeAttackScore >= 10000)
            badges.Add("Speed Scorer");
        if (_stats.WinsByMode.ContainsKey(WordleGameMode.Ghost.ToString()))
            badges.Add("Ghostbuster");
        if (_stats.RoyaleWins >= 1)
            badges.Add("Royale Victor");
        if (_stats.BestRoyalePlacement is > 1 and <= 10)
            badges.Add("Royale Finalist");
        if (_stats.RoyaleTopTenFinishes >= 3)
            badges.Add("Final Table Regular");
        if (_stats.BestRoyaleSolved >= 10)
            badges.Add("Royale Wordstorm");
        if (_stats.BestMegaBoardClears >= 25)
            badges.Add("Mega Mind");
        if (_stats.BestMegaBoardClears >= 100)
            badges.Add("Centurion");
        if (_stats.MegaBoardPerfectClears >= 1)
            badges.Add("Perfect Multiverse");
        if (_stats.WinsByMode.ContainsKey(WordleGameMode.RareHunt.ToString()))
            badges.Add("Rare Hunter");
        if (_stats.WinsByMode.ContainsKey(WordleGameMode.Discovery.ToString()))
            badges.Add("Explorer");
        if (_stats.DailyWins.Count >= 1)
            badges.Add("Daily Done");
        if (_stats.DailyWins.Count >= 7)
            badges.Add("Week of Words");
        if (_stats.TotalScore >= 10000)
            badges.Add("Point Hoarder");
        if (_stats.BestRoundScore >= 2500)
            badges.Add("High Roller");
        if (_stats.PerfectGames >= 1)
            badges.Add("Oracle");

        return badges.Count == 0
            ? "Badges: none yet"
            : $"Badges: {string.Join(", ", badges)}";
    }

    private string BuildRoyalePlacementText() =>
        _stats.BestRoyalePlacement == 0 ? "none" : $"#{_stats.BestRoyalePlacement}";

    private string BuildPowerUpInventoryText() =>
        _stats.PowerUps.Count == 0
            ? "none"
            : string.Join(", ", _stats.PowerUps.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}: {pair.Value}"));

    private void UpdateModeHelp()
    {
        var mode = GetSelectedMode();
        var coverage = _wordleService.GetModeCoverage(mode, GetSelectedWordLength());
        var help = mode switch
        {
            WordleGameMode.Classic => "Classic: safer answer pool; full dictionary still allowed for guesses.",
            WordleGameMode.Hard => "Hard: safer answer pool, and guesses must reuse revealed letters.",
            WordleGameMode.Daily => "Daily: deterministic answer for today's date and selected word length.",
            WordleGameMode.Alphabet => "Alphabet: solve A through Z. The first letter is free, so you get one fewer guess.",
            WordleGameMode.Timed => $"Time Attack: 3 minutes, solve words to gain +{TimeAttackRun.DefaultSolveBonusSeconds}s, fail/skip to lose -{TimeAttackRun.DefaultFailurePenaltySeconds}s.",
            WordleGameMode.Ghost => "Ghost: feedback only tells you how many letters are exact and how many are present somewhere.",
            WordleGameMode.VsCpu => $"Vs. CPU: race to {GetSelectedCpuDifficulty().TargetWords} words on {_cpuDifficultySelector.SelectedItem}. Persona {_cpuRaceRun?.CpuPersona ?? GetSelectedCpuDifficulty().CpuPersona}.",
            WordleGameMode.WordsRoyale => $"Words Royale: 99 enter, bot-filled. Solves add +{RoyaleRun.DefaultSolveBonusSeconds}s; fail/skip costs -{RoyaleRun.DefaultFailurePenaltySeconds}s. Bomb sabotages leaders; Freeze stalls bot actions.",
            WordleGameMode.MegaBoard => $"Mega Board: one guess hits {GetSelectedMegaBoardCount()} boards at once. Yes, 100 boards and 20-letter words are allowed.",
            WordleGameMode.Survival => "Survival: win to continue; word length ramps as your streak grows.",
            WordleGameMode.RareHunt => "Rare Hunt: intentionally targets rare and wild dictionary answers.",
            WordleGameMode.Discovery => "Discovery: prioritizes answer words you have not discovered yet.",
            _ => "Wild Dictionary: the full giant word bank can be the answer."
        };
        var pitchHint = "Use Pitch Setup for a fast showcase baseline.";
        _modeHelp.Text = $"{help} {pitchHint} Answer pool: {coverage.CandidateCount}/{coverage.TotalWords} words ({coverage.CandidatePercent:0.00}%).";
    }

    private WordleSession StartConfiguredSession()
    {
        _activeMode = GetSelectedMode();
        var wordLength = _activeMode == WordleGameMode.Survival
            ? GetSurvivalWordLength(_survivalStreak)
            : GetSelectedWordLength();

        SaveDemoSettings(_activeMode, wordLength);

        if (_activeMode == WordleGameMode.Discovery)
            return StartDiscoverySession(wordLength);
        if (_activeMode == WordleGameMode.Alphabet)
            return _wordleService.StartAlphabetSession(GetAlphabetLetter(), wordLength, Math.Max(1, GetAttemptCount(wordLength) - 1));
        if (_activeMode == WordleGameMode.MegaBoard)
        {
            _megaBoardRun ??= _wordleService.StartMegaBoardRun(boardCount: GetSelectedMegaBoardCount(), wordLength: wordLength, maxAttempts: GetMegaBoardAttemptCount(wordLength));
            return _megaBoardRun.FocusBoard;
        }

        return _wordleService.StartSession(_activeMode, wordLength, GetAttemptCount(wordLength));
    }

    private WordleSession StartDiscoverySession(int wordLength)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var session = _wordleService.StartSession(WordleGameMode.Discovery, wordLength, GetAttemptCount(wordLength));
            if (!_stats.DiscoveredWords.Contains(session.Answer, StringComparer.OrdinalIgnoreCase))
                return session;
        }

        return _wordleService.StartSession(WordleGameMode.Discovery, wordLength, GetAttemptCount(wordLength));
    }

    private WordleGameMode GetSelectedMode() =>
        _modeSelector.SelectedItem is ModeOption option
            ? option.Mode
            : WordleGameMode.Classic;

    private int GetSelectedWordLength() =>
        _wordLengthSelector.SelectedItem is int length ? length : 5;

    private int GetSelectedMegaBoardCount() =>
        _megaBoardCountSelector.SelectedItem is int count ? count : 100;

    private CpuDifficultyOption GetSelectedCpuDifficulty() =>
        _cpuDifficultySelector.SelectedItem as CpuDifficultyOption
        ?? new CpuDifficultyOption("Standard", TargetWords: 5, CpuSecondsPerWord: 18, CpuPersona: "Sprinter");

    private string GetSelectedMegaBoardFilter() =>
        _megaBoardFilterSelector.SelectedItem as string ?? "All";

    private string GetSelectedMegaBoardSort() =>
        _megaBoardSortSelector.SelectedItem as string ?? "Smart";

    private void RestoreDemoSettings()
    {
        var mode = Enum.TryParse<WordleGameMode>(_stats.LastMode, out var savedMode)
            ? savedMode
            : WordleGameMode.Classic;
        _modeSelector.SelectedItem = _modeSelector.Items
            .OfType<ModeOption>()
            .FirstOrDefault(option => option.Mode == mode)
            ?? _modeSelector.Items[0];

        _wordLengthSelector.SelectedItem = Math.Clamp(_stats.LastWordLength, 4, 20);
        _cpuDifficultySelector.SelectedItem = _cpuDifficultySelector.Items.OfType<CpuDifficultyOption>().FirstOrDefault(option => option.Label == "Standard") ?? _cpuDifficultySelector.Items[0];
        _megaBoardCountSelector.SelectedItem = 100;
        _megaBoardFilterSelector.SelectedItem = "All";
        _megaBoardSortSelector.SelectedItem = "Smart";
        UpdateMegaBoardControls();
        UpdateCpuRaceControls();
    }

    private void SaveDemoSettings(WordleGameMode mode, int wordLength)
    {
        _stats.LastMode = mode.ToString();
        _stats.LastWordLength = wordLength;
        _stats.Save(StatsPath);
    }

    private static int GetAttemptCount(int wordLength) => wordLength switch
    {
        <= 5 => 6,
        <= 7 => 7,
        <= 10 => 8,
        <= 14 => 10,
        _ => 12
    };

    private static int GetMegaBoardAttemptCount(int wordLength) => GetAttemptCount(wordLength) + wordLength switch
    {
        <= 5 => 4,
        <= 10 => 6,
        <= 14 => 8,
        _ => 10
    };

    private static int GetSurvivalWordLength(int streak) => Math.Min(20, 4 + (streak / 2));

    private static int GetTimedSeconds(int wordLength) => wordLength switch
    {
        <= 5 => 90,
        <= 7 => 120,
        <= 10 => 150,
        <= 14 => 210,
        _ => 300
    };

    private string GetPromptText()
    {
        var text = $"Enter a {_session.WordLength}-letter word. {_session.RemainingAttempts} tries left.";
        if (_activeMode == WordleGameMode.Survival)
            text = $"Survival streak {_survivalStreak}. " + text;
        if (_activeMode == WordleGameMode.Alphabet)
            text = $"Alphabet {GetAlphabetLetter()} ({_alphabetIndex + 1}/26). First letter is free. " + text;
        if (_activeMode == WordleGameMode.Timed && _timeAttackRun is not null)
            text = $"Time Attack: {_timeAttackRun.WordsSolved} solved, {_timeAttackRun.WordsFailed} failed, combo x{_timeAttackRun.CurrentCombo}, best x{_timeAttackRun.BestCombo}, score {_timeAttackRun.Score}. " + text + $" Time: {_secondsRemaining / 60}:{_secondsRemaining % 60:00}.";
        if (_activeMode == WordleGameMode.VsCpu && _cpuRaceRun is not null)
            text = $"Vs. CPU {_cpuDifficultySelector.SelectedItem} ({_cpuRaceRun.CpuPersona}) to {_cpuRaceRun.TargetWords}: You {_cpuRaceRun.PlayerSolved}, CPU {_cpuRaceRun.CpuSolved}. " + text;
        if (_activeMode == WordleGameMode.WordsRoyale && _royaleRun is not null)
        {
            var you = GetRoyalePlayer();
            var freezeText = _royaleBotFreezeTicksRemaining > 0 ? $" Bot freeze {_royaleBotFreezeTicksRemaining}s." : string.Empty;
            text = $"WORDS ROYALE {_royaleRun.Phase}: rank #{_royaleRun.GetRank("You")}/{Math.Max(1, _royaleRun.AliveCount)}, {_royaleRun.GetEliminationZoneCount()} in elimination zone. You: {you.SecondsRemaining}s, {you.WordsSolved} solved.{freezeText} " + text;
        }
        if (_activeMode == WordleGameMode.MegaBoard && _megaBoardRun is not null)
            text = BuildMegaBoardLiveSummary() + " " + text;
        return text;
    }

    private void SetLiveStatus()
    {
        _status.Text = GetPromptText();
        if (_activeMode == WordleGameMode.VsCpu && _cpuRaceRun is not null)
        {
            _cpuRaceSummary.Text = BuildCpuRaceLiveSummary();
            var raceColor = GetCpuRaceColor();
            _cpuRaceSummary.ForeColor = raceColor;
            _status.ForeColor = raceColor;
            return;
        }
        if (_activeMode != WordleGameMode.WordsRoyale || _royaleRun is null)
        {
            _status.ForeColor = Color.DimGray;
            _roundSummary.ForeColor = Color.DimGray;
            return;
        }

        var phaseColor = GetRoyalePhaseColor(_royaleRun.Phase);
        _status.ForeColor = phaseColor;
        _roundSummary.ForeColor = phaseColor;
    }

    private static Color GetRoyalePhaseColor(string phase) => phase switch
    {
        "Victory" => Color.SeaGreen,
        "Duel" => Color.DarkRed,
        "Final 10" => Color.OrangeRed,
        "Bubble" => Color.DarkOrange,
        "Midgame" => Color.RoyalBlue,
        _ => Color.DimGray
    };

    private static Color GetRoyalePlacementColor(int placement) => placement switch
    {
        1 => Color.SeaGreen,
        <= 3 => Color.Goldenrod,
        <= 10 => Color.DarkOrange,
        _ => Color.Firebrick
    };

    private static Color GetTimeAttackCeremonyColor(TimeAttackRun run)
    {
        if (run.BestCombo >= 10 || run.Score >= 10000)
            return Color.OrangeRed;
        if (run.BestCombo >= 5 || run.WordsSolved >= 8)
            return Color.DarkOrange;
        if (run.WordsSolved > 0)
            return Color.RoyalBlue;
        return Color.DimGray;
    }

    private Color GetCpuRaceColor()
    {
        if (_cpuRaceRun is null)
            return Color.DimGray;

        var gap = _cpuRaceRun.PlayerSolved - _cpuRaceRun.CpuSolved;
        if (_cpuRaceRun.IsComplete)
            return _cpuRaceRun.Winner == "Player" ? Color.SeaGreen : Color.Firebrick;
        if (gap >= 2)
            return Color.SeaGreen;
        if (gap <= -2)
            return Color.Firebrick;
        return Color.RoyalBlue;
    }

    private string BuildCpuRaceLiveSummary()
    {
        var profile = GetSelectedCpuDifficulty();
        if (_cpuRaceRun is null)
            return $"Vs. CPU {profile.Label}: target {profile.TargetWords}, persona {profile.CpuPersona}, pace {profile.CpuSecondsPerWord}s/word.";

        var gap = _cpuRaceRun.PlayerSolved - _cpuRaceRun.CpuSolved;
        var pressure = gap switch
        {
            >= 2 => "You are pulling ahead.",
            <= -2 => "CPU pressure rising.",
            _ => "Neck-and-neck."
        };
        return $"Vs. CPU {profile.Label} ({_cpuRaceRun.CpuPersona}) | Target {_cpuRaceRun.TargetWords} | You {_cpuRaceRun.PlayerSolved} - CPU {_cpuRaceRun.CpuSolved} | Gap {gap:+#;-#;0} | {pressure}";
    }

    private void TickTimer()
    {
        if (_session.Status != GameStatus.InProgress)
        {
            _timer.Stop();
            return;
        }

        if (_freezeTicksRemaining > 0)
        {
            _freezeTicksRemaining--;
            _status.Text = $"{GetPromptText()} Freeze: {_freezeTicksRemaining}s.";
            return;
        }

        if (_activeMode == WordleGameMode.WordsRoyale && _royaleRun is not null)
        {
            _royaleRun.Tick();
            TickRoyaleBots();
            var you = GetRoyalePlayer();
            if (you.IsEliminated)
            {
                if (_crownArmed)
                {
                    _crownArmed = false;
                    _royaleRun.AddTime("You", 10);
                    _roundSummary.Text = "Crown saved you from elimination. +10s.";
                    SetLiveStatus();
                    return;
                }

                _session.Expire();
                EndGame($"Eliminated from Words Royale. Placement: #{_royaleRun.GetPlacement("You")}. Last word: {_session.Answer}.");
                return;
            }
            if (_royaleRun.IsComplete)
            {
                EndGame($"WORDS ROYALE VICTORY. {_royaleRun.Winner?.Name} is the last wordsmith standing.");
                return;
            }
        }
        else if (_activeMode == WordleGameMode.VsCpu && _cpuRaceRun is not null)
        {
            _cpuRaceRun.Tick();
            if (_cpuRaceRun.IsComplete)
            {
                _session.Expire();
                EndGame($"CPU wins the race {_cpuRaceRun.CpuSolved}-{_cpuRaceRun.PlayerSolved}. The word was {_session.Answer}.");
                return;
            }
        }
        else if (_activeMode == WordleGameMode.Timed && _timeAttackRun is not null)
        {
            _timeAttackRun.Tick();
            _secondsRemaining = _timeAttackRun.SecondsRemaining;
            if (_secondsRemaining > 0
                && _secondsRemaining <= 10
                && _secondsRemaining != _lastLowTimeCueSecond
                && (_secondsRemaining == 10 || _secondsRemaining == 5 || _secondsRemaining <= 3))
            {
                PlaySfx("timer-warning");
                _lastLowTimeCueSecond = _secondsRemaining;
            }
        }
        else
        {
            _secondsRemaining--;
        }

        if (_activeMode != WordleGameMode.WordsRoyale
            && _activeMode != WordleGameMode.VsCpu
            && _secondsRemaining <= 0)
        {
            _session.Expire();
            EndGame(_timeAttackRun is null
                ? $"Time's up. The word was {_session.Answer}."
                : $"Time Attack over. {TimeAttackResultFormatter.BuildSummaryText(_timeAttackRun)} Last word: {_session.Answer}.");
            return;
        }

        SetLiveStatus();
    }

    private void ContinueRoyaleAfterSolved()
    {
        _royaleRun ??= new RoyaleRun(["You"]);
        _royaleRun.RecordSolved("You");
        AddRoyaleEvent($"You solved! +{RoyaleRun.DefaultSolveBonusSeconds}s");
        UpdateRoyaleRankPanel();
        PlaySfx("royale-survive");
        var clockMessage = ApplyClockBonusToRoyale();
        if (_royaleRun.IsComplete)
        {
            EndGame("WORDS ROYALE VICTORY. You are the last wordsmith standing.");
            return;
        }

        _session = StartConfiguredSession();
        _revealedPositions.Clear();
        ResetBoard();
        _roundSummary.Text = $"+{RoyaleRun.DefaultSolveBonusSeconds}s.{clockMessage} {BuildRoyaleLiveSummary()}";
        SetLiveStatus();
    }

    private void ContinueAlphabetAfterSolved()
    {
        if (_alphabetIndex >= 25)
        {
            EndGame("Alphabet complete. You solved A through Z!");
            return;
        }

        _alphabetIndex++;
        PlaySfx("streak-up");
        _session = StartConfiguredSession();
        _revealedPositions.Clear();
        ResetBoard();
        _roundSummary.Text = $"Alphabet advanced to {GetAlphabetLetter()}.";
    }

    private void ContinueRoyaleAfterFailed(string message)
    {
        _royaleRun ??= new RoyaleRun(["You"]);
        if (_shieldArmed)
        {
            _shieldArmed = false;
            PlaySfx("shield-block");
            message = $"Shield blocked the penalty for {_session.Answer}.";
            AddRoyaleEvent("You: Shield blocked penalty!");
        }
        else
        {
            _royaleRun.RecordFailed("You");
            AddRoyaleEvent($"You failed: -{RoyaleRun.DefaultFailurePenaltySeconds}s");
            PlaySfx("penalty-hit");
        }
        UpdateRoyaleRankPanel();
        var you = GetRoyalePlayer();
        if (you.IsEliminated)
        {
            AddRoyaleEvent("You have been eliminated!");
            EndGame($"Eliminated from Words Royale. {message} Placement: #{_royaleRun.GetPlacement("You")}.");
            return;
        }

        _session = StartConfiguredSession();
        _revealedPositions.Clear();
        ResetBoard();
        _roundSummary.Text = $"{message} {BuildRoyaleLiveSummary()}";
        SetLiveStatus();
    }

    private void TickRoyaleBots()
    {
        if (_royaleRun is null)
            return;

        _royaleBotPulse++;
        if (_royaleBotFreezeTicksRemaining > 0)
        {
            _royaleBotFreezeTicksRemaining--;
            return;
        }

        var aliveBots = _royaleRun.Entrants
            .Where(entrant => entrant.IsBot && !entrant.IsEliminated)
            .ToList();
        var initialEliminated = _royaleRun.Entrants.Count(e => e.IsEliminated);

        if (_royaleBotPulse % 7 == 0)
        {
            foreach (var bot in aliveBots.Where((bot, index) => RoyaleBotTactics.ShouldSolve(bot, index, _royaleBotPulse, _royaleRun.Phase)))
            {
                _royaleRun.RecordSolved(bot.Name);
                AddRoyaleEvent($"{bot.Name} solved! +{RoyaleRun.DefaultSolveBonusSeconds}s");
            }
        }

        if (_royaleBotPulse % 11 == 0)
        {
            foreach (var bot in aliveBots.Where((bot, index) => RoyaleBotTactics.ShouldFail(bot, index, _royaleBotPulse, _royaleRun.Phase)))
            {
                _royaleRun.RecordFailed(bot.Name);
                var botAfter = _royaleRun.Entrants.First(e => e.Name == bot.Name);
                if (botAfter.IsEliminated)
                    AddRoyaleEvent($"{bot.Name} eliminated!");
            }
        }

        // Update panels if eliminations occurred
        if (_royaleRun.Entrants.Count(e => e.IsEliminated) > initialEliminated || _royaleBotPulse % 5 == 0)
            UpdateRoyaleRankPanel();
    }

    private RoyaleEntrant GetRoyalePlayer() =>
        _royaleRun?.Entrants.First(entrant => entrant.Name == "You")
        ?? throw new InvalidOperationException("Words Royale has not started.");

    private int GetMegaBoardFocusIndex()
    {
        if (_megaBoardRun is null)
            return 0;

        for (var index = 0; index < _megaBoardRun.Boards.Count; index++)
        {
            if (ReferenceEquals(_megaBoardRun.Boards[index], _megaBoardRun.FocusBoard))
                return index;
        }

        return 0;
    }

    private int GetMegaBoardIndex(WordleSession board)
    {
        if (_megaBoardRun is null)
            return 0;

        for (var index = 0; index < _megaBoardRun.Boards.Count; index++)
        {
            if (ReferenceEquals(_megaBoardRun.Boards[index], board))
                return index;
        }

        return 0;
    }

    private char GetAlphabetLetter() => (char)('A' + Math.Clamp(_alphabetIndex, 0, 25));

    private void RenderAlphabetFreeLetter()
    {
        var letter = GetAlphabetLetter().ToString();
        for (var row = 0; row < _tiles.GetLength(0); row++)
        {
            var tile = _tiles[row, 0];
            tile.Text = letter;
            tile.BackColor = Color.SteelBlue;
            tile.ForeColor = Color.White;
        }
    }

    private void RenderRevealedPowerUpClues()
    {
        foreach (var position in _revealedPositions.Where(position => position >= 0 && position < _session.WordLength))
        {
            var letter = _session.Answer[position].ToString();
            for (var row = 0; row < _tiles.GetLength(0); row++)
            {
                var tile = _tiles[row, position];
                if (!string.IsNullOrEmpty(tile.Text) && tile.Text != letter)
                    continue;

                tile.Text = letter;
                tile.BackColor = Color.Goldenrod;
                tile.ForeColor = Color.White;
            }
        }
    }

    private string BuildRoyaleLiveSummary()
    {
        if (_royaleRun is null)
            return "Royale has not started.";

        return RoyaleResultFormatter.BuildLiveDramaText(_royaleRun, "You");
    }

    private string BuildRoyaleSummary()
    {
        if (_royaleRun is null)
            return string.Empty;

        return RoyaleResultFormatter.BuildCeremonyText(_royaleRun, "You");
    }

    private static string FormatRoyaleEntrant(RoyaleEntrant entrant) =>
        RoyaleResultFormatter.FormatEntrant(entrant);

    private void ContinueCpuRaceAfterSolved()
    {
        var profile = GetSelectedCpuDifficulty();
        _cpuRaceRun ??= new CpuRaceRun(targetWords: profile.TargetWords, cpuSecondsPerWord: profile.CpuSecondsPerWord, cpuPersona: profile.CpuPersona);
        _cpuRaceRun.RecordPlayerSolved();
        if (_cpuRaceRun.IsComplete)
        {
            EndGame($"You beat the CPU {_cpuRaceRun.PlayerSolved}-{_cpuRaceRun.CpuSolved}!");
            return;
        }

        PlaySfx("streak-up");
        _session = StartConfiguredSession();
        _revealedPositions.Clear();
        ResetBoard();
        _roundSummary.Text = $"Point scored. {BuildCpuRaceLiveSummary()}";
    }

    private void ContinueTimeAttackAfterSolved()
    {
        var metadata = _wordleService.GetWordMetadata(_session.Answer);
        var score = DemoStatsStore.CalculateRoundScore(_session, _activeMode, metadata);
        _timeAttackRun ??= new TimeAttackRun();
        _timeAttackRun.RecordSolved(score);
        if (_timeAttackRun.CurrentCombo >= 3)
            PlaySfx("streak-up");
        var clockMessage = ApplyClockBonusToTimeAttack();
        _session = StartConfiguredSession();
        _revealedPositions.Clear();
        ResetBoard();
        var bonusText = _timeAttackRun.LastComboBonus > 0 ? $" combo bonus +{_timeAttackRun.LastComboBonus}" : " no combo bonus";
        _roundSummary.Text = $"+{TimeAttackRun.DefaultSolveBonusSeconds}s.{clockMessage} Combo x{_timeAttackRun.CurrentCombo}; +{_timeAttackRun.LastScoreAwarded} pts ({bonusText}); score {_timeAttackRun.Score}.";
    }

    private void ContinueTimeAttackAfterFailed(string message)
    {
        _timeAttackRun ??= new TimeAttackRun();
        if (_shieldArmed)
        {
            _shieldArmed = false;
            PlaySfx("shield-block");
            message = $"Shield blocked the penalty for {_session.Answer}.";
        }
        else
        {
            _timeAttackRun.RecordFailed();
            PlaySfx("penalty-hit");
        }
        if (_timeAttackRun.IsOver)
        {
            EndGame($"Time Attack over. {message} {TimeAttackResultFormatter.BuildSummaryText(_timeAttackRun)}");
            return;
        }

        _session = StartConfiguredSession();
        _revealedPositions.Clear();
        ResetBoard();
        _roundSummary.Text = message;
    }

    private string ApplyClockBonusToTimeAttack()
    {
        if (_armedClockBonusSeconds <= 0 || _timeAttackRun is null)
            return string.Empty;

        var bonus = _armedClockBonusSeconds;
        _armedClockBonusSeconds = 0;
        _timeAttackRun.AddSeconds(bonus);
        return $" Clock +{bonus}s.";
    }

    private string ApplyClockBonusToRoyale()
    {
        if (_armedClockBonusSeconds <= 0 || _royaleRun is null)
            return string.Empty;

        var bonus = _armedClockBonusSeconds;
        _armedClockBonusSeconds = 0;
        _royaleRun.AddTime("You", bonus);
        return $" Clock +{bonus}s.";
    }

    private void BuildBoard(int rows, int columns)
    {
        _board.Controls.Clear();
        _board.ColumnStyles.Clear();
        _board.RowStyles.Clear();
        _board.ColumnCount = columns;
        _board.RowCount = rows;
        _tiles = new Label[rows, columns];

        // 4K optimized tile sizing - scales from 80px (5-letter) to 36px (20-letter)
        var tileSize = columns switch
        {
            <= 5 => 80,   // Optimal for 5-letter words on 4K
            6 => 72,
            7 => 64,
            8 => 58,
            9 => 54,
            10 => 50,
            11 => 46,
            12 => 44,
            13 => 42,
            14 => 40,
            15 => 38,
            16 => 38,
            17 => 37,
            18 => 37,
            19 => 36,
            _ => 36       // Minimum for 20-letter words
        };
        var fontSize = columns switch
        {
            <= 5 => 28,
            <= 7 => 24,
            <= 10 => 20,
            <= 14 => 16,
            <= 17 => 14,
            _ => 12
        };
        var tileMargin = columns <= 10 ? 8 : columns <= 14 ? 6 : 4;

        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                var tile = new Label
                {
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Width = tileSize,
                    Height = tileSize + (columns <= 10 ? 10 : 8),
                    Font = new Font("Segoe UI", fontSize, FontStyle.Bold),
                    BackColor = TileBaseColor,
                    ForeColor = TileTextColor,
                    BorderStyle = BorderStyle.FixedSingle,
                    Margin = new Padding(tileMargin)
                };
                _tiles[row, column] = tile;
                _board.Controls.Add(tile, column, row);
            }
        }
    }

    private static Color GetTileColor(WordleTileState state) => state switch
    {
        WordleTileState.Correct => Color.SeaGreen,
        WordleTileState.Present => Color.Goldenrod,
        _ => Color.DimGray
    };

    private static Color GetCommonalityColor(WordCommonality commonality) => commonality switch
    {
        WordCommonality.Common => Color.DarkGreen,
        WordCommonality.Uncommon => Color.DarkGoldenrod,
        WordCommonality.Rare => Color.DarkOrange,
        WordCommonality.Obscure => Color.DarkRed,
        _ => Color.Purple
    };

    private sealed record ModeOption(string Label, WordleGameMode Mode)
    {
        public override string ToString() => Label;
    }

    private sealed record CpuDifficultyOption(string Label, int TargetWords, int CpuSecondsPerWord, string CpuPersona)
    {
        public override string ToString() => Label;
    }

    private sealed record PowerUpOption(PowerUpType Type, int Count)
    {
        public override string ToString() => $"{Type} x{Count}";
    }
}
