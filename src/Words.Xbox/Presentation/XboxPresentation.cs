using System.Text;
using Words.Core.Models;

namespace Words.Xbox.Presentation;

public static class XboxPresentation
{
    private const int PageWidth = 74;
    private static readonly string[] TitleArt =
    [
        @" __        __   ____  ____  ____  ____  ",
        @" \ \  /\  / /__| __ )|  _ \|  _ \|  _ \ ",
        @"  \ \/  \/ / _ \  _ \| |_) | | | | | | |",
        @"   \  /\  /  __/ |_) |  __/| |_| | |_| |",
        @"    \/  \/ \___|____/|_|   |____/|____/ ",
    ];

    private static readonly string[] TitleFrames =
    [
        "✦   •   ✦   •   ✦   •   ✦   •   ✦   •",
        "  •   ✦   •   ✦   •   ✦   •   ✦   •   ",
        "•   ✦   •   ✦   •   ✦   •   ✦   •   ✦ ",
        "  ✦   •   ✦   •   ✦   •   ✦   •   ✦   ",
    ];

    public static string ComposeTitleScreen(Player? player, int frameIndex, bool reducedMotion)
    {
        var body = new List<string>();
        body.AddRange(TitleArt);
        body.Add(string.Empty);
        body.Add(reducedMotion ? TitleFrames[0] : TitleFrames[frameIndex % TitleFrames.Length]);
        body.Add("Guess the word. Read the room. Keep the controller in your hands.");
        body.Add(string.Empty);
        body.Add($"Current player: {player?.GamerTag ?? "Guest"}");
        body.Add("Menu:");
        body.Add("  [Play]      Start a round");
        body.Add("  [Tutorial]  Controller tips and scoring");
        body.Add("  [Credits]   Team, music, and unlocks");
        body.Add("  [Privacy]   Data handling notes");
        body.Add("  [Legal]     Terms and disclaimers");
        body.Add("  [Quit]      Exit the game");

        var footer = new[]
        {
            "Accessibility: high-contrast text, readable sizes, and controller-first navigation.",
            "Reduced-motion builds keep the layout static and skip background cycling.",
        };

        return BuildPage("WORDS", "Xbox edition title screen", body, footer);
    }

    public static string ComposeTutorialPage()
    {
        return BuildPage(
            "Tutorial",
            "How to play",
            new[]
            {
                "1. Pick a word length and how many words you want at once.",
                "2. Move across the QWERTY rows with LEFT/RIGHT.",
                "3. Press A to confirm a highlighted letter, B to delete, and X to submit.",
                "4. Y asks for a hint when hints are enabled.",
                "5. Guess every letter before your misses run out.",
            },
            new[]
            {
                "The board keeps the same visual language across the whole game.",
                "Controls are designed for a controller first, with readable text fallback.",
            });
    }

    public static string ComposeCreditsPage(Player? player)
    {
        return BuildPage(
            "Credits",
            "Lightweight roll call",
            new[]
            {
                "Words: Xbox edition",
                "Concept, code, and chaos: the Words team",
                $"Player profile: {player?.GamerTag ?? "Guest"}",
                "Music and sound effects live as separate assets under src/Words.Xbox/Audio.",
            },
            new[]
            {
                "Title update achievements include Credits Roll, Rivalry Crown, and Perfect Ten.",
            });
    }

    public static string ComposePrivacyPage()
    {
        return BuildPage(
            "Privacy Policy",
            "Simple sample-build privacy notes",
            new[]
            {
                "This sample stores progress in memory only while the game is running.",
                "No account, network sync, or analytics service is bundled with this repo.",
                "Audio files are loaded from the local game directory when available.",
            },
            new[]
            {
                "If you adapt this into a shipped title, replace this page with the final policy.",
            });
    }

