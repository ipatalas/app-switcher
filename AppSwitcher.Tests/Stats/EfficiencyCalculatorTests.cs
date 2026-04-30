using AppSwitcher.Stats;
using AwesomeAssertions;
using Xunit;

namespace AppSwitcher.Tests.Stats;

public class EfficiencyCalculatorTests
{
    [Theory]
    [InlineData(0, 100 + 140 * 0)]  // 100ms
    [InlineData(1, 100 + 140 * 1)]  // 240ms
    [InlineData(3, 100 + 140 * 3)]  // 520ms
    [InlineData(7, 100 + 140 * 7)]  // 1080ms
    public void AltTabTimeMs_ReturnsExpectedValue(int windowCount, int expectedMs)
    {
        var result = EfficiencyCalculator.AltTabTimeMs(windowCount);

        result.Should().Be(expectedMs);
    }

    [Theory]
    [InlineData(1, 100, 240 - 100)]
    [InlineData(1, 200, 240 - 200)]
    [InlineData(3, 50, 520 - 50)]
    [InlineData(7, 80, 1080 - 80)]
    public void SavedMs_ReturnsAltTabMinusAppSwitcher(int windowCount, int actualDurationMs, int expectedSavedMs)
    {
        var result = EfficiencyCalculator.SavedMs(windowCount, actualDurationMs);

        result.Should().Be(expectedSavedMs);
    }

    [Fact]
    public void SavedMs_NeverReturnsNegative()
    {
        // Even with 0 choices, saved time should be non-negative
        var result = EfficiencyCalculator.SavedMs(0, 100);

        result.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void SavedMs_IncreasesWithMoreChoices()
    {
        var savedFor1 = EfficiencyCalculator.SavedMs(1, 100);
        var savedFor10 = EfficiencyCalculator.SavedMs(10, 100);

        savedFor10.Should().BeGreaterThan(savedFor1);
    }
}
