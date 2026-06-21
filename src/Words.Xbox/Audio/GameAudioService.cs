using NAudio.Wave;

namespace Words.Xbox.Audio;

public sealed class GameAudioService : IDisposable
{
    private static readonly string[] SupportedExtensions = [".mp3", ".wav", ".aac", ".m4a", ".flac", ".wma"];
    private readonly string _assetRoot;
    private readonly object _sync = new();
    private IWavePlayer? _musicOutput;
    private AudioFileReader? _musicReader;
    private string? _currentMusicPath;

    public GameAudioService(string? assetRoot = null)
    {
        _assetRoot = string.IsNullOrWhiteSpace(assetRoot)
            ? Path.Combine(AppContext.BaseDirectory, "Audio")
            : assetRoot;
    }

    public void EnterTitleScreen() => PlayMusic("Music", "title", loop: true);
    public void EnterRound() => PlayMusic("Music", "round", loop: true);
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

    private void PlayMusic(string category, string trackName, bool loop)
    {
        var path = ResolveAsset(category, trackName);
        if (path is null || !OperatingSystem.IsWindows())
            return;

        lock (_sync)
        {
            if (string.Equals(_currentMusicPath, path, StringComparison.OrdinalIgnoreCase))
                return;

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
            }
            catch
            {
                StopMusicCore();
            }
        }
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

    private string? ResolveAsset(string category, string name)
    {
        var directory = Path.Combine(_assetRoot, category);
        if (!Directory.Exists(directory))
            return null;

        return Directory.EnumerateFiles(directory)
            .FirstOrDefault(file =>
                string.Equals(Path.GetFileNameWithoutExtension(file), name, StringComparison.OrdinalIgnoreCase) &&
                SupportedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase));
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
