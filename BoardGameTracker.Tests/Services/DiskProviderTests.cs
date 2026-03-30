
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
        public void DeleteFile_ShouldLogUnknownError_WhenUnexpectedExceptionOccurs()
        {
            var invalidPath =Path.Combine("Z", "nonexistent-path", "file.txt");

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

        private void VerifyLogError(string message, params object[] args)
        {
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
}