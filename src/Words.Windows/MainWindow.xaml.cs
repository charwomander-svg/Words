using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using Words.Core.Models;
using Words.Core.Services;

namespace Words.Windows;

public partial class MainWindow : Window
{
    private const int DefaultWordLength = 5;
    private const double AlphabetSurfaceWidth = 1408;
    private const double AlphabetSurfaceHeight = 768;
    private const double AlphabetBoardX = 523;
    private const double AlphabetBoardY = 146;
    private const double AlphabetBoardSize = 360;
    private const double AlphabetKeyboardX = 292;
    private const double AlphabetKeyboardY = 615;
    private const double AlphabetKeyboardWidth = 820;
    private const double AlphabetKeyboardHeight = 130;
    private const double ClassicBoardX = 520;
    private const double ClassicBoardY = 168;
    private const double ClassicBoardWidth = 370;
    private const double ClassicBoardHeight = 438;
    private const double ClassicGuessX = 438;
    private const double ClassicGuessY = 118;
    private const double ClassicGuessWidth = 500;
    private const double ClassicStatsX = 100;
    private const double ClassicStatsY = 165;
    private const int AlphabetGuessRows = 5;
    private const int ClassicGuessRows = 6;
    private readonly WordService _wordService = WordService.FromEmbeddedResource();
    private string? _selectedMode;
    private ComboBox? _modeComboBox;
    private TextBlock? _modeDescriptionTextBlock;
    private TextBlock? _alphabetProgressTextBlock;
    private TextBlock? _alphabetTargetTextBlock;
    private TextBlock? _alphabetHintTextBlock;
    private TextBlock? _alphabetAttemptsTextBlock;
    private TextBlock? _alphabetKnownLettersTextBlock;
    private TextBlock? _alphabetEliminatedLettersTextBlock;
    private UniformGrid? _alphabetBoardGrid;
    private TextBox? _alphabetGuessTextBox;
    private Button? _alphabetSubmitButton;
    private string? _alphabetAnswer;
    private string _alphabetKnownLetters = string.Empty;
    private char _alphabetTargetLetter = 'A';
    private int _alphabetAttemptsLeft;
    private int _alphabetWordsCleared;
    private int _alphabetCurrentRow;
    private WordleRunMode _activeWordleRunMode = WordleRunMode.Alphabet;
    private bool _isSubmittingAlphabetGuess;
    private readonly List<Border> _alphabetTileBorders = [];
    private readonly List<TextBlock> _alphabetTileTextBlocks = [];
    private readonly HashSet<char> _alphabetEliminatedLetters = [];
    private readonly Dictionary<char, Border> _alphabetKeyboardKeyBorders = [];
    private readonly Dictionary<char, TextBlock> _alphabetKeyboardKeyTextBlocks = [];
    private readonly Dictionary<char, KeyboardLetterState> _alphabetKeyboardStates = [];
    private bool _isAlphabetFullscreen;
    private WindowStyle _savedWindowStyle;
    private WindowState _savedWindowState;
    private ResizeMode _savedResizeMode;
    private int _activeGuessRows = AlphabetGuessRows;

    private readonly Dictionary<string, string[]> _pageContent = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Multiplayer"] =
        [
            "Local Co-op, Local Vs, Head to Head, Showdown, 2v2",
            "Tournament and Words Royale via join codes.",
            "Lobby lifecycle: create, find, quick match, add guest, start, leave, disband, list."
        ],
        ["Leaderboards"] =
        [
            "Shows top-scoring players from the persisted score service.",
            "Demo uses isolated leaderboard data."
        ],
        ["Statistics"] =
        [
            "Tracks score, wins, mode usage, mode wins, and top mode.",
            "Tracks lobby counts, players, and matches started.",
            "Tracks daily progress and survival performance."
        ],
        ["Settings"] =
        [
            "Toggle hints and sound.",
            "Choose difficulty preference (Adaptive/Casual/Competitive).",
            "Choose visual theme (System/Light/Dark)."
        ],
        ["Achievements"] =
        [
            "Full unlocked/locked list rendered every time.",
            "Examples: Purist, Party Starter, Competitor, Daily Devotee, Iron Will."
        ],
        ["Credits"] =
        [
            "Words - Guess That Word",
            "Built by charwomander-svg."
        ]
    };

    public MainWindow()
    {
        InitializeComponent();
        RenderPage("Single Player");
    }

    private void MenuButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string page)
        {
            return;
        }

        RenderPage(page);
    }

    private async void RunDemo_Click(object sender, RoutedEventArgs e)
    {
        StatusTextBlock.Text = "Running visual screen-flow demo...";
        foreach (var page in _pageContent.Keys)
        {
            RenderPage(page);
            await Task.Delay(700);
        }

        StatusTextBlock.Text = "Demo complete. Use the left menu to revisit any screen.";
    }

    private void RenderPage(string page)
    {
        if (!string.Equals(page, "Single Player", StringComparison.OrdinalIgnoreCase))
        {
            ExitAlphabetFullscreen();
        }

        PageTitleTextBlock.Text = page;
        ContentStackPanel.Children.Clear();

        if (string.Equals(page, "Single Player", StringComparison.OrdinalIgnoreCase))
        {
            RenderSinglePlayerPage();
            StatusTextBlock.Text = "Single Player enabled.";
            return;
        }

        if (!_pageContent.TryGetValue(page, out var lines))
        {
            lines = ["No content configured for this page yet."];
        }

        foreach (var line in lines)
        {
            ContentStackPanel.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(12),
                Child = new TextBlock
                {
                    Text = line,
                    FontSize = 16,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.FromRgb(17, 24, 39))
                }
            });
        }

        StatusTextBlock.Text = $"Viewing: {page}";
    }

    private void RenderSinglePlayerPage()
    {
        ExitAlphabetFullscreen();

        ContentStackPanel.Children.Add(new TextBlock
        {
            Text = "Single Player Mode Hub",
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(17, 24, 39)),
            Margin = new Thickness(0, 0, 0, 12)
        });

        var configPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 12)
        };

        _modeComboBox = new ComboBox
        {
            Width = 260,
            Margin = new Thickness(0, 0, 12, 0),
            ItemsSource = new[]
            {
                "Choose mode...",
                "Classic",
                "Hard",
                "Daily",
                "Time Attack",
                "Ghost",
                "Speedrun",
                "Alphabet",
                "Vs. CPU",
                "Survival",
                "Word Hunt",
                "Wild Dictionary"
            },
            SelectedIndex = 0
        };
        _modeComboBox.SelectionChanged += (_, _) => UpdateModeDescription();

        var startButton = new Button
        {
            Content = "Open Mode Screen",
            Padding = new Thickness(12, 6, 12, 6)
        };
        startButton.Click += (_, _) => OpenSinglePlayerModeScreen();

        configPanel.Children.Add(_modeComboBox);
        configPanel.Children.Add(startButton);
        ContentStackPanel.Children.Add(configPanel);

        _modeDescriptionTextBlock = CreateSessionTextBlock("Select a mode to preview its screen flow.");
        _modeDescriptionTextBlock.Margin = new Thickness(0, 8, 0, 0);
        _modeDescriptionTextBlock.TextWrapping = TextWrapping.Wrap;
        ContentStackPanel.Children.Add(_modeDescriptionTextBlock);

        var note = new TextBlock
        {
            Text = "This Windows single-player area uses mode-screen flow only.",
            Margin = new Thickness(0, 14, 0, 0),
            Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99)),
            TextWrapping = TextWrapping.Wrap
        };
        ContentStackPanel.Children.Add(note);
    }

    private TextBlock CreateSessionTextBlock(string text) => new()
    {
        Text = text,
        Margin = new Thickness(0, 4, 0, 0),
        Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55)),
        FontSize = 16
    };

    private void UpdateModeDescription()
    {
        if (_modeComboBox?.SelectedItem is not string mode || mode == "Choose mode...")
        {
            _selectedMode = null;
            if (_modeDescriptionTextBlock is not null)
            {
                _modeDescriptionTextBlock.Text = "Select a mode to preview its screen flow.";
            }
            return;
        }

        _selectedMode = mode;
        if (_modeDescriptionTextBlock is null)
        {
            return;
        }

        _modeDescriptionTextBlock.Text = mode switch
        {
            "Classic" => "Classic: baseline single-player round flow screen.",
            "Hard" => "Hard: increased pressure with stricter margins and tougher pacing.",
            "Daily" => "Daily: one challenge per day with streak and completion tracking.",
            "Time Attack" => "Time Attack: accelerated pace with reduced error tolerance.",
            "Ghost" => "Ghost: hidden-information style flow (count feedback only).",
            "Speedrun" => "Speedrun: race progression to 10 completed words, no power-ups.",
            "Alphabet" => "Alphabet: A→Z progression where each next word starts with the target letter.",
            "Vs. CPU" => "Vs. CPU: competitive single-player lane versus AI outcome tracking.",
            "Survival" => "Survival: escalating rounds with best-streak progression.",
            "Word Hunt" => "Word Hunt: chained multi-round objective flow.",
            "Wild Dictionary" => "Wild Dictionary: randomized challenge setup and route.",
            _ => "Mode details are loading."
        };
    }

    private void OpenSinglePlayerModeScreen()
    {
        if (string.IsNullOrWhiteSpace(_selectedMode))
        {
            StatusTextBlock.Text = "Choose a single-player mode first.";
            return;
        }

        if (string.Equals(_selectedMode, "Alphabet", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(_selectedMode, "Classic", StringComparison.OrdinalIgnoreCase))
        {
            _activeWordleRunMode = string.Equals(_selectedMode, "Classic", StringComparison.OrdinalIgnoreCase)
                ? WordleRunMode.Classic
                : WordleRunMode.Alphabet;
            _activeGuessRows = _activeWordleRunMode == WordleRunMode.Classic
                ? ClassicGuessRows
                : AlphabetGuessRows;
            RenderWordleModePage();
            StatusTextBlock.Text = $"{_selectedMode} mode opened.";
            return;
        }

        StatusTextBlock.Text = $"{_selectedMode} screen opened (Windows mode-screen flow).";
    }

    private void RenderWordleModePage()
    {
        EnterAlphabetFullscreen();

        ContentStackPanel.Children.Clear();
        ContentStackPanel.Children.Add(new TextBlock
        {
            Text = _activeWordleRunMode == WordleRunMode.Classic
                ? "Classic Mode (Chardcore Layout)"
                : "Alphabet Mode (Chardcore Layout)",
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(17, 24, 39)),
            Margin = new Thickness(0, 0, 0, 12)
        });

        var actionRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var startButton = new Button
        {
            Content = _activeWordleRunMode == WordleRunMode.Classic ? "Start Classic Run" : "Start A→Z Run",
            Padding = new Thickness(12, 6, 12, 6),
            Margin = new Thickness(0, 0, 12, 0)
        };
        startButton.Click += (_, _) => StartWordleRun();
        actionRow.Children.Add(startButton);

        var backButton = new Button
        {
            Content = "Back to Modes",
            Padding = new Thickness(12, 6, 12, 6)
        };
        backButton.Click += (_, _) => RenderSinglePlayerPage();
        actionRow.Children.Add(backButton);
        ContentStackPanel.Children.Add(actionRow);

        var playLayoutGrid = new Grid { Margin = new Thickness(0, 6, 0, 0) };
        playLayoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var centerPanel = new StackPanel();
        centerPanel.Children.Add(new Border
        {
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
            Margin = new Thickness(0, 0, 0, 12),
            Child = new Grid
            {
                Height = 860,
                Children = { CreateAlphabetLayoutSurface() }
            }
        });
        Grid.SetColumn(centerPanel, 0);
        playLayoutGrid.Children.Add(centerPanel);

        var powerupsPanel = new StackPanel();
        powerupsPanel.Children.Add(new TextBlock
        {
            Text = "Powerups",
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(17, 24, 39)),
            Margin = new Thickness(0, 0, 0, 8)
        });
        powerupsPanel.Children.Add(CreateSessionTextBlock("No powerups in Alphabet mode"));
        powerupsPanel.Children.Add(CreateSessionTextBlock("Reserved for mode variants"));
        Grid.SetColumn(powerupsPanel, 0);
        powerupsPanel.HorizontalAlignment = HorizontalAlignment.Right;
        powerupsPanel.Margin = new Thickness(0, 12, 12, 0);
        playLayoutGrid.Children.Add(powerupsPanel);

        ContentStackPanel.Children.Add(playLayoutGrid);
    }

    private void StartWordleRun()
    {
        _alphabetWordsCleared = 0;
        if (_activeWordleRunMode == WordleRunMode.Alphabet)
        {
            _alphabetTargetLetter = 'A';
            StartAlphabetRound();
            return;
        }

        StartClassicRound();
    }

    private void StartClassicRound()
    {
        if (!TryFindClassicWord(out _alphabetAnswer))
        {
            _alphabetAnswer = null;
            if (_alphabetSubmitButton is not null)
            {
                _alphabetSubmitButton.IsEnabled = false;
            }

            UpdateAlphabetView();
            StatusTextBlock.Text = "Run stopped: no 5-letter word data for classic mode.";
            return;
        }

        _activeGuessRows = ClassicGuessRows;
        _alphabetAttemptsLeft = ClassicGuessRows;
        _alphabetCurrentRow = 0;
        _alphabetKnownLetters = new string('_', DefaultWordLength);
        _alphabetEliminatedLetters.Clear();
        ResetAlphabetBoardVisuals();
        ResetAlphabetKeyboardOverlay();
        if (_alphabetGuessTextBox is not null)
        {
            _alphabetGuessTextBox.Text = string.Empty;
            _alphabetGuessTextBox.Focus();
        }

        if (_alphabetSubmitButton is not null)
        {
            _alphabetSubmitButton.IsEnabled = true;
        }

        UpdateAlphabetView();
        StatusTextBlock.Text = "Classic round started.";
    }

    private void StartAlphabetRun()
    {
        _alphabetTargetLetter = 'A';
        _activeGuessRows = AlphabetGuessRows;
        StartAlphabetRound();
    }

    private void StartAlphabetRound()
    {
        if (!TryFindAlphabetWord(_alphabetTargetLetter, out _alphabetAnswer))
        {
            _alphabetAnswer = null;
            if (_alphabetSubmitButton is not null)
            {
                _alphabetSubmitButton.IsEnabled = false;
            }

            UpdateAlphabetView();
            StatusTextBlock.Text = $"Run stopped: no word data for letter {_alphabetTargetLetter}.";
            return;
        }

        _alphabetAttemptsLeft = 5;
        _alphabetCurrentRow = 0;
        _alphabetKnownLetters = $"{_alphabetTargetLetter}{new string('_', DefaultWordLength - 1)}";
        _alphabetEliminatedLetters.Clear();
        ResetAlphabetBoardVisuals();
        ResetAlphabetKeyboardOverlay();
        if (_alphabetGuessTextBox is not null)
        {
            _alphabetGuessTextBox.Text = string.Empty;
            _alphabetGuessTextBox.Focus();
        }

        if (_alphabetSubmitButton is not null)
        {
            _alphabetSubmitButton.IsEnabled = true;
        }

        UpdateAlphabetView();
        StatusTextBlock.Text = $"Alphabet round {_alphabetTargetLetter} started.";
    }

    private bool TryFindClassicWord(out string? answer)
    {
        var candidates = _wordService.GetAllWords()
            .Select(word => word.Text.Trim().ToUpperInvariant())
            .Where(text => text.Length == DefaultWordLength)
            .Distinct()
            .ToList();
        if (candidates.Count == 0)
        {
            answer = null;
            return false;
        }

        answer = candidates[Random.Shared.Next(candidates.Count)];
        return true;
    }

    private bool TryFindAlphabetWord(char targetLetter, out string? answer)
    {
        var candidates = new List<string>();
        foreach (var word in _wordService.GetAllWords())
        {
            if (string.IsNullOrWhiteSpace(word.Text))
            {
                continue;
            }

            var normalized = word.Text.Trim().ToUpperInvariant();
            if (normalized.Length == DefaultWordLength && normalized[0] == targetLetter)
            {
                candidates.Add(normalized);
            }
        }

        if (candidates.Count == 0)
        {
            answer = null;
            return false;
        }

        answer = candidates[Random.Shared.Next(candidates.Count)];
        return true;
    }

    private void SubmitAlphabetWord()
    {
        if (_isSubmittingAlphabetGuess)
        {
            return;
        }

        _isSubmittingAlphabetGuess = true;
        if (string.IsNullOrWhiteSpace(_alphabetAnswer))
        {
            StatusTextBlock.Text = "Start an A→Z run first.";
            _isSubmittingAlphabetGuess = false;
            return;
        }

        var guess = (_alphabetGuessTextBox?.Text ?? string.Empty).Trim().ToUpperInvariant();
        if (_alphabetGuessTextBox is not null)
        {
            _alphabetGuessTextBox.Text = string.Empty;
        }

        if (string.IsNullOrWhiteSpace(guess))
        {
            StatusTextBlock.Text = "Enter a full word guess.";
            _isSubmittingAlphabetGuess = false;
            return;
        }

        if (guess.Length != DefaultWordLength)
        {
            StatusTextBlock.Text = $"Enter a {DefaultWordLength}-letter word.";
            _isSubmittingAlphabetGuess = false;
            return;
        }

        if (_activeWordleRunMode == WordleRunMode.Alphabet && guess[0] != _alphabetTargetLetter)
        {
            StatusTextBlock.Text = $"Alphabet mode: word must start with {_alphabetTargetLetter}.";
            _isSubmittingAlphabetGuess = false;
            return;
        }

        if (guess == _alphabetAnswer)
        {
            var solvedEvaluation = EvaluateGuess(guess, _alphabetAnswer);
            PaintAlphabetRow(guess, solvedEvaluation[0], solvedEvaluation[1], solvedEvaluation[2], solvedEvaluation[3], solvedEvaluation[4]);
            UpdateKeyboardFromEvaluation(guess, solvedEvaluation);
            _alphabetWordsCleared++;
            if (_activeWordleRunMode == WordleRunMode.Classic)
            {
                StartClassicRound();
                _isSubmittingAlphabetGuess = false;
                return;
            }

            if (_alphabetTargetLetter == 'Z')
            {
                _alphabetAnswer = null;
                if (_alphabetSubmitButton is not null)
                {
                    _alphabetSubmitButton.IsEnabled = false;
                }

                UpdateAlphabetView();
                StatusTextBlock.Text = "Alphabet complete: cleared A→Z.";
                _isSubmittingAlphabetGuess = false;
                return;
            }

            _alphabetTargetLetter++;
            StartAlphabetRound();
            _isSubmittingAlphabetGuess = false;
            return;
        }

        var evaluation = EvaluateGuess(guess, _alphabetAnswer);
        PaintAlphabetRow(guess, evaluation[0], evaluation[1], evaluation[2], evaluation[3], evaluation[4]);
        UpdateAlphabetKnownLettersFromEvaluation(guess, evaluation);
        UpdateKeyboardFromEvaluation(guess, evaluation);
        SyncEliminatedLettersFromKeyboardStates();
        _alphabetAttemptsLeft--;
        if (_alphabetAttemptsLeft <= 0)
        {
            var answer = _alphabetAnswer;
            _alphabetAnswer = null;
            if (_alphabetSubmitButton is not null)
            {
                _alphabetSubmitButton.IsEnabled = false;
            }

            UpdateAlphabetView();
            StatusTextBlock.Text = $"Run ended at {_alphabetTargetLetter}. Word was {answer}.";
            _isSubmittingAlphabetGuess = false;
            return;
        }

        UpdateAlphabetView();
        var revealed = _alphabetKnownLetters.Count(character => character != '_');
        StatusTextBlock.Text = $"Incorrect. {revealed}/{DefaultWordLength} letters known.";
        _isSubmittingAlphabetGuess = false;
    }

    private void UpdateAlphabetView()
    {
        if (_alphabetProgressTextBlock is null || _alphabetTargetTextBlock is null || _alphabetHintTextBlock is null || _alphabetAttemptsTextBlock is null || _alphabetKnownLettersTextBlock is null || _alphabetEliminatedLettersTextBlock is null)
        {
            return;
        }

        _alphabetProgressTextBlock.Text = $"Progress: {_alphabetWordsCleared}/26 words cleared";
        if (_activeWordleRunMode == WordleRunMode.Classic)
        {
            _alphabetProgressTextBlock.Text = $"Classic words solved: {_alphabetWordsCleared}";
            _alphabetTargetTextBlock.Text = "Target letter: none";
            _alphabetHintTextBlock.Text = "Rule: standard 5-letter Wordle";
        }
        else
        {
            _alphabetProgressTextBlock.Text = $"Progress: {_alphabetWordsCleared}/26 words cleared";
            _alphabetTargetTextBlock.Text = _alphabetAnswer is null
                ? "Target letter: -"
                : $"Target letter (free): {_alphabetTargetLetter}";
            _alphabetHintTextBlock.Text = _alphabetAnswer is null
                ? "Mode: 5-letter Wordle"
                : $"Rule: 5 letters • starts with {_alphabetTargetLetter}";
        }
        _alphabetAttemptsTextBlock.Text = _alphabetAnswer is null
            ? "Guesses left: -"
            : $"Guesses left: {_alphabetAttemptsLeft}";
        _alphabetKnownLettersTextBlock.Text = _alphabetAnswer is null
            ? "Known: -"
            : $"Known: {_alphabetKnownLetters}";
        _alphabetEliminatedLettersTextBlock.Text = _alphabetEliminatedLetters.Count == 0
            ? "Eliminated: -"
            : $"Eliminated: {string.Join(" ", _alphabetEliminatedLetters.Order())}";
    }

    private void BuildAlphabetBoardTiles()
    {
        if (_alphabetBoardGrid is null)
        {
            return;
        }

        _alphabetTileBorders.Clear();
        _alphabetTileTextBlocks.Clear();
        _alphabetBoardGrid.Children.Clear();
        var tileCount = _activeGuessRows * DefaultWordLength;
        for (var index = 0; index < tileCount; index++)
        {
            var text = new TextBlock
            {
                Text = string.Empty,
                FontWeight = FontWeights.Bold,
                FontSize = 30,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Color.FromRgb(15, 23, 42),
                    BlurRadius = 0,
                    ShadowDepth = 1,
                    Opacity = 0.95
                },
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var tile = new Border
            {
                Width = 54,
                Height = 54,
                BorderThickness = new Thickness(1.5),
                BorderBrush = new SolidColorBrush(Color.FromArgb(170, 180, 189, 200)),
                Background = new SolidColorBrush(Color.FromArgb(120, 30, 41, 59)),
                Margin = new Thickness(4),
                CornerRadius = new CornerRadius(4),
                Child = text
            };

            _alphabetTileBorders.Add(tile);
            _alphabetTileTextBlocks.Add(text);
            _alphabetBoardGrid.Children.Add(tile);
        }
    }

    private void ResetAlphabetBoardVisuals()
    {
        var requiredTileCount = _activeGuessRows * DefaultWordLength;
        if (_alphabetTileBorders.Count < requiredTileCount || _alphabetTileTextBlocks.Count < requiredTileCount)
        {
            return;
        }

        for (var index = 0; index < _alphabetTileBorders.Count; index++)
        {
            _alphabetTileBorders[index].Background = new SolidColorBrush(Color.FromArgb(120, 30, 41, 59));
            _alphabetTileBorders[index].BorderBrush = new SolidColorBrush(Color.FromArgb(170, 180, 189, 200));
            _alphabetTileTextBlocks[index].Text = string.Empty;
            _alphabetTileTextBlocks[index].Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        }

        for (var row = 0; row < _activeGuessRows; row++)
        {
            var tileIndex = row * DefaultWordLength;
            if (_activeWordleRunMode == WordleRunMode.Alphabet)
            {
                _alphabetTileTextBlocks[tileIndex].Text = _alphabetTargetLetter.ToString();
                _alphabetTileBorders[tileIndex].Background = new SolidColorBrush(Color.FromArgb(230, 152, 22, 52));
                _alphabetTileBorders[tileIndex].BorderBrush = new SolidColorBrush(Color.FromArgb(230, 110, 13, 37));
            }
        }
    }

    private TileState[] EvaluateGuess(string guess, string answer)
    {
        var states = WordleEvaluator
            .Evaluate(guess, answer)
            .Select(state => state switch
            {
                WordleLetterState.Correct => TileState.Correct,
                WordleLetterState.Present => TileState.Present,
                _ => TileState.Absent
            })
            .ToArray();

        return states;
    }

    private void PaintAlphabetRow(string guess, TileState first, TileState second, TileState third, TileState fourth, TileState fifth)
    {
        var states = new[] { first, second, third, fourth, fifth };
        var requiredTileCount = _activeGuessRows * DefaultWordLength;
        if (_alphabetCurrentRow >= _activeGuessRows || _alphabetTileTextBlocks.Count < requiredTileCount || _alphabetTileBorders.Count < requiredTileCount)
        {
            return;
        }

        for (var column = 0; column < DefaultWordLength; column++)
        {
            var tileIndex = (_alphabetCurrentRow * DefaultWordLength) + column;
            _alphabetTileTextBlocks[tileIndex].Text = guess[column].ToString();
            ApplyTileState(_alphabetTileBorders[tileIndex], states[column]);
            _alphabetTileTextBlocks[tileIndex].Foreground = GetTileTextBrush(states[column]);
        }

        _alphabetCurrentRow++;
    }

    private static void ApplyTileState(Border tile, TileState state)
    {
        switch (state)
        {
            case TileState.Correct:
                tile.Background = new SolidColorBrush(Color.FromArgb(235, 152, 22, 52));
                tile.BorderBrush = new SolidColorBrush(Color.FromArgb(235, 110, 13, 37));
                break;
            case TileState.Present:
                tile.Background = new SolidColorBrush(Color.FromArgb(235, 186, 151, 100));
                tile.BorderBrush = new SolidColorBrush(Color.FromArgb(235, 140, 109, 70));
                break;
            default:
                tile.Background = new SolidColorBrush(Color.FromArgb(230, 78, 65, 56));
                tile.BorderBrush = new SolidColorBrush(Color.FromArgb(225, 60, 49, 42));
                break;
        }
    }

    private static Brush GetTileTextBrush(TileState state) => state switch
    {
        TileState.Present => new SolidColorBrush(Color.FromRgb(10, 15, 25)),
        _ => new SolidColorBrush(Color.FromRgb(255, 255, 255))
    };

    private void UpdateAlphabetKnownLettersFromEvaluation(string guess, IReadOnlyList<TileState> states)
    {
        var known = _alphabetKnownLetters.ToCharArray();
        for (var index = 0; index < DefaultWordLength; index++)
        {
            if (states[index] == TileState.Correct)
            {
                known[index] = guess[index];
            }
        }

        _alphabetKnownLetters = new string(known);
    }

    private void SyncEliminatedLettersFromKeyboardStates()
    {
        _alphabetEliminatedLetters.Clear();
        foreach (var entry in _alphabetKeyboardStates)
        {
            if (entry.Value == KeyboardLetterState.Absent)
            {
                _alphabetEliminatedLetters.Add(entry.Key);
            }
        }
    }

    private void UpdateKeyboardFromEvaluation(string guess, IReadOnlyList<TileState> evaluation)
    {
        for (var index = 0; index < Math.Min(guess.Length, evaluation.Count); index++)
        {
            var letter = guess[index];
            var nextState = evaluation[index] switch
            {
                TileState.Correct => KeyboardLetterState.Correct,
                TileState.Present => KeyboardLetterState.Present,
                _ => KeyboardLetterState.Absent
            };

            SetKeyboardLetterState(letter, nextState);
        }
    }

    private FrameworkElement CreateAlphabetLayoutSurface()
    {
        var surface = new Grid
        {
            Width = AlphabetSurfaceWidth,
            Height = AlphabetSurfaceHeight
        };

        surface.Children.Add(CreateAlphabetReferenceImage());

        _alphabetBoardGrid = new UniformGrid
        {
            Rows = _activeGuessRows,
            Columns = DefaultWordLength,
            Width = _activeWordleRunMode == WordleRunMode.Classic ? ClassicBoardWidth : AlphabetBoardSize,
            Height = _activeWordleRunMode == WordleRunMode.Classic ? ClassicBoardHeight : AlphabetBoardSize,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = _activeWordleRunMode == WordleRunMode.Classic
                ? new Thickness(ClassicBoardX, ClassicBoardY, 0, 0)
                : new Thickness(AlphabetBoardX, AlphabetBoardY, 0, 0)
        };
        BuildAlphabetBoardTiles();
        surface.Children.Add(_alphabetBoardGrid);
        surface.Children.Add(CreateStatsOverlay());
        surface.Children.Add(CreateGuessOverlay());
        if (_activeWordleRunMode == WordleRunMode.Alphabet)
        {
            surface.Children.Add(CreateKeyboardOverlay());
        }

        return new Viewbox
        {
            Stretch = Stretch.UniformToFill,
            StretchDirection = StretchDirection.DownOnly,
            Width = AlphabetSurfaceWidth * 0.85,
            Height = AlphabetSurfaceHeight * 0.85,
            Child = surface
        };
    }

    private Grid CreateGuessOverlay()
    {
        var overlay = new Grid
        {
            Width = _activeWordleRunMode == WordleRunMode.Classic ? ClassicGuessWidth : 660,
            Height = 52,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = _activeWordleRunMode == WordleRunMode.Classic
                ? new Thickness(ClassicGuessX, ClassicGuessY, 0, 0)
                : new Thickness(344, 102, 0, 0)
        };
        overlay.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        overlay.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        overlay.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _alphabetGuessTextBox = new TextBox
        {
            MaxLength = DefaultWordLength,
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Thickness(8, 6, 8, 6),
            Background = new SolidColorBrush(Color.FromArgb(230, 246, 247, 250)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
            BorderThickness = new Thickness(2)
        };
        _alphabetGuessTextBox.KeyDown += (_, args) =>
        {
            if (args.Key == System.Windows.Input.Key.Enter)
            {
                SubmitAlphabetWord();
            }
        };
        Grid.SetColumn(_alphabetGuessTextBox, 0);
        overlay.Children.Add(_alphabetGuessTextBox);

        _alphabetSubmitButton = new Button
        {
            Content = "Guess",
            Padding = new Thickness(18, 8, 18, 8),
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            IsEnabled = false
        };
        _alphabetSubmitButton.Click += (_, _) => SubmitAlphabetWord();
        Grid.SetColumn(_alphabetSubmitButton, 2);
        overlay.Children.Add(_alphabetSubmitButton);

        return overlay;
    }

    private Border CreateStatsOverlay()
    {
        var statsPanel = new StackPanel
        {
            Margin = new Thickness(0)
        };

        _alphabetProgressTextBlock = CreateSessionTextBlock("Run progress: not started");
        _alphabetTargetTextBlock = CreateSessionTextBlock("Target letter: -");
        _alphabetHintTextBlock = CreateSessionTextBlock("Mode: 5-letter Wordle");
        _alphabetAttemptsTextBlock = CreateSessionTextBlock("Guesses left: -");
        _alphabetKnownLettersTextBlock = CreateSessionTextBlock("Known: -");
        _alphabetEliminatedLettersTextBlock = CreateSessionTextBlock("Eliminated: -");
        foreach (var text in new[]
                 {
                     _alphabetProgressTextBlock, _alphabetTargetTextBlock, _alphabetHintTextBlock,
                     _alphabetAttemptsTextBlock, _alphabetKnownLettersTextBlock, _alphabetEliminatedLettersTextBlock
                 })
        {
            if (text is null) continue;
            text.FontSize = 17;
            text.Foreground = new SolidColorBrush(Color.FromRgb(241, 245, 249));
            text.Margin = new Thickness(0, 6, 0, 0);
            statsPanel.Children.Add(text);
        }

        return new Border
        {
            Width = 360,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = _activeWordleRunMode == WordleRunMode.Classic
                ? new Thickness(ClassicStatsX, ClassicStatsY, 0, 0)
                : new Thickness(280, 245, 0, 0),
            Background = Brushes.Transparent,
            Child = statsPanel
        };
    }

    private void EnterAlphabetFullscreen()
    {
        if (_isAlphabetFullscreen)
        {
            return;
        }

        _isAlphabetFullscreen = true;
        _savedWindowStyle = WindowStyle;
        _savedWindowState = WindowState;
        _savedResizeMode = ResizeMode;
        RootLayoutGrid.Margin = new Thickness(0);
        NavPanelBorder.Visibility = Visibility.Collapsed;
        NavColumnDefinition.Width = new GridLength(0);
        NavGapColumnDefinition.Width = new GridLength(0);
        ContentColumnDefinition.Width = new GridLength(1, GridUnitType.Star);
        Grid.SetColumn(ContentPanelBorder, 0);
        Grid.SetColumnSpan(ContentPanelBorder, 3);
        WindowStyle = WindowStyle.None;
        WindowState = WindowState.Maximized;
        ResizeMode = ResizeMode.NoResize;
    }

    private void ExitAlphabetFullscreen()
    {
        if (!_isAlphabetFullscreen)
        {
            return;
        }

        _isAlphabetFullscreen = false;
        Grid.SetColumnSpan(ContentPanelBorder, 1);
        Grid.SetColumn(ContentPanelBorder, 2);
        ContentColumnDefinition.Width = new GridLength(1, GridUnitType.Star);
        NavGapColumnDefinition.Width = new GridLength(20);
        NavColumnDefinition.Width = new GridLength(280);
        NavPanelBorder.Visibility = Visibility.Visible;
        RootLayoutGrid.Margin = new Thickness(20);
        ResizeMode = _savedResizeMode;
        WindowStyle = _savedWindowStyle;
        WindowState = _savedWindowState;
    }

    private Canvas CreateKeyboardOverlay()
    {
        var canvas = new Canvas
        {
            Width = AlphabetKeyboardWidth,
            Height = AlphabetKeyboardHeight,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(AlphabetKeyboardX, AlphabetKeyboardY, 0, 0),
            IsHitTestVisible = false
        };

        _alphabetKeyboardKeyBorders.Clear();
        _alphabetKeyboardKeyTextBlocks.Clear();
        _alphabetKeyboardStates.Clear();

        AddKeyboardRow(canvas, "QWERTYUIOP", y: 0);
        AddKeyboardRow(canvas, "ASDFGHJKL", y: 44);
        AddKeyboardRow(canvas, "ZXCVBNM", y: 88);
        ResetAlphabetKeyboardOverlay();
        return canvas;
    }

    private void AddKeyboardRow(Canvas canvas, string letters, double y)
    {
        const double keyWidth = 58;
        const double keyHeight = 30;
        const double keyGap = 9;
        var rowWidth = (letters.Length * keyWidth) + ((letters.Length - 1) * keyGap);
        var x = (canvas.Width - rowWidth) / 2;

        foreach (var letter in letters)
        {
            var text = new TextBlock
            {
                Text = letter.ToString(),
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromArgb(210, 22, 28, 35)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var border = new Border
            {
                Width = keyWidth,
                Height = keyHeight,
                CornerRadius = new CornerRadius(5),
                Background = Brushes.Transparent,
                BorderBrush = new SolidColorBrush(Color.FromArgb(130, 176, 184, 192)),
                BorderThickness = new Thickness(1.2),
                Child = text
            };

            Canvas.SetLeft(border, x);
            Canvas.SetTop(border, y);
            canvas.Children.Add(border);
            _alphabetKeyboardKeyBorders[letter] = border;
            _alphabetKeyboardKeyTextBlocks[letter] = text;
            _alphabetKeyboardStates[letter] = KeyboardLetterState.Unknown;
            x += keyWidth + keyGap;
        }
    }

    private void ResetAlphabetKeyboardOverlay()
    {
        foreach (var letter in _alphabetKeyboardStates.Keys.ToList())
        {
            _alphabetKeyboardStates[letter] = KeyboardLetterState.Unknown;
            ApplyKeyboardLetterState(letter, KeyboardLetterState.Unknown);
        }
    }

    private void SetKeyboardLetterState(char letter, KeyboardLetterState next)
    {
        if (!_alphabetKeyboardStates.TryGetValue(letter, out var current))
        {
            return;
        }

        if (GetKeyboardStateRank(next) < GetKeyboardStateRank(current))
        {
            return;
        }

        _alphabetKeyboardStates[letter] = next;
        ApplyKeyboardLetterState(letter, next);
    }

    private static int GetKeyboardStateRank(KeyboardLetterState state) => state switch
    {
        KeyboardLetterState.Correct => 3,
        KeyboardLetterState.Present => 2,
        KeyboardLetterState.Absent => 1,
        _ => 0
    };

    private void ApplyKeyboardLetterState(char letter, KeyboardLetterState state)
    {
        if (!_alphabetKeyboardKeyBorders.TryGetValue(letter, out var border) ||
            !_alphabetKeyboardKeyTextBlocks.TryGetValue(letter, out var text))
        {
            return;
        }

        switch (state)
        {
            case KeyboardLetterState.Correct:
                border.Background = new SolidColorBrush(Color.FromArgb(225, 152, 22, 52));
                border.BorderBrush = new SolidColorBrush(Color.FromArgb(235, 110, 13, 37));
                text.Foreground = new SolidColorBrush(Color.FromRgb(248, 250, 252));
                text.TextDecorations = null;
                break;
            case KeyboardLetterState.Present:
                border.Background = new SolidColorBrush(Color.FromArgb(225, 186, 151, 100));
                border.BorderBrush = new SolidColorBrush(Color.FromArgb(235, 140, 109, 70));
                text.Foreground = new SolidColorBrush(Color.FromRgb(17, 24, 39));
                text.TextDecorations = null;
                break;
            case KeyboardLetterState.Absent:
                border.Background = new SolidColorBrush(Color.FromArgb(225, 73, 80, 87));
                border.BorderBrush = new SolidColorBrush(Color.FromArgb(235, 55, 65, 75));
                text.Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                text.TextDecorations = TextDecorations.Strikethrough;
                break;
            default:
                border.Background = Brushes.Transparent;
                border.BorderBrush = new SolidColorBrush(Color.FromArgb(95, 195, 200, 205));
                text.Foreground = new SolidColorBrush(Color.FromArgb(170, 25, 25, 25));
                text.TextDecorations = null;
                break;
        }
    }

    private FrameworkElement CreateAlphabetReferenceImage()
    {
        try
        {
            var layoutFile = _activeWordleRunMode == WordleRunMode.Classic
                ? "chardcorelayoutclassic.png"
                : "chardcorealphabet.png";
            var resourceUri = new Uri($"pack://application:,,,/Assets/{layoutFile}", UriKind.Absolute);
            var resourceStream = Application.GetResourceStream(resourceUri);
            if (resourceStream is null)
            {
                return CreateMissingImageText();
            }

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = resourceStream.Stream;
            bitmap.EndInit();
            bitmap.Freeze();

            return new Image
            {
                Source = bitmap,
                Stretch = Stretch.Fill,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
        catch (IOException)
        {
            return CreateMissingImageText();
        }
        catch (NotSupportedException)
        {
            return CreateMissingImageText();
        }
        catch (UriFormatException)
        {
            return CreateMissingImageText();
        }
    }

    private static TextBlock CreateMissingImageText() => new()
    {
        Text = "Layout image unavailable.",
        Margin = new Thickness(10),
        Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99))
    };

    private enum TileState
    {
        Absent,
        Present,
        Correct
    }

    private enum KeyboardLetterState
    {
        Unknown,
        Absent,
        Present,
        Correct
    }

    private enum WordleRunMode
    {
        Alphabet,
        Classic
    }
}
