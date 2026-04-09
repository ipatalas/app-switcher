using AppSwitcher.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO;
using System.Windows.Input;
using Xunit;
using AwesomeAssertions;
using AppConfig = AppSwitcher.Configuration.Configuration;

namespace AppSwitcher.Tests.Configuration;

public class ConfigurationValidatorTests
{
    private readonly ConfigurationValidator _sut = new(NullLogger<ConfigurationValidator>.Instance);

    private static AppConfig MakeConfig(Key modifier, params ApplicationConfiguration[] apps) =>
        new(modifier, [.. apps], true, AppThemeSetting.System, true, 1000, true);

    private static ApplicationConfiguration MakeApp(Key key, string processPath) =>
        new(key, processPath, CycleMode.NextApp, false);

    private static ApplicationConfiguration MakePackagedApp(Key key, string exeFilename, string? aumid) =>
        new(key, exeFilename, CycleMode.NextApp, false, ApplicationType.Packaged, aumid);

    [Fact]
    public void ValidateAndLog_ReturnsSuccess_WhenNoApplicationsConfigured()
    {
        var result = _sut.ValidateAndLog(MakeConfig(Key.LeftCtrl));

        result.Status.Should().Be(ValidationResultStatus.Success);
    }

    [Theory]
    [InlineData(Key.LeftCtrl)]
    [InlineData(Key.LeftAlt)]
    [InlineData(Key.LeftShift)]
    [InlineData(Key.LWin)]
    [InlineData(Key.RightCtrl)]
    [InlineData(Key.RightAlt)]
    [InlineData(Key.Apps)]
    [InlineData(Key.RightShift)]
    [InlineData(Key.Capital)]
    public void ValidateAndLog_ReturnsSuccess_ForEachValidModifier(Key modifier)
    {
        var result = _sut.ValidateAndLog(MakeConfig(modifier));

        result.Status.Should().Be(ValidationResultStatus.Success);
    }

    [Theory]
    [InlineData(Key.A)]
    [InlineData(Key.B)]
    [InlineData(Key.None)]
    [InlineData(Key.F1)]
    [InlineData(Key.D0)]
    [InlineData(Key.Return)]
    public void ValidateAndLog_ReturnsError_WhenModifierIsNotInAllowedSet(Key modifier)
    {
        var result = _sut.ValidateAndLog(MakeConfig(modifier));

        result.Status.Should().Be(ValidationResultStatus.Error);
        result.Message.Should().Contain("Invalid modifier key");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAndLog_ReturnsError_WhenProcessPathIsEmpty(string processPath)
    {
        var result = _sut.ValidateAndLog(MakeConfig(Key.LeftCtrl, MakeApp(Key.A, processPath)));

        result.Status.Should().Be(ValidationResultStatus.Error);
        result.Message.Should().Contain("process path missing or invalid");
    }

    [Fact]
    public void ValidateAndLog_ReturnsError_WhenProcessFileDoesNotExist()
    {
        var result = _sut.ValidateAndLog(
            MakeConfig(Key.LeftCtrl, MakeApp(Key.A, @"C:\nonexistent\fake_app_12345.exe")));

        result.Status.Should().Be(ValidationResultStatus.Error);
        result.Message.Should().Contain("process path missing or invalid");
    }

    [Theory]
    [InlineData(Key.None)]
    [InlineData(Key.D1)]
    [InlineData(Key.F1)]
    [InlineData(Key.Return)]
    [InlineData(Key.Space)]
    public void ValidateAndLog_ReturnsError_WhenApplicationKeyIsNotALetter(Key key)
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var result = _sut.ValidateAndLog(MakeConfig(Key.LeftCtrl, MakeApp(key, tempFile)));

            result.Status.Should().Be(ValidationResultStatus.Error);
            result.Message.Should().Contain("single letter");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(Key.A)]
    [InlineData(Key.M)]
    [InlineData(Key.Z)]
    public void ValidateAndLog_ReturnsSuccess_WhenApplicationKeyIsALetter(Key key)
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var result = _sut.ValidateAndLog(MakeConfig(Key.LeftCtrl, MakeApp(key, tempFile)));

            result.Status.Should().Be(ValidationResultStatus.Success);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ValidateAndLog_IncludesProcessName_InErrorResult_WhenApplicationKeyIsInvalid()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var result = _sut.ValidateAndLog(MakeConfig(Key.LeftCtrl, MakeApp(Key.D1, tempFile)));

            result.Process.Should().Be(Path.GetFileName(tempFile));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ValidateAndLog_ReturnsFirstError_WhenMultipleAppsAreInvalid()
    {
        // Validation is short-circuit: returns on first error found
        var result = _sut.ValidateAndLog(MakeConfig(Key.LeftCtrl,
            MakeApp(Key.A, "A"),
            MakeApp(Key.B, "B")));

        result.Status.Should().Be(ValidationResultStatus.Error);
        result.Message.Should().Contain("process path missing or invalid");
        result.Process.Should().Be("A");
    }

    [Fact]
    public void ValidateAndLog_ReturnsSuccess_ForPackagedApp_WithValidAumid()
    {
        var app = MakePackagedApp(Key.T, "WindowsTerminal.exe", "Microsoft.WindowsTerminal_8wekyb3d8bbwe!App");

        var result = _sut.ValidateAndLog(MakeConfig(Key.LeftCtrl, app));

        result.Status.Should().Be(ValidationResultStatus.Success);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAndLog_ReturnsError_ForPackagedApp_WhenAumidIsMissingOrWhitespace(string? aumid)
    {
        var app = MakePackagedApp(Key.T, "WindowsTerminal.exe", aumid);

        var result = _sut.ValidateAndLog(MakeConfig(Key.LeftCtrl, app));

        result.Status.Should().Be(ValidationResultStatus.Error);
        result.Message.Should().Contain("AUMID");
    }

    [Fact]
    public void ValidateAndLog_DoesNotCheckFileExistence_ForPackagedApp()
    {
        var app = MakePackagedApp(Key.T, "C:\\WindowsTerminal.exe", "Microsoft.WindowsTerminal_8wekyb3d8bbwe!App");

        var result = _sut.ValidateAndLog(MakeConfig(Key.LeftCtrl, app));

        result.Status.Should().Be(ValidationResultStatus.Success);
    }
}
