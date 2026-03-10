using AppSwitcher.Utils;
using System.IO;
using Xunit;
using AwesomeAssertions;

namespace AppSwitcher.Tests.Utils;

public class AppLocatorTests
{
    private sealed class FakeProcessPathExtractor : IProcessPathExtractor
    {
        public string? ImageNameResult { get; set; }
        public string? RegistryResult { get; set; }

        public string? GetProcessImageName(uint processId) => ImageNameResult;
        public string? GetPathFromRegistry(string processNameWithExtension) => RegistryResult;
    }

    private readonly FakeProcessPathExtractor _fake = new();
    private AppLocator CreateSut() => new(_fake);

    // ── Strategy 1: rooted path returned as-is ──────────────────────────────

    [Theory]
    [InlineData(@"C:\Windows\System32\notepad.exe")]
    [InlineData(@"D:\tools\my app\app.exe")]
    [InlineData(@"\\server\share\tool.exe")]
    public void FindExecutablePath_ReturnsPath_WhenInputIsRooted(string rootedPath)
    {
        var result = CreateSut().FindExecutablePath(rootedPath);

        result.Should().Be(rootedPath);
    }

    // ── Strategy 2: running process found via image name ────────────────────
    // (Strategy 2 calls Process.GetProcessesByName + processPathExtractor.GetProcessImageName.
    //  We can only exercise the extractor's return value for a process that is
    //  actually running; we test the "not found" branch here deterministically.)

    [Fact]
    public void FindExecutablePath_ReturnsNull_WhenProcessNotRunning_AndRegistryReturnsNull_AndNotOnPath()
    {
        _fake.RegistryResult = null;

        // Use a name that is virtually guaranteed not to be running
        var result = CreateSut().FindExecutablePath("__nonexistent_process__.exe");

        result.Should().BeNull();
    }

    // ── Strategy 4: registry lookup ─────────────────────────────────────────

    [Fact]
    public void FindExecutablePath_ReturnsRegistryPath_WhenProcessNotRunning_AndRegistryHasEntry()
    {
        // Create a real temp file so the validator inside GetPathFromRegistry passes
        var tempFile = Path.GetTempFileName();
        try
        {
            _fake.RegistryResult = tempFile;

            var result = CreateSut().FindExecutablePath("some-app.exe");

            result.Should().Be(tempFile);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // ── Strategy 5: PATH environment variable search ────────────────────────

    [Fact]
    public void FindExecutablePath_ReturnsFullPath_WhenExecutableExistsOnEnvPath()
    {
        // Add a temp directory we own to PATH for the duration of this test,
        // then place a fake executable there and verify it is discovered.
        var tempDir = Directory.CreateTempSubdirectory("AppLocatorTest_").FullName;
        var originalPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        try
        {
            Environment.SetEnvironmentVariable("PATH", tempDir + ";" + originalPath);

            var fileName = $"appswitcher_test_{Guid.NewGuid():N}.exe";
            var fullPath = Path.Combine(tempDir, fileName);
            File.WriteAllBytes(fullPath, []);

            _fake.RegistryResult = null;

            // AppLocator reads PATH at construction time, so create it after setting PATH
            var result = new AppLocator(_fake).FindExecutablePath(fileName);

            result.Should().Be(fullPath);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", originalPath);
            Directory.Delete(tempDir, recursive: true);
        }
    }
}