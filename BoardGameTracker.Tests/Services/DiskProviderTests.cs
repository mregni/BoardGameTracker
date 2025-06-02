
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Disk;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class DiskProviderTests: IDisposable
{
    private readonly Mock<ILogger<DiskProvider>> _loggerMock;
        private readonly DiskProvider _diskProvider;
        private readonly string _testDirectory;
        private readonly List<string> _filesToCleanup;

        public DiskProviderTests()
        {
            _loggerMock = new Mock<ILogger<DiskProvider>>();
            _diskProvider = new DiskProvider(_loggerMock.Object);
            _testDirectory = Path.Combine(Path.GetTempPath(), "DiskProviderTests", Guid.NewGuid().ToString());
            _filesToCleanup = new List<string>();
            
            Directory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            foreach (var file in _filesToCleanup.Where(File.Exists))
            {
                File.Delete(file);
            }

            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [Fact]
        public void FileExists_ShouldReturnTrue_WhenFileExists()
        {
            var filePath = CreateTestFile("test.txt", "content");

            var result = _diskProvider.FileExists(filePath);

            result.Should().BeTrue();
        }

        [Fact]
        public void FileExists_ShouldReturnFalse_WhenFileDoesNotExist()
        {
            var filePath = Path.Combine(_testDirectory, "nonexistent.txt");

            var result = _diskProvider.FileExists(filePath);

            result.Should().BeFalse();
        }

        [Fact]
        public void FileExists_ShouldReturnFalse_WhenPathIsNull()
        {
            var result = _diskProvider.FileExists(null);

            result.Should().BeFalse();
        }

        [Fact]
        public void FileExists_ShouldReturnFalse_WhenPathIsEmpty()
        {
            var result = _diskProvider.FileExists("");

            result.Should().BeFalse();
        }

        [Fact]
        public void ReadAllText_ShouldReturnFileContent_WhenFileExists()
        {
            const string expectedContent = "This is test content";
            var filePath = CreateTestFile("test.txt", expectedContent);

            var result = _diskProvider.ReadAllText(filePath);

            result.Should().Be(expectedContent);
        }

        [Fact]
        public void ReadAllText_ShouldThrowException_WhenFileDoesNotExist()
        {
            var filePath = Path.Combine(_testDirectory, "nonexistent.txt");

            var action = () => _diskProvider.ReadAllText(filePath);

            action.Should().Throw<FileNotFoundException>();
        }

        [Fact]
        public void ReadAllText_ShouldReturnEmptyString_WhenFileIsEmpty()
        {
            var filePath = CreateTestFile("empty.txt", "");

            var result = _diskProvider.ReadAllText(filePath);

            result.Should().Be("");
        }

        [Theory]
        [InlineData("Simple text")]
        [InlineData("Text with\nnew lines")]
        [InlineData("Text with special chars: !@#$%^&*()")]
        [InlineData("Unicode text: éñçødé")]
        public void ReadAllText_ShouldReturnCorrectContent_WithDifferentTextTypes(string content)
        {
            var filePath = CreateTestFile("test.txt", content);

            var result = _diskProvider.ReadAllText(filePath);

            result.Should().Be(content);
        }

        [Fact]
        public void WriteAllText_ShouldCreateFileWithContent_WhenFileDoesNotExist()
        {
            var filePath = Path.Combine(_testDirectory, "new.txt");
            const string content = "New file content";
            _filesToCleanup.Add(filePath);

            _diskProvider.WriteAllText(filePath, content);

            File.Exists(filePath).Should().BeTrue();
            File.ReadAllText(filePath).Should().Be(content);
        }

        [Fact]
        public void WriteAllText_ShouldOverwriteFileContent_WhenFileExists()
        {
            var filePath = CreateTestFile("existing.txt", "old content");
            const string newContent = "new content";

            _diskProvider.WriteAllText(filePath, newContent);

            File.ReadAllText(filePath).Should().Be(newContent);
        }

        [Fact]
        public void WriteAllText_ShouldRemoveReadOnlyAttribute_WhenFileIsReadOnly()
        {
            var filePath = CreateTestFile("readonly.txt", "content");
            File.SetAttributes(filePath, FileAttributes.ReadOnly);

            _diskProvider.WriteAllText(filePath, "new content");

            var attributes = File.GetAttributes(filePath);
            attributes.HasFlag(FileAttributes.ReadOnly).Should().BeFalse();
            File.ReadAllText(filePath).Should().Be("new content");
        }

        [Fact]
        public async Task WriteFile_ShouldGenerateUniqueFileName_WhenCalled()
        {
            const string fileName = "test.jpg";
            var formFile = CreateMockFormFile(fileName, "content");
            const UploadFileType uploadType = UploadFileType.Profile;
            var expectedPath = uploadType.ConvertToPath();
            
            Directory.CreateDirectory(expectedPath);

            var result = await _diskProvider.WriteFile(formFile, uploadType);

            result.Should().NotBe(fileName);
            result.Should().Contain("_");
            
            var fullPath = Path.Combine(expectedPath, result);
            _filesToCleanup.Add(fullPath);
        }

        [Fact]
        public void DeleteFile_ShouldRemoveFile_WhenFileExists()
        {
            var filePath = CreateTestFile("delete.txt", "content");

            _diskProvider.DeleteFile(filePath);

            File.Exists(filePath).Should().BeFalse();
            VerifyLogInformation("Removing file {Path}", filePath);
        }

        [Fact]
        public void DeleteFile_ShouldNotThrow_WhenFileDoesNotExist()
        {
            var filePath = Path.Combine(_testDirectory, "nonexistent.txt");

            var action = () => _diskProvider.DeleteFile(filePath);

            action.Should().NotThrow();
            VerifyLogInformation("Removing file {Path}", filePath);
        }

        [Fact]
        public void DeleteFile_ShouldLogError_WhenIOExceptionOccurs()
        {
            var filePath = CreateTestFile("locked.txt", "content");
            
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            
            _diskProvider.DeleteFile(filePath);

            File.Exists(filePath).Should().BeTrue();
            VerifyLogInformation("Removing file {Path}", filePath);
            VerifyLogError("Can't delete file because it seems to be in use");
        }

        [Fact]
        public void EnsureFolder_ShouldCreateDirectory_WhenDirectoryDoesNotExist()
        {
            var dirPath = Path.Combine(_testDirectory, "newdir");

            _diskProvider.EnsureFolder(dirPath);

            Directory.Exists(dirPath).Should().BeTrue();
        }

        [Fact]
        public void EnsureFolder_ShouldNotThrow_WhenDirectoryAlreadyExists()
        {
            var dirPath = Path.Combine(_testDirectory, "existingdir");
            Directory.CreateDirectory(dirPath);

            var action = () => _diskProvider.EnsureFolder(dirPath);

            action.Should().NotThrow();
            Directory.Exists(dirPath).Should().BeTrue();
        }

        [Fact]
        public void EnsureFolder_ShouldCreateNestedDirectories_WhenParentDoesNotExist()
        {
            var dirPath = Path.Combine(_testDirectory, "parent", "child", "grandchild");

            _diskProvider.EnsureFolder(dirPath);

            Directory.Exists(dirPath).Should().BeTrue();
            Directory.Exists(Path.Combine(_testDirectory, "parent")).Should().BeTrue();
            Directory.Exists(Path.Combine(_testDirectory, "parent", "child")).Should().BeTrue();
        }

        [Theory]
        [InlineData("file1.txt", "content1")]
        [InlineData("file2.json", "{\"test\": \"value\"}")]
        [InlineData("file3.xml", "<root><item>test</item></root>")]
        public void WriteAllText_ShouldHandleDifferentFileTypes_WithVariousContent(string fileName, string content)
        {
            var filePath = Path.Combine(_testDirectory, fileName);
            _filesToCleanup.Add(filePath);

            _diskProvider.WriteAllText(filePath, content);

            File.Exists(filePath).Should().BeTrue();
            File.ReadAllText(filePath).Should().Be(content);
        }

        [Fact]
        public void DeleteFile_ShouldLogUnknownError_WhenUnexpectedExceptionOccurs()
        {
            var invalidPath = "Z:\\nonexistent\\invalid\\path\\file.txt";

            _diskProvider.DeleteFile(invalidPath);

            VerifyLogInformation("Removing file {Path}", invalidPath);
            VerifyLogError("Can't delete file because it seems to be in use");
        }

        private string CreateTestFile(string fileName, string content)
        {
            var filePath = Path.Combine(_testDirectory, fileName);
            File.WriteAllText(filePath, content);
            _filesToCleanup.Add(filePath);
            return filePath;
        }

        private static IFormFile CreateMockFormFile(string fileName, string content)
        {
            var formFileMock = new Mock<IFormFile>();
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            
            formFileMock.Setup(f => f.FileName).Returns(fileName);
            formFileMock.Setup(f => f.Length).Returns(stream.Length);
            formFileMock.Setup(f => f.OpenReadStream()).Returns(stream);
            formFileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                       .Returns((Stream target, CancellationToken token) => stream.CopyToAsync(target, token));

            return formFileMock.Object;
        }

        private void VerifyLogInformation(string message, params object[] args)
        {
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(message.Replace("{Path}", args[0].ToString()))),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        private void VerifyLogError(string message, params object[] args)
        {
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(message)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
}