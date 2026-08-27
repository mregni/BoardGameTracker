
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BoardGameTracker.Core.Disk;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
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
            _filesToCleanup = [];
            
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
            
            GC.SuppressFinalize(this);
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

        [Fact]
        public void DeleteFile_ShouldNotThrowAndLogError_WhenDirectoryDoesNotExist()
        {
            var invalidPath = Path.Combine("Z", "nonexistent-path", "file.txt");

            var action = () => _diskProvider.DeleteFile(invalidPath);

            action.Should().NotThrow();
            VerifyLogInformation("Removing file {Path}", invalidPath);
            VerifyErrorLogged();
        }

        [Fact]
        public async Task WriteFile_WithImage_ShouldWriteUniqueFileAndReturnItsName()
        {
            using var image = new Image<Rgba32>(10, 10);

            var result = await _diskProvider.WriteFile(image, "picture.jpg", _testDirectory);

            result.Should().NotBe("picture.jpg");
            result.Should().StartWith("picture_");
            result.Should().EndWith(".jpg");
            File.Exists(Path.Combine(_testDirectory, result)).Should().BeTrue();
        }

        [Fact]
        public async Task WriteFile_WithImageAndEncoder_ShouldWriteFileUsingEncoder()
        {
            using var image = new Image<Rgba32>(10, 10);

            var result = await _diskProvider.WriteFile(image, "picture.webp", _testDirectory, new WebpEncoder());

            result.Should().EndWith(".webp");
            File.Exists(Path.Combine(_testDirectory, result)).Should().BeTrue();
            new FileInfo(Path.Combine(_testDirectory, result)).Length.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task WriteFile_WithStream_ShouldCopyStreamContentToUniqueFile()
        {
            var content = new byte[] { 1, 2, 3, 4, 5 };
            using var stream = new MemoryStream(content);

            var result = await _diskProvider.WriteFile(stream, "data.bin", _testDirectory);

            result.Should().NotBe("data.bin");
            result.Should().EndWith(".bin");
            File.ReadAllBytes(Path.Combine(_testDirectory, result)).Should().Equal(content);
        }

        [Fact]
        public void ClearFolder_ShouldDeleteAllFiles_AndLeaveSubdirectories()
        {
            CreateTestFile("one.txt", "1");
            CreateTestFile("two.txt", "2");
            var subDir = Path.Combine(_testDirectory, "sub");
            Directory.CreateDirectory(subDir);
            var nestedFile = Path.Combine(subDir, "nested.txt");
            File.WriteAllText(nestedFile, "nested");

            _diskProvider.ClearFolder(_testDirectory);

            Directory.EnumerateFiles(_testDirectory).Should().BeEmpty();
            Directory.Exists(subDir).Should().BeTrue();
            File.Exists(nestedFile).Should().BeTrue();
        }

        [Fact]
        public void ClearFolder_ShouldNotThrow_WhenDirectoryDoesNotExist()
        {
            var missingDir = Path.Combine(_testDirectory, "missing");

            var action = () => _diskProvider.ClearFolder(missingDir);

            action.Should().NotThrow();
        }

        [Fact]
        public void FileExists_ShouldReturnTrue_WhenFileExists()
        {
            var filePath = CreateTestFile("exists.txt", "content");

            _diskProvider.FileExists(filePath).Should().BeTrue();
        }

        [Fact]
        public void FileExists_ShouldReturnFalse_WhenFileDoesNotExist()
        {
            var filePath = Path.Combine(_testDirectory, "missing.txt");

            _diskProvider.FileExists(filePath).Should().BeFalse();
        }

        [Fact]
        public void OpenRead_ShouldReturnStreamWithFileContent()
        {
            var filePath = CreateTestFile("read.txt", "file-content");

            using var stream = _diskProvider.OpenRead(filePath);
            using var reader = new StreamReader(stream);

            reader.ReadToEnd().Should().Be("file-content");
        }

        private string CreateTestFile(string fileName, string content)
        {
            var filePath = Path.Combine(_testDirectory, fileName);
            File.WriteAllText(filePath, content);
            _filesToCleanup.Add(filePath);
            return filePath;
        }

        private void VerifyLogInformation(string message, params object[] args)
        {
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message.Replace("{Path}", args[0].ToString()!))),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        private void VerifyErrorLogged()
        {
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
}