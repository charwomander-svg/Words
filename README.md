# Words
For the Xbox game "guess that word"

Players can guess one letter at a time or enter the full word when they are ready. A wrong full-word guess costs one guess.

Run the repeatable demo tour with `dotnet run --project src/Words.Xbox/Words.Xbox.csproj -- --demo`.
It uses curated words and an isolated demo leaderboard, so it does not touch your normal save data.

## Modes

Each round can be played in one of three modes:

- Classic: standard six-guess scoring.
- Relaxed: three extra guesses with reduced scoring.
- Challenge: two fewer guesses with higher scoring bonuses.

## Xbox package

The console host stays in `Words.Xbox`. The Xbox package target lives in `src/Words.Xbox.Package/Words.Xbox.Package.csproj`.

Build the shared code and host with:

`dotnet build Words.slnx`

Build the Xbox package target on Windows:

`dotnet msbuild src/Words.Xbox.Package/Words.Xbox.Package.csproj /p:Configuration=Debug /p:Platform=x64`

The package output is written under `src/Words.Xbox.Package/bin/x64/Debug/AppPackages/`.

Deploy to an Xbox in Dev Mode by uploading the generated MSIX from the Device Portal or by installing it from Visual Studio after enabling device deployment.