    public static string ComposeLegalPage()
    {
        return BuildPage(
            "Legalese",
            "General disclaimer text",
            new[]
            {
                "This project is provided as-is for demonstration and development purposes.",
                "All trademarks and platform names remain the property of their respective owners.",
                "Gameplay rules, visuals, and menu copy may change as the title evolves.",
            },
            new[]
            {
                "Nothing here is legal advice; use counsel before shipping a commercial release.",
            });
    }

    public static string ComposeRoundScreen(GameSession session, string pendingGuess, int selectedIndex)
    {
        var keyboardRows = RenderKeyboard(selectedIndex).ToArray();
        var body = new List<string>
        {
            $"Hint: {session.Hint}",
            $"Words: {string.Join(" | ", session.MaskedWords)}",
            $"Guesses left: {session.RemainingGuesses}",
            $"Hints left: {(session.CanUseHints ? session.RemainingHints : 0)}",
            $"Current guess: {(pendingGuess.Length == 0 ? "(empty)" : pendingGuess)}",
            string.Empty,
            "Keyboard:",
        };

        body.AddRange(keyboardRows);
        body.AddRange(new[]
        {
            string.Empty,
            $"Guessed letters: {(session.GuessedLetters.Count == 0 ? "(none)" : string.Join(" ", session.GuessedLetters.Order()))}",
            "Status indicators: ✓ correct, ✕ incorrect, ⌁ hint, ←/→ move, A confirm, B delete, X submit, Y help",
        });

        return BuildPage("Game Screen", session.Status == GameStatus.InProgress ? "Round in progress" : "Round complete", body, Array.Empty<string>());
    }

    private static IEnumerable<string> RenderKeyboard(int selectedIndex)
    {
        var rows = new[]
        {
            "QWERTYUIOP",
            "ASDFGHJKL",
            "ZXCVBNM",
        };

        var index = 0;
        for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            var builder = new StringBuilder();
            foreach (char letter in rows[rowIndex])
            {
                var display = index == selectedIndex ? $"[{letter}]" : $" {letter} ";
                builder.Append(display);
                builder.Append(' ');
                index++;
            }
            yield return builder.ToString().TrimEnd();
        }
    }

    private static string BuildPage(string title, string subtitle, IReadOnlyCollection<string> bodyLines, IReadOnlyCollection<string> footerLines)
    {
        var lines = new List<string>
        {
            $"╔{new string('═', PageWidth - 2)}╗",
            CenterLine(title.ToUpperInvariant()),
            CenterLine(subtitle),
            $"╠{new string('═', PageWidth - 2)}╣",
        };

        foreach (var line in bodyLines)
            lines.AddRange(FormatParagraph(line));

        if (footerLines.Count > 0)
        {
            lines.Add($"╟{new string('─', PageWidth - 2)}╢");
            foreach (var line in footerLines)
                lines.AddRange(FormatParagraph(line));
        }

        lines.Add($"╚{new string('═', PageWidth - 2)}╝");
        return string.Join(Environment.NewLine, lines);
    }

    private static IEnumerable<string> FormatParagraph(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield return $"║{new string(' ', PageWidth - 2)}║";
            yield break;
        }

        if (text.Length <= PageWidth - 4)
        {
            yield return $"║ {text.PadRight(PageWidth - 4)} ║";
            yield break;
        }

        foreach (var chunk in Wrap(text, PageWidth - 4))
            yield return $"║ {chunk.PadRight(PageWidth - 4)} ║";
    }

    private static string CenterLine(string text)
    {
        var padded = text.PadLeft((PageWidth - 2 + text.Length) / 2).PadRight(PageWidth - 2);
        return $"║{padded[..(PageWidth - 2)]}║";
    }

    private static IEnumerable<string> Wrap(string text, int width)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var line = new StringBuilder();

        foreach (var word in words)
        {
            if (line.Length == 0)
            {
                line.Append(word);
                continue;
            }

            if (line.Length + 1 + word.Length <= width)
            {
                line.Append(' ').Append(word);
                continue;
            }

            yield return line.ToString();
            line.Clear();
            line.Append(word);
        }

        if (line.Length > 0)
            yield return line.ToString();
    }
}
