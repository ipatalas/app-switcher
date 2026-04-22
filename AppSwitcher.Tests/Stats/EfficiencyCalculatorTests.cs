using AppSwitcher.Stats;
using AwesomeAssertions;
using Xunit;

namespace AppSwitcher.Tests.Stats;

public class EfficiencyCalculatorTests
{
    [Theory]
    [InlineData(0, 500 + 0 + 400)]    // log₂(1) = 0
    [InlineData(1, 500 + 150 + 400)]  // log₂(2) = 1 → 150ms
    [InlineData(3, 500 + 300 + 400)]  // log₂(4) = 2 → 300ms
    [InlineData(7, 500 + 450 + 400)]  // log₂(8) = 3 → 450ms
    public void AltTabTimeMs_ReturnsExpectedValue(int windowCount, int expectedMs)
    {
        var result = EfficiencyCalculator.AltTabTimeMs(windowCount);

        result.Should().Be(expectedMs);
    }

    [Theory]
    [InlineData(0, 900 - 350)]   // 900ms alt-tab - 350ms AppSwitcher = 550ms saved
    [InlineData(1, 1050 - 350)]  // 1050ms - 350ms = 700ms saved
    public void SavedMs_ReturnsAltTabMinusAppSwitcher(int windowCount, int expectedSavedMs)
    {
        var result = EfficiencyCalculator.SavedMs(windowCount);

        result.Should().Be(expectedSavedMs);
    }

    [Fact]
    public void SavedMs_NeverReturnsNegative()
    {
        // Even with 0 choices, saved time should be non-negative
        var result = EfficiencyCalculator.SavedMs(0);

        result.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void SavedMs_IncreasesWithMoreChoices()
    {
        var savedFor1 = EfficiencyCalculator.SavedMs(1);
        var savedFor10 = EfficiencyCalculator.SavedMs(10);

        savedFor10.Should().BeGreaterThan(savedFor1);
    }

    [Fact]
    public void AltTabTimeMs_IncludesAllThreeComponents()
    {
        // With 1 window: 500 (base) + 150×log₂(2) (scan) + 400 (taps) = 1050
        var result = EfficiencyCalculator.AltTabTimeMs(1);

        result.Should().Be(1050);
    }
}