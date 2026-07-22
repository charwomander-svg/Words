# Words
For the game "guess that word" with a Windows-first UI path.

## Windows app (visual screens)

Run the new Windows desktop shell:

`dotnet run --project src/Words.Windows/Words.Windows.csproj`

Use **Run Visual Demo Tour** in the app to auto-walk all major pages.

Run the repeatable demo tour with `dotnet run --project src/Words.Xbox/Words.Xbox.csproj -- --demo`.
It uses curated words and an isolated demo leaderboard, so it does not touch your normal save data.

## Xbox package

The console host stays in `Words.Xbox`. The Xbox package target lives in `src/Words.Xbox.Package/Words.Xbox.Package.csproj`.

Build the shared code and host with:

`dotnet build Words.slnx`

Build the Xbox package target on Windows:

`dotnet msbuild src/Words.Xbox.Package/Words.Xbox.Package.csproj /p:Configuration=Debug /p:Platform=x64`

The package output is written under `src/Words.Xbox.Package/bin/x64/Debug/AppPackages/`.

Deploy to an Xbox in Dev Mode by uploading the generated MSIX from the Device Portal or by installing it from Visual Studio after enabling device deployment.
