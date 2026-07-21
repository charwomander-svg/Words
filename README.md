# Words

**Words** is a giant word-game prototype built around Wordle-style guessing, a large English dictionary, rarity metadata, progression, and multiple modes.

The headline prototype mode is **Words Royale**: a local 99-entrant battle royale simulation where open seats are filled with bots, correct solves add time, failed or skipped words cost a large time penalty, and the last wordsmith standing wins.

## Run the Windows demo

From the repository root:

```powershell
.\scripts\run-windows-demo.ps1
```

Or run the project directly:

```powershell
dotnet run --project src\Words.Windows\Words.Windows.csproj
```

## Package the Windows demo

```powershell
.\scripts\publish-windows-demo.ps1
.\scripts\launch-windows-demo.ps1
```

The packaged demo is written to:

```text
artifacts\WordsDemo\Words.Windows.exe
```

It is self-contained for `win-x64`, so it does not require a separate .NET install.

## Xbox-facing pitch build

For pitch flow, the Xbox artifact is packaged from the graphical Windows demo shell (so `Words.Xbox.exe` opens the full UI instead of the legacy console loop):

```powershell
.\scripts\publish-xbox-demo.ps1
.\scripts\launch-xbox-demo.ps1
```

To publish both showcase artifacts in one pass:

```powershell
.\scripts\publish-pitch-build.ps1
```

## Current demo features

- 4-20 letter word lengths backed by `english-words-4to20.txt.gz`
- Full-dictionary guess validation
- Answer rarity/commonality warnings
- Seeded safe-answer metadata for common 4-8 letter words
- Post-round word info only after reveal
- On-screen keyboard with letter-state colors, Enter, and Back
- Shareable emoji result grid
- Local stats, score, streaks, badges, Time Attack combo records, Royale milestones, Mega Board records, and recent discovered words
- Time Attack, Ghost, Vs. CPU, 100-board Mega Board, and playable Words Royale prototype loops
- Copy stats export and local stats folder access
- Metadata coverage and answer-pool coverage reporting
- Repeatable all-mode playthrough coverage in tests
- Main UI has a **Pitch Setup** shortcut that loads a showcase baseline quickly (Words Royale 5-letter focus, Elite CPU preset, and Mega Board danger-view defaults)

## Modes

| Mode | Purpose |
| --- | --- |
| Classic | Safer answer pool; full dictionary still valid for guesses |
| Hard | Classic answers plus forced reuse of revealed letters |
| Daily | Deterministic answer by date and word length |
| Alphabet | A-to-Z chain; first letter is free, so each word has one fewer guess |
| Timed / Time Attack | 3-minute multi-word run; solves add time, fail/skip costs time, consecutive solves build combo bonus scoring, and run recaps/share text include score, solved count, best combo, time left, rating, and highlights |
| Ghost | Hidden positional feedback; only exact/present counts are shown |
| Vs. CPU | Difficulty-selectable race against a simulated CPU persona with distinct pacing, plus dedicated end-of-race results/share text |
| Words Royale | 99-entrant bot-filled survival match; solves add time, mistakes drain time, phase-aware persona bots change pace under pressure, phase-colored readable multi-line phase/rank/leaders/danger/rival/surging/collapsing/bubble summaries track the drama, Royale power-ups can sabotage leaders or stall bot actions, podium-style final-table ceremonies celebrate top finishers, and progression records runs, wins, top-10s, best placement, and best solve count |
| Mega Board | 4, 9, 16, 25, 50, or 100 simultaneous boards; one shared guess applies to every active board, including 20-letter chaos, with aggregate keyboard feedback, filtered/sorted dashboards, danger reports, Focus Critical, smart focus, patterns, scoring, share text, and Mega-aware power-ups |
| Survival | Win to continue; word length ramps as streak grows |
| Rare Hunt | Intentionally targets rare and wild dictionary answers |
| Discovery | Prioritizes answer words not already discovered |
| Wild Dictionary | Full giant dictionary can be the answer |

## Data and saves

The Windows demo stores local progression at:

```text
%APPDATA%\Words\demo-stats.json
```

Use **Stats Folder** in the demo to open that location, or **Reset Stats** to clear local demo progress.

## Core projects

| Project | Purpose |
| --- | --- |
| `src\Words.Core` | Shared word services, dictionary loading, metadata, scoring/session logic, progression state |
| `src\Words.Windows` | Windows Forms Words demo UI |
| `src\Words.Xbox` | Legacy console/Xbox host retained for platform plumbing; pitch packaging now ships the graphical shell |
| `tests\Words.Tests` | Core tests |

## Build and test

```powershell
dotnet test Words.slnx
dotnet build src\Words.Windows\Words.Windows.csproj
```

If the Windows demo is currently running, the normal build output may be locked. Close the app before rebuilding, or build to an alternate output path.

## Next major work

- Import a larger real frequency/definition metadata source
- Continue improving answer quality across 9-20 letter lengths
- Expand Words Royale with stronger end-of-match ceremony visuals and mode-specific visual treatments for rivalry/drama cues
- Replace the WinForms prototype UI with a polished game interface
- Revisit audio assets and deeper Xbox UI readiness
