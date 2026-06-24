# Words
For the Xbox game "guess that word" 
Run a repeatable demo with `dotnet run --project src/Words.Xbox/Words.Xbox.csproj -- --demo`.

## Xbox package

The console host stays in `Words.Xbox`. The Xbox package target lives in `src/Words.Xbox.Package/Words.Xbox.Package.csproj`.

Build the shared code and host with:

`dotnet build Words.slnx`

Build the Xbox package target on Windows:

`dotnet msbuild src/Words.Xbox.Package/Words.Xbox.Package.csproj /p:Configuration=Debug /p:Platform=x64`

The package output is written under `src/Words.Xbox.Package/bin/x64/Debug/AppPackages/`.

Deploy to an Xbox in Dev Mode by uploading the generated MSIX from the Device Portal or by installing it from Visual Studio after enabling device deployment.
