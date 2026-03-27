using AppSwitcher.Utils;
using AwesomeAssertions;
using Xunit;

namespace AppSwitcher.Tests.Utils;

public class TitleSuffixHelperTests
{
    private readonly TitleSuffixHelper _sut = new();

    public class FindCommonSuffix
    {
        private readonly TitleSuffixHelper _sut = new();

        [Fact]
        public void ReturnsNull_WhenListIsEmpty()
        {
            _sut.FindCommonSuffix(Array.Empty<string>()).Should().BeNull();
        }

        [Fact]
        public void ReturnsNull_WhenSingleTitle()
        {
            _sut.FindCommonSuffix(["Only Title - App"]).Should().BeNull();
        }

        [Fact]
        public void ReturnsNull_WhenNoCommonCharacters()
        {
            _sut.FindCommonSuffix(["abc", "xyz"]).Should().BeNull();
        }

        [Fact]
        public void ReturnsNull_WhenCommonSuffixHasNoRecognisedSeparator()
        {
            // Common suffix ".docx" has no word separator prefix
            _sut.FindCommonSuffix(["Document1.docx", "Document2.docx"])
                .Should().BeNull();
        }

        [Fact]
        public void ReturnsNull_WhenAppNamePortionAfterSeparatorIsTooShort()
        {
            // Suffix would be " - X" — app name "X" is only 1 character
            _sut.FindCommonSuffix(["Hello - X", "World - X"]).Should().BeNull();
        }

        [Theory]
        [InlineData(" - ")]
        [InlineData(" — ")]
        [InlineData(" | ")]
        [InlineData(" : ")]
        public void ReturnsSuffix_ForAllRecognisedSeparators(string separator)
        {
            var suffix = $"{separator}MyApp";
            string[] titles = [$"Window One{suffix}", $"Window Two{suffix}"];

            _sut.FindCommonSuffix(titles).Should().Be(suffix);
        }

        [Fact]
        public void ReturnsSuffix_WhenAllTitlesShareAppNameSuffix()
        {
            var titles = new[]
            {
                "Welcome - Visual Studio Code",
                "README.md - Visual Studio Code",
                "src/main.cs - Visual Studio Code"
            };

            _sut.FindCommonSuffix(titles).Should().Be(" - Visual Studio Code");
        }

        [Fact]
        public void ReturnsSuffix_ForTwoTitles()
        {
            _sut.FindCommonSuffix(["Tab One - Vivaldi", "Tab Two - Vivaldi"])
                .Should().Be(" - Vivaldi");
        }
    }

    public class StripSuffix
    {
        private readonly TitleSuffixHelper _sut = new();

        [Fact]
        public void ReturnsTitleUnchanged_WhenSuffixIsNull()
        {
            _sut.StripSuffix("Hello - App", null).Should().Be("Hello - App");
        }

        [Fact]
        public void ReturnsTitleUnchanged_WhenSuffixNotPresent()
        {
            _sut.StripSuffix("Hello - App", " - Other").Should().Be("Hello - App");
        }

        [Fact]
        public void StripsSuffix_AndTrimsTrailingWhitespace()
        {
            _sut.StripSuffix("Hello - App", " - App").Should().Be("Hello");
        }

        [Fact]
        public void StripsSuffix_LeavingWindowTitlePortion()
        {
            _sut.StripSuffix("README.md - Visual Studio Code", " - Visual Studio Code")
                .Should().Be("README.md");
        }
    }
}
