using System.Collections.Generic;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Extensions;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Extensions;

public class StringExtensionTests
{
[Fact]
        public void ToPersonTypeEnum_ShouldReturnArtist_WhenTypeIsArtistConstant()
        {
            const string type = Constants.Bgg.Artist;

            var result = type.ToPersonTypeEnum();

            result.Should().Be(PersonType.Artist);
        }

        [Fact]
        public void ToPersonTypeEnum_ShouldReturnDesigner_WhenTypeIsDesignerConstant()
        {
            const string type = Constants.Bgg.Designer;

            var result = type.ToPersonTypeEnum();

            result.Should().Be(PersonType.Designer);
        }

        [Fact]
        public void ToPersonTypeEnum_ShouldReturnPublisher_WhenTypeIsUnknownValue()
        {
            const string type = "unknown";

            var result = type.ToPersonTypeEnum();

            result.Should().Be(PersonType.Publisher);
        }

        [Fact]
        public void ToPersonTypeEnum_ShouldReturnPublisher_WhenTypeIsEmptyString()
        {
            const string type = "";

            var result = type.ToPersonTypeEnum();

            result.Should().Be(PersonType.Publisher);
        }

        [Fact]
        public void ToPersonTypeEnum_ShouldReturnPublisher_WhenTypeIsNull()
        {
            string? type = null;

            var result = type.ToPersonTypeEnum();

            result.Should().Be(PersonType.Publisher);
        }

        [Theory]
        [InlineData("publisher")]
        [InlineData("ARTIST")]
        [InlineData("designer")]
        [InlineData("random")]
        [InlineData("123")]
        [InlineData("special!@#")]
        public void ToPersonTypeEnum_ShouldReturnPublisher_WhenTypeDoesNotMatchExactConstants(string type)
        {
            var result = type.ToPersonTypeEnum();

            result.Should().Be(PersonType.Publisher);
        }

        [Fact]
        public void GenerateUniqueFileName_ShouldReturnUniqueFileName_WhenValidFileNameProvided()
        {
            const string fileName = "test.txt";

            var result = fileName.GenerateUniqueFileName();

            result.Should().StartWith("test_");
            result.Should().EndWith(".txt");
            result.Should().NotBe(fileName);
            result.Length.Should().BeGreaterThan(fileName.Length);
        }

        [Fact]
        public void GenerateUniqueFileName_ShouldHandleFileWithMultipleDots()
        {
            const string fileName = "test.backup.txt";

            var result = fileName.GenerateUniqueFileName();

            result.Should().StartWith("test.backup_");
            result.Should().EndWith(".txt");
            result.Should().NotBe(fileName);
        }

        [Fact]
        public void GenerateUniqueFileName_ShouldHandleEmptyFileName()
        {
            const string fileName = "";

            var result = fileName.GenerateUniqueFileName();

            result.Should().StartWith("_");
            result.Should().NotBeEmpty();
        }

        [Fact]
        public void GenerateUniqueFileName_ShouldHandleFileNameWithOnlyExtension()
        {
            const string fileName = ".txt";

            var result = fileName.GenerateUniqueFileName();

            result.Should().StartWith("_");
            result.Should().EndWith(".txt");
        }

        [Theory]
        [InlineData("document.pdf", ".pdf")]
        [InlineData("image.jpg", ".jpg")]
        [InlineData("archive.tar.gz", ".gz")]
        [InlineData("script.sh", ".sh")]
        public void GenerateUniqueFileName_ShouldPreserveExtension_WithDifferentExtensions(string fileName, string expectedExtension)
        {
            var result = fileName.GenerateUniqueFileName();

            result.Should().EndWith(expectedExtension);
            result.Should().NotBe(fileName);
        }

        [Fact]
        public void GenerateUniqueFileName_ShouldGenerateDifferentResults_WhenCalledMultipleTimes()
        {
            const string fileName = "test.txt";
            var results = new List<string>();

            for (var i = 0; i < 10; i++)
            {
                results.Add(fileName.GenerateUniqueFileName());
            }

            results.Should().OnlyHaveUniqueItems();
            results.Should().AllSatisfy(r => r.Should().StartWith("test_"));
            results.Should().AllSatisfy(r => r.Should().EndWith(".txt"));
        }

        [Fact]
        public void FirstCharToUpper_ShouldReturnEmptyString_WhenInputIsNull()
        {
            string? input = null;

            var result = input.FirstCharToUpper();

            result.Should().Be(string.Empty);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("a", "A")]
        [InlineData("A", "A")]
        [InlineData("hello world", "Hello world")]
        [InlineData("Hello world", "Hello world")]
        [InlineData("hELLo WoRLd", "HELLo WoRLd")]
        [InlineData("123test", "123test")]
        [InlineData("@test", "@test")]
        [InlineData("HELLO", "HELLO")]
        [InlineData("hELLO", "HELLO")]
        [InlineData("ümlaut test", "Ümlaut test")]
        [InlineData(" test", " test")]
        public void FirstCharToUpper_ShouldReturnEmptyString_WhenInputIsEmpty(string input, string output)
        {
            var result = input.FirstCharToUpper();

            result.Should().Be(output);
        }
} 