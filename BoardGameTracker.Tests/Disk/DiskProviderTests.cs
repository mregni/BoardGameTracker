
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BoardGameTracker.Core.Disk;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Disk;

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

        private void VerifyNoOtherCalls()
        {
            _loggerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void DeleteFile_ShouldRemoveFile_WhenFileExists()
        {
            var filePath = CreateTestFile("delete.txt", "content");

            _diskProvider.DeleteFile(filePath);

            File.Exists(filePath).Should().BeFalse();
            VerifyFileRemovalLogged(filePath);
            VerifyNoOtherCalls();
        }

        [Fact]
        public void DeleteFile_ShouldNotThrow_WhenFileDoesNotExist()
        {
            var filePath = Path.Combine(_testDirectory, "nonexistent.txt");

            var action = () => _diskProvider.DeleteFile(filePath);

            action.Should().NotThrow();
            VerifyFileRemovalLogged(filePath);
            VerifyNoOtherCalls();
        }

        [Fact]
        public void EnsureFolder_ShouldCreateDirectory_WhenDirectoryDoesNotExist()
        {
            var dirPath = Path.Combine(_testDirectory, "newdir");

            _diskProvider.EnsureFolder(dirPath);

            Directory.Exists(dirPath).Should().BeTrue();
            VerifyNoOtherCalls();
        }

        [Fact]
        public void EnsureFolder_ShouldNotThrow_WhenDirectoryAlreadyExists()
        {
            var dirPath = Path.Combine(_testDirectory, "existingdir");
            Directory.CreateDirectory(dirPath);

            var action = () => _diskProvider.EnsureFolder(dirPath);

            action.Should().NotThrow();
            Directory.Exists(dirPath).Should().BeTrue();
            VerifyNoOtherCalls();
        }

        [Fact]
        public void EnsureFolder_ShouldCreateNestedDirectories_WhenParentDoesNotExist()
        {
            var dirPath = Path.Combine(_testDirectory, "parent", "child", "grandchild");

            _diskProvider.EnsureFolder(dirPath);

            Directory.Exists(dirPath).Should().BeTrue();
            Directory.Exists(Path.Combine(_testDirectory, "parent")).Should().BeTrue();
            Directory.Exists(Path.Combine(_testDirectory, "parent", "child")).Should().BeTrue();
            VerifyNoOtherCalls();
        }

        [Fact]
        public void DeleteFile_ShouldNotThrowAndLogError_WhenDirectoryDoesNotExist()
        {
            var invalidPath = Path.Combine("Z", "nonexistent-path", "file.txt");

            var action = () => _diskProvider.DeleteFile(invalidPath);

            action.Should().NotThrow();
            VerifyFileRemovalLogged(invalidPath);
            _loggerMock.VerifyLog(LogLevel.Error, "Can't delete file because it seems to be in use");
            VerifyNoOtherCalls();
        }

        [Fact]
        public async Task WriteFile_WithStream_ShouldCopyStreamContentToUniqueFile()
        {
            var content = new byte[] { 1, 2, 3, 4, 5 };
            using var stream = new MemoryStream(content);

            var result = await _diskProvider.WriteFile(stream, "data.bin", _testDirectory);

            result.Should().MatchRegex(@"^data_\w+\.\w+\.bin$");
            File.ReadAllBytes(Path.Combine(_testDirectory, result)).Should().Equal(content);
            VerifyNoOtherCalls();
        }

        [Fact]
        public async Task WriteFile_ShouldRemoveThePartialFile_WhenTheCopyFails()
        {
            await using var stream = new FailingStream();

            var action = async () => await _diskProvider.WriteFile(stream, "data.bin", _testDirectory);

            await action.Should().ThrowAsync<IOException>().WithMessage("connection reset");
            Directory.EnumerateFiles(_testDirectory).Should().BeEmpty();
            VerifyNoOtherCalls();
        }

        [Fact]
        public async Task WriteFile_ShouldNotOverwriteAnEarlierUpload_WithTheSameName()
        {
            using var first = new MemoryStream([1]);
            using var second = new MemoryStream([2]);

            var firstName = await _diskProvider.WriteFile(first, "data.bin", _testDirectory);
            var secondName = await _diskProvider.WriteFile(second, "data.bin", _testDirectory);

            secondName.Should().NotBe(firstName);
            File.ReadAllBytes(Path.Combine(_testDirectory, firstName)).Should().Equal(1);
            File.ReadAllBytes(Path.Combine(_testDirectory, secondName)).Should().Equal(2);
            VerifyNoOtherCalls();
        }

        [Fact]
        public void DeleteFile_ShouldLogAnUnknownError_WhenThePathIsADirectory()
        {
            var dirPath = Path.Combine(_testDirectory, "folder");
            Directory.CreateDirectory(dirPath);

            var action = () => _diskProvider.DeleteFile(dirPath);

            action.Should().NotThrow();
            Directory.Exists(dirPath).Should().BeTrue();
            VerifyFileRemovalLogged(dirPath);
            _loggerMock.VerifyLog(LogLevel.Error, $"Unknown error occurred while deleting file {dirPath}");
            VerifyNoOtherCalls();
        }

        [Fact]
        public void ClearFolder_ShouldDeleteAllFiles_AndLeaveSubdirectories()
        {
            var first = CreateTestFile("one.txt", "1");
            var second = CreateTestFile("two.txt", "2");
            var subDir = Path.Combine(_testDirectory, "sub");
            Directory.CreateDirectory(subDir);
            var nestedFile = Path.Combine(subDir, "nested.txt");
            File.WriteAllText(nestedFile, "nested");

            _diskProvider.ClearFolder(_testDirectory);

            Directory.EnumerateFiles(_testDirectory).Should().BeEmpty();
            Directory.Exists(subDir).Should().BeTrue();
            File.Exists(nestedFile).Should().BeTrue();
            VerifyFileRemovalLogged(first);
            VerifyFileRemovalLogged(second);
            VerifyNoOtherCalls();
        }

        [Fact]
        public void ClearFolder_ShouldNotThrow_WhenDirectoryDoesNotExist()
        {
            var missingDir = Path.Combine(_testDirectory, "missing");

            var action = () => _diskProvider.ClearFolder(missingDir);

            action.Should().NotThrow();
            VerifyNoOtherCalls();
        }

        [Fact]
        public void FileExists_ShouldReturnTrue_WhenFileExists()
        {
            var filePath = CreateTestFile("exists.txt", "content");

            _diskProvider.FileExists(filePath).Should().BeTrue();
            VerifyNoOtherCalls();
        }

        [Fact]
        public void FileExists_ShouldReturnFalse_WhenFileDoesNotExist()
        {
            var filePath = Path.Combine(_testDirectory, "missing.txt");

            _diskProvider.FileExists(filePath).Should().BeFalse();
            VerifyNoOtherCalls();
        }

        [Fact]
        public void OpenRead_ShouldReturnStreamWithFileContent()
        {
            var filePath = CreateTestFile("read.txt", "file-content");

            using var stream = _diskProvider.OpenRead(filePath);
            using var reader = new StreamReader(stream);

            reader.ReadToEnd().Should().Be("file-content");
            VerifyNoOtherCalls();
        }

        private string CreateTestFile(string fileName, string content)
        {
            var filePath = Path.Combine(_testDirectory, fileName);
            File.WriteAllText(filePath, content);
            _filesToCleanup.Add(filePath);
            return filePath;
        }

        private void VerifyFileRemovalLogged(string path)
        {
            _loggerMock.VerifyLog(LogLevel.Information, $"Removing file {path}");
        }

        private sealed class FailingStream : MemoryStream
        {
            public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) =>
                throw new IOException("connection reset");
        }
}
