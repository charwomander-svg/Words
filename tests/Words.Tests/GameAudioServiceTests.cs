using Words.Xbox.Audio;

namespace Words.Tests;

public class GameAudioServiceTests
{
    [Fact]
    public void ResolveAsset_ReturnsExactMatchWhenAvailable()
    {
        using var scope = new TempDirectoryScope();
        var music = scope.CreateFile("Music", "title.mp3");
        scope.CreateFile("Music", "title-2.wav");
        var service = new GameAudioService(scope.Root, new Random(0));

        var resolved = service.ResolveAsset("Music", "title");

        Assert.Equal(music, resolved);
    }

    [Fact]
    public void ResolveAsset_CyclesRandomlyAmongPrefixMatches()
    {
        using var scope = new TempDirectoryScope();
        scope.CreateFile("Music", "stage1.mp3");
        var expected = scope.CreateFile("Music", "stage2.wav");
        scope.CreateFile("Music", "stage7.flac");
        var service = new GameAudioService(scope.Root, new FixedRandom(1));

        var resolved = service.ResolveAsset("Music", "stage");

        Assert.Equal(expected, resolved);
    }

    [Fact]
    public void ResolveAsset_ReturnsNullWhenNothingMatches()
    {
        using var scope = new TempDirectoryScope();
        scope.CreateFile("Music", "unrelated.mp3");
        var service = new GameAudioService(scope.Root, new Random(0));

        var resolved = service.ResolveAsset("Music", "stage");

        Assert.Null(resolved);
    }

    private sealed class FixedRandom(int value) : Random
    {
        public override int Next(int maxValue) => value;
    }

    private sealed class TempDirectoryScope : IDisposable
    {
        public TempDirectoryScope()
        {
            Root = Path.Combine(Path.GetTempPath(), "Words.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public string CreateFile(string category, string fileName)
        {
            var directory = Path.Combine(Root, category);
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, fileName);
            File.WriteAllText(path, "test");
            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
    }
}
