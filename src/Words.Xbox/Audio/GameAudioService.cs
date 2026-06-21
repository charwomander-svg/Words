using NAudio.Wave;

namespace Words.Xbox.Audio;

public sealed class GameAudioService : IDisposable
{
    private static readonly string[] SupportedExtensions = [".mp3", ".wav", ".aac", ".m4a", ".flac", ".wma"];
    private readonly string _assetRoot;
    private readonly Random _random;
    private readonly object _sync = new();
    private IWavePlayer? _musicOutput;
    private AudioFileReader? _musicReader;
    private string? _currentMusicPath;

    public GameAudioService(string? assetRoot = null, Random? random = null)
    {
        _assetRoot = string.IsNullOrWhiteSpace(assetRoot)
            ? Path.Combine(AppContext.BaseDirectory, "Audio")
            : assetRoot;
        _random = random ?? Random.Shared;
    }

    public void EnterTitleScreen() => PlayMusic("Music", "title", loop: true);
    public void EnterRound()
    {
        if (!PlayMusic("Music", "stage", loop: true))
            PlayMusic("Music", "round", loop: true);
    }
    public void EnterCredits() => PlayMusic("Music", "credits", loop: true);

    public void PlayCorrectGuess() => PlayEffect("Sfx", "correct");
    public void PlayIncorrectGuess() => PlayEffect("Sfx", "incorrect");
    public void PlayHint() => PlayEffect("Sfx", "hint");
    public void PlayAchievement() => PlayEffect("Sfx", "achievement");
    public void PlayNavigate() => PlayEffect("Sfx", "navigate");
    public void PlayConfirm() => PlayEffect("Sfx", "confirm");
    public void PlayDelete() => PlayEffect("Sfx", "delete");
    public void PlayRoundStart() => PlayEffect("Sfx", "round-start");
    public void PlayRoundEnd() => PlayEffect("Sfx", "round-end");

    public void StopMusic()
    {
        lock (_sync)
            StopMusicCore();
    }

    public void Dispose()
    {
        lock (_sync)
            StopMusicCore();
    }

    private bool PlayMusic(string category, string trackName, bool loop)
    {
        var path = ResolveAsset(category, trackName);
        if (path is null || !OperatingSystem.IsWindows())
            return false;

        lock (_sync)
        {
            if (string.Equals(_currentMusicPath, path, StringComparison.OrdinalIgnoreCase))
                return true;

            StopMusicCore();

            try
            {
                var reader = new AudioFileReader(path);
                var output = new WaveOutEvent();
                output.Init(reader);
                output.PlaybackStopped += (_, _) =>
                {
                    if (!loop)
                    {
                        lock (_sync)
                        {
                            if (ReferenceEquals(_musicOutput, output))
                                StopMusicCore();
                        }
                        return;
                    }

                    lock (_sync)
                    {
                        if (!ReferenceEquals(_musicOutput, output) || _musicReader is null)
                            return;

                        _musicReader.Position = 0;
                        output.Play();
                    }
                };

                _musicReader = reader;
                _musicOutput = output;
                _currentMusicPath = path;
                output.Play();
                return true;
            }
            catch
            {
                StopMusicCore();
            }
        }

        return false;
    }

    private void PlayEffect(string category, string effectName)
    {
        var path = ResolveAsset(category, effectName);
        if (path is null || !OperatingSystem.IsWindows())
            return;

        _ = Task.Run(() =>
        {
            try
            {
                using var reader = new AudioFileReader(path);
                using var output = new WaveOutEvent();
                output.Init(reader);
                output.Play();

                while (output.PlaybackState == PlaybackState.Playing)
                    Thread.Sleep(25);
            }
            catch
            {
            }
        });
    }

    internal string? ResolveAsset(string category, string name)
    {
        var directory = Path.Combine(_assetRoot, category);
        if (!Directory.Exists(directory))
            return null;

        var exactMatch = Directory.EnumerateFiles(directory)
            .FirstOrDefault(file =>
                string.Equals(Path.GetFileNameWithoutExtension(file), name, StringComparison.OrdinalIgnoreCase) &&
                SupportedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase));

        if (exactMatch is not null)
            return exactMatch;

        var prefixMatches = Directory.EnumerateFiles(directory)
            .Where(file =>
                Path.GetFileNameWithoutExtension(file).StartsWith(name, StringComparison.OrdinalIgnoreCase) &&
                SupportedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .OrderBy(file => Path.GetFileNameWithoutExtension(file), StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (prefixMatches.Count == 0)
            return null;

        return prefixMatches[_random.Next(prefixMatches.Count)];
    }

    private void StopMusicCore()
    {
        var output = _musicOutput;
        var reader = _musicReader;

        _musicOutput = null;
        _musicReader = null;
        _currentMusicPath = null;

        try
        {
            output?.Stop();
        }
        catch
        {
        }

        output?.Dispose();
        reader?.Dispose();
    }
}
