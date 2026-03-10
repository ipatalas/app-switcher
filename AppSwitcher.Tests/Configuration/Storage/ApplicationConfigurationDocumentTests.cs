using AppSwitcher.Configuration;
using AppSwitcher.Configuration.Storage;
using System.Windows.Input;
using Xunit;
using AwesomeAssertions;

namespace AppSwitcher.Tests.Configuration.Storage;

public class ApplicationConfigurationDocumentTests
{
    [Fact]
    public void ToApplicationConfiguration_MapsAllFields()
    {
        var doc = new ApplicationConfigurationDocument
        {
            Key = Key.W,
            ProcessPath = @"C:\tools\app.exe",
            CycleMode = CycleMode.NextWindow,
            StartIfNotRunning = true
        };

        var result = doc.ToApplicationConfiguration();

        result.Should()
            .BeEquivalentTo(new ApplicationConfiguration(
                Key: Key.W,
                ProcessPath: @"C:\tools\app.exe",
                CycleMode: CycleMode.NextWindow,
                StartIfNotRunning: true));
    }

    [Fact]
    public void FromApplicationConfiguration_MapsAllFields()
    {
        var config = new ApplicationConfiguration(Key.W, @"C:\tools\app.exe", CycleMode.Hide, true);

        var result = ApplicationConfigurationDocument.FromApplicationConfiguration(config);

        result.Should().BeEquivalentTo(new ApplicationConfigurationDocument
        {
            Key = Key.W,
            ProcessPath = @"C:\tools\app.exe",
            CycleMode = CycleMode.Hide,
            StartIfNotRunning = true
        });
    }

    [Theory]
    [InlineData(CycleMode.NextApp)]
    [InlineData(CycleMode.Hide)]
    [InlineData(CycleMode.NextWindow)]
    public void RoundTrip_PreservesAllData(CycleMode cycleMode)
    {
        var original = new ApplicationConfiguration(Key.C, @"C:\apps\tool.exe", cycleMode, false);

        var roundTripped = ApplicationConfigurationDocument
            .FromApplicationConfiguration(original)
            .ToApplicationConfiguration();

        roundTripped.Should().Be(original);
    }
}