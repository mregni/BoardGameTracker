using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Specification;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Disk.Interfaces;
using BoardGameTracker.Core.GameNights.Specifications;
using BoardGameTracker.Core.Manuals;
using BoardGameTracker.Core.Manuals.Specifications;
using BoardGameTracker.Core.Rag.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Manuals;

public class ManualServiceTests
{
    private static readonly DateTime Now = new(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc);
    private static readonly GameNight OtherNight = NewNight();

    private readonly Mock<IRepository<Manual>> _manualRepositoryMock;
    private readonly Mock<IDiskProvider> _diskProviderMock;
    private readonly Mock<IReadRepository<GameNight>> _gameNightRepositoryMock;
    private readonly Mock<IConfigRepository> _configRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IManualIndexingQueue> _indexingQueueMock;
    private readonly Mock<IPdfPageRenderer> _pageRendererMock;
    private readonly Mock<IEnvironmentProvider> _environmentProviderMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();
    private readonly CancellationToken _token = new CancellationTokenSource().Token;
    private readonly ManualService _manualService;

    public ManualServiceTests()
    {
        _manualRepositoryMock = new Mock<IRepository<Manual>>();
        _diskProviderMock = new Mock<IDiskProvider>();
        _gameNightRepositoryMock = new Mock<IReadRepository<GameNight>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _indexingQueueMock = new Mock<IManualIndexingQueue>();
        _pageRendererMock = new Mock<IPdfPageRenderer>();
        _environmentProviderMock = new Mock<IEnvironmentProvider>();

        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(Now);
        _configRepositoryMock
            .Setup(x => x.GetConfigValueOrDefaultAsync(BoardGameTracker.Common.Constants.AppConfig.GameNightsEnabled, true))
            .ReturnsAsync(true);
        _manualService = new ManualService(
            _manualRepositoryMock.Object,
            _diskProviderMock.Object,
            _gameNightRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _indexingQueueMock.Object,
            _pageRendererMock.Object,
            _environmentProviderMock.Object,
            _dateTimeProviderMock.Object,
            _configRepositoryMock.Object,
            NullLogger<ManualService>.Instance);
    }

    private void VerifyNoOtherCalls()
    {
        _manualRepositoryMock.VerifyNoOtherCalls();
        _diskProviderMock.VerifyNoOtherCalls();
        _gameNightRepositoryMock.VerifyNoOtherCalls();
        _configRepositoryMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
        _indexingQueueMock.VerifyNoOtherCalls();
        _pageRendererMock.VerifyNoOtherCalls();
        _environmentProviderMock.VerifyNoOtherCalls();
        _dateTimeProviderMock.VerifyNoOtherCalls();
    }

    private void VerifyGameNightsEnabledChecked(int times = 1)
    {
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(BoardGameTracker.Common.Constants.AppConfig.GameNightsEnabled, true), Times.Exactly(times));
    }

    private static FormFile CreateFormFile(string fileName = "rulebook.pdf", string contentType = "application/pdf", long length = 1024)
    {
        var bytes = new byte[Math.Max(Math.Min(length, 1024), 8)];
        "%PDF-1.7"u8.CopyTo(bytes);
        return CreateFormFile(bytes, fileName, contentType, length);
    }

    private static FormFile CreateFormFile(byte[] content, string fileName = "rulebook.pdf", string contentType = "application/pdf", long? length = null)
    {
        return new FormFile(new MemoryStream(content), 0, length ?? content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static FormFile CreatePdfWithSignatureAt(int offset)
    {
        var bytes = new byte[2048];
        "%PDF-"u8.CopyTo(bytes.AsSpan(offset));
        return CreateFormFile(bytes);
    }

    private static Manual CreateManual(int id, int gameId, string title = "rulebook.pdf")
    {
        return new Manual(title, $"stored-{title}", "application/pdf", 1024, gameId, Now) { Id = id };
    }

    private static GameNight NewNight() => GameNight.Create("Night", "", Now, 1, 1);

    private static bool ForNight(ISingleResultSpecification<GameNight> spec, GameNight night) =>
        spec.IsSatisfiedBy(night) && !spec.IsSatisfiedBy(OtherNight);

    private static string Physical(string storedFileName) => PathHelper.MapManualFileToPhysical(storedFileName);

    private void SetupWrite(string fileName)
    {
        _diskProviderMock
            .Setup(x => x.WriteFile(It.IsAny<Stream>(), fileName, PathHelper.FullManualsPath))
            .ReturnsAsync($"stored-{fileName}");
    }

    private void VerifyWritten(string fileName)
    {
        _diskProviderMock.Verify(x => x.WriteFile(It.IsAny<Stream>(), fileName, PathHelper.FullManualsPath), Times.Once);
    }

    private void VerifySaved(List<Manual> manuals)
    {
        _manualRepositoryMock.Verify(x => x.CreateRangeAsync(manuals), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
        _environmentProviderMock.Verify(x => x.RagEnabled, Times.Once);
        _dateTimeProviderMock.Verify(x => x.UtcNow, Times.Exactly(manuals.Count));
    }

    [Fact]
    public async Task UploadManuals_ShouldWriteFilesAndPersist_WhenFilesAreValid()
    {
        var files = new List<IFormFile> { CreateFormFile("a.pdf"), CreateFormFile("b.PDF", "Application/PDF", 2048) };
        SetupWrite("a.pdf");
        SetupWrite("b.PDF");

        var result = await _manualService.UploadManuals(7, files);

        result.Select(m => m.Title).Should().Equal("a.pdf", "b.PDF");
        result.Select(m => m.StoredFileName).Should().Equal("stored-a.pdf", "stored-b.PDF");
        result.Select(m => m.FileSizeBytes).Should().Equal(1024, 2048);
        result.Should().OnlyContain(m => m.GameId == 7 && m.ContentType == "application/pdf" && m.UploadDate == Now);

        VerifyWritten("a.pdf");
        VerifyWritten("b.PDF");
        VerifySaved(result);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadManuals_ShouldThrow_WhenNoFilesProvided()
    {
        var act = async () => await _manualService.UploadManuals(1, new List<IFormFile>());

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.ManualMissing);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadManuals_ShouldThrowAndWriteNothing_WhenAPdfNamedFileHasNoPdfSignature()
    {
        var disguised = CreateFormFile("<html><script>alert(1)</script></html>"u8.ToArray(), "evil.pdf");

        var act = async () => await _manualService.UploadManuals(1, [disguised]);

        var thrown = await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.ManualNotPdf);
        thrown.Which.Parameters.Should().Equal(new Dictionary<string, object> { ["fileName"] = "evil.pdf" });
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadManuals_ShouldFindTheSignature_AtTheEndOfTheHeaderWindow()
    {
        SetupWrite("rulebook.pdf");

        var result = await _manualService.UploadManuals(1, [CreatePdfWithSignatureAt(1019)]);

        result.Should().ContainSingle().Which.Title.Should().Be("rulebook.pdf");
        VerifyWritten("rulebook.pdf");
        VerifySaved(result);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadManuals_ShouldRefuseASignature_BehindTheHeaderWindow()
    {
        var act = async () => await _manualService.UploadManuals(1, [CreatePdfWithSignatureAt(1020)]);

        var thrown = await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.ManualNotPdf);
        thrown.Which.Parameters.Should().Equal(new Dictionary<string, object> { ["fileName"] = "rulebook.pdf" });
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadManuals_ShouldThrowAndWriteNothing_WhenAFileIsNotPdf()
    {
        var files = new List<IFormFile> { CreateFormFile("a.pdf"), CreateFormFile("b.txt", "text/plain") };

        var act = async () => await _manualService.UploadManuals(1, files);

        var thrown = await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.ManualNotPdf);
        thrown.Which.Parameters.Should().Equal(new Dictionary<string, object> { ["fileName"] = "b.txt" });
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("rulebook.txt", "application/pdf", 1024L, Constants.Errors.ManualNotPdf)]
    [InlineData("rulebook.pdf", "text/plain", 1024L, Constants.Errors.ManualNotPdf)]
    [InlineData("empty.pdf", "application/pdf", 0L, Constants.Errors.ManualEmpty)]
    public async Task UploadManuals_ShouldThrowAndWriteNothing_WhenFileIsInvalid(
        string fileName, string contentType, long length, string expectedMessage)
    {
        var files = new List<IFormFile> { CreateFormFile(fileName, contentType, length) };

        var act = async () => await _manualService.UploadManuals(1, files);

        var thrown = await act.Should().ThrowAsync<ValidationException>().WithMessage(expectedMessage);
        thrown.Which.Parameters.Should().Equal(new Dictionary<string, object> { ["fileName"] = fileName });
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadManuals_ShouldThrowAndWriteNothing_WhenAFileIsTooLarge()
    {
        var files = new List<IFormFile> { CreateFormFile("big.pdf", "application/pdf", 200L * 1024 * 1024 + 1) };

        var act = async () => await _manualService.UploadManuals(1, files);

        var thrown = await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.ManualTooLarge);
        thrown.Which.Parameters.Should().Equal(new Dictionary<string, object> { ["fileName"] = "big.pdf", ["maxSize"] = 200L });
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadManuals_ShouldAcceptFile_AtExactMaxSize()
    {
        var files = new List<IFormFile> { CreateFormFile("max.pdf", length: 200L * 1024 * 1024) };
        SetupWrite("max.pdf");

        var result = await _manualService.UploadManuals(1, files);

        result.Should().ContainSingle().Which.FileSizeBytes.Should().Be(200L * 1024 * 1024);

        VerifyWritten("max.pdf");
        VerifySaved(result);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadManuals_ShouldDeleteWrittenFilesAndNotPersist_WhenWriteFailsMidBatch()
    {
        var files = new List<IFormFile> { CreateFormFile("a.pdf"), CreateFormFile("b.pdf") };
        var failure = new IOException("disk full");
        SetupWrite("a.pdf");
        _diskProviderMock
            .Setup(x => x.WriteFile(It.IsAny<Stream>(), "b.pdf", PathHelper.FullManualsPath))
            .ThrowsAsync(failure);

        var act = async () => await _manualService.UploadManuals(1, files);

        (await act.Should().ThrowAsync<IOException>()).Which.Should().BeSameAs(failure);

        VerifyWritten("a.pdf");
        VerifyWritten("b.pdf");
        _diskProviderMock.Verify(x => x.DeleteFile(Path.Combine(PathHelper.FullManualsPath, "stored-a.pdf")), Times.Once);
        _dateTimeProviderMock.Verify(x => x.UtcNow, Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadManuals_ShouldDeleteAllWrittenFiles_WhenSaveChangesFails()
    {
        var files = new List<IFormFile> { CreateFormFile("a.pdf"), CreateFormFile("b.pdf") };
        SetupWrite("a.pdf");
        SetupWrite("b.pdf");
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("save failed"));

        var act = async () => await _manualService.UploadManuals(1, files);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("save failed");

        VerifyWritten("a.pdf");
        VerifyWritten("b.pdf");
        _diskProviderMock.Verify(x => x.DeleteFile(Path.Combine(PathHelper.FullManualsPath, "stored-a.pdf")), Times.Once);
        _diskProviderMock.Verify(x => x.DeleteFile(Path.Combine(PathHelper.FullManualsPath, "stored-b.pdf")), Times.Once);
        _manualRepositoryMock.Verify(x => x.CreateRangeAsync(It.Is<List<Manual>>(l => l.Count == 2)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
        _dateTimeProviderMock.Verify(x => x.UtcNow, Times.Exactly(2));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadManuals_ShouldEnqueueEachManualForIndexing_AfterTheSave_WhenRagEnabled()
    {
        var files = new List<IFormFile> { CreateFormFile("a.pdf"), CreateFormFile("b.pdf") };
        var calls = new List<string>();
        _environmentProviderMock.Setup(x => x.RagEnabled).Returns(true);
        SetupWrite("a.pdf");
        SetupWrite("b.pdf");
        _manualRepositoryMock
            .Setup(x => x.CreateRangeAsync(It.Is<List<Manual>>(l => l.Count == 2)))
            .Callback((List<Manual> manuals) =>
            {
                manuals[0].Id = 10;
                manuals[1].Id = 20;
            })
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(CancellationToken.None))
            .Callback(() => calls.Add("save"))
            .ReturnsAsync(2);
        _indexingQueueMock.Setup(x => x.Enqueue(10)).Callback(() => calls.Add("10"));
        _indexingQueueMock.Setup(x => x.Enqueue(20)).Callback(() => calls.Add("20"));

        var result = await _manualService.UploadManuals(1, files);

        calls.Should().Equal("save", "10", "20");
        _indexingQueueMock.Verify(x => x.Enqueue(10), Times.Once);
        _indexingQueueMock.Verify(x => x.Enqueue(20), Times.Once);
        VerifyWritten("a.pdf");
        VerifyWritten("b.pdf");
        VerifySaved(result);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadManuals_ShouldNotEnqueueForIndexing_WhenRagDisabled()
    {
        var files = new List<IFormFile> { CreateFormFile("a.pdf") };
        _environmentProviderMock.Setup(x => x.RagEnabled).Returns(false);
        SetupWrite("a.pdf");

        var result = await _manualService.UploadManuals(1, files);

        result.Should().ContainSingle();
        VerifyWritten("a.pdf");
        VerifySaved(result);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RequeueManualForIndexing_ShouldThrow_WhenManualDoesNotExist()
    {
        _manualRepositoryMock.Setup(x => x.GetByIdAsync(99)).ReturnsAsync((Manual?)null);

        var act = async () => await _manualService.RequeueManualForIndexing(99);

        await act.Should().ThrowAsync<EntityNotFoundException>();

        _manualRepositoryMock.Verify(x => x.GetByIdAsync(99), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RequeueManualForIndexing_ShouldResetStateAndEnqueue_WhenRagEnabled()
    {
        var manual = CreateManual(7, 5);
        manual.MarkIndexed(12, DateTime.UtcNow);
        manual.MarkFailed("boom");
        _manualRepositoryMock.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(manual);
        _environmentProviderMock.Setup(x => x.RagEnabled).Returns(true);

        await _manualService.RequeueManualForIndexing(7);

        manual.IndexStatus.Should().Be(ManualIndexStatus.Pending);
        manual.IndexedChunkCount.Should().Be(0);
        manual.IndexError.Should().BeNull();
        manual.IndexedDate.Should().BeNull();

        _manualRepositoryMock.Verify(x => x.GetByIdAsync(7), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
        _environmentProviderMock.Verify(x => x.RagEnabled, Times.Once);
        _indexingQueueMock.Verify(x => x.Enqueue(7), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RequeueManualForIndexing_ShouldResetStateWithoutEnqueue_WhenRagDisabled()
    {
        var manual = CreateManual(7, 5);
        manual.MarkFailed("boom");
        _manualRepositoryMock.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(manual);
        _environmentProviderMock.Setup(x => x.RagEnabled).Returns(false);

        await _manualService.RequeueManualForIndexing(7);

        manual.IndexStatus.Should().Be(ManualIndexStatus.Pending);
        manual.IndexError.Should().BeNull();

        _manualRepositoryMock.Verify(x => x.GetByIdAsync(7), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
        _environmentProviderMock.Verify(x => x.RagEnabled, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetManualsForGame_ShouldReturnRepositoryResult()
    {
        var manuals = new List<Manual> { CreateManual(1, 5), CreateManual(2, 5) };
        _manualRepositoryMock
            .Setup(x => x.ListAsync(It.Is<ManualsByGameIdSpec>(s => s.IsSatisfiedBy(CreateManual(1, 5)) && !s.IsSatisfiedBy(CreateManual(1, 6))), CancellationToken.None))
            .ReturnsAsync(manuals);

        var result = await _manualService.GetManualsForGame(5);

        result.Should().BeEquivalentTo(manuals);
        _manualRepositoryMock.Verify(x => x.ListAsync(It.Is<ManualsByGameIdSpec>(s => s.IsSatisfiedBy(CreateManual(1, 5)) && !s.IsSatisfiedBy(CreateManual(1, 6))), CancellationToken.None), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteManual_ShouldDeleteFileAndRow_WhenManualExists()
    {
        var manual = CreateManual(7, 5);
        _manualRepositoryMock.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(manual);
        _manualRepositoryMock.Setup(x => x.DeleteAsync(7)).ReturnsAsync(true);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(CancellationToken.None)).ReturnsAsync(1);

        await _manualService.DeleteManual(7);

        _manualRepositoryMock.Verify(x => x.GetByIdAsync(7), Times.Once);
        _diskProviderMock.Verify(x => x.DeleteFile(Physical("stored-rulebook.pdf")), Times.Once);
        _pageRendererMock.Verify(x => x.DeleteFigures(7), Times.Once);
        _manualRepositoryMock.Verify(x => x.DeleteAsync(7), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteManual_ShouldThrow_WhenManualDoesNotExist()
    {
        _manualRepositoryMock.Setup(x => x.GetByIdAsync(99)).ReturnsAsync((Manual?)null);

        var act = async () => await _manualService.DeleteManual(99);

        await act.Should().ThrowAsync<EntityNotFoundException>();

        _manualRepositoryMock.Verify(x => x.GetByIdAsync(99), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetManualForDownload_ShouldThrow_WhenManualDoesNotExist()
    {
        _manualRepositoryMock.Setup(x => x.GetByIdAsync(99)).ReturnsAsync((Manual?)null);

        var act = async () => await _manualService.GetManualForDownload(99);

        await act.Should().ThrowAsync<EntityNotFoundException>();

        _manualRepositoryMock.Verify(x => x.GetByIdAsync(99), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteManual_ShouldThrowAndDeleteNothing_WhenStoredFileNameEscapesManualsFolder()
    {
        var manual = new Manual("evil.pdf", Path.Combine("..", "evil.pdf"), "application/pdf", 1024, 5, DateTime.UtcNow) { Id = 7 };
        _manualRepositoryMock.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(manual);

        var act = async () => await _manualService.DeleteManual(7);

        await act.Should().ThrowAsync<EntityNotFoundException>();

        _manualRepositoryMock.Verify(x => x.GetByIdAsync(7), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetManualForDownload_ShouldThrow_WhenFileMissingOnDisk()
    {
        var manual = CreateManual(3, 5);
        _manualRepositoryMock.Setup(x => x.GetByIdAsync(3)).ReturnsAsync(manual);
        _diskProviderMock.Setup(x => x.FileExists(Physical("stored-rulebook.pdf"))).Returns(false);

        var act = async () => await _manualService.GetManualForDownload(3);

        await act.Should().ThrowAsync<EntityNotFoundException>();

        _manualRepositoryMock.Verify(x => x.GetByIdAsync(3), Times.Once);
        _diskProviderMock.Verify(x => x.FileExists(Physical("stored-rulebook.pdf")), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetManualForDownload_ShouldReturnStream_WhenManualExists()
    {
        var manual = CreateManual(3, 5, "Catan.pdf");
        _manualRepositoryMock.Setup(x => x.GetByIdAsync(3)).ReturnsAsync(manual);
        _diskProviderMock.Setup(x => x.FileExists(Physical("stored-Catan.pdf"))).Returns(true);
        var stream = new MemoryStream();
        _diskProviderMock.Setup(x => x.OpenRead(Physical("stored-Catan.pdf"))).Returns(stream);

        var result = await _manualService.GetManualForDownload(3);

        result.FileName.Should().Be("Catan.pdf");
        result.ContentType.Should().Be("application/pdf");
        result.Stream.Should().BeSameAs(stream);

        _manualRepositoryMock.Verify(x => x.GetByIdAsync(3), Times.Once);
        _diskProviderMock.Verify(x => x.FileExists(Physical("stored-Catan.pdf")), Times.Once);
        _diskProviderMock.Verify(x => x.OpenRead(Physical("stored-Catan.pdf")), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetManualPageImage_ShouldThrow_WhenManualDoesNotExist()
    {
        _manualRepositoryMock.Setup(x => x.GetByIdAsync(99)).ReturnsAsync((Manual?)null);

        var act = async () => await _manualService.GetManualPageImage(99, 1);

        await act.Should().ThrowAsync<EntityNotFoundException>();

        _manualRepositoryMock.Verify(x => x.GetByIdAsync(99), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetManualPageImage_ShouldThrow_WhenPdfFileMissingOnDisk()
    {
        var manual = CreateManual(3, 5);
        _manualRepositoryMock.Setup(x => x.GetByIdAsync(3)).ReturnsAsync(manual);
        _diskProviderMock.Setup(x => x.FileExists(Physical("stored-rulebook.pdf"))).Returns(false);

        var act = async () => await _manualService.GetManualPageImage(3, 1);

        await act.Should().ThrowAsync<EntityNotFoundException>();

        _manualRepositoryMock.Verify(x => x.GetByIdAsync(3), Times.Once);
        _diskProviderMock.Verify(x => x.FileExists(Physical("stored-rulebook.pdf")), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetManualPageImage_ShouldReturnPng_WhenRendered()
    {
        var manual = CreateManual(3, 5, "Catan.pdf");
        _manualRepositoryMock.Setup(x => x.GetByIdAsync(3)).ReturnsAsync(manual);
        _diskProviderMock.Setup(x => x.FileExists(Physical("stored-Catan.pdf"))).Returns(true);
        var rendered = new MemoryStream();
        _pageRendererMock
            .Setup(x => x.RenderPageAsync(Physical("stored-Catan.pdf"), 3, 2, _token))
            .ReturnsAsync(rendered);

        var result = await _manualService.GetManualPageImage(3, 2, _token);

        result!.Stream.Should().BeSameAs(rendered);
        result.ContentType.Should().Be("image/png");
        result.FileName.Should().Be("page-2.png");

        _manualRepositoryMock.Verify(x => x.GetByIdAsync(3), Times.Once);
        _diskProviderMock.Verify(x => x.FileExists(Physical("stored-Catan.pdf")), Times.Once);
        _pageRendererMock.Verify(x => x.RenderPageAsync(Physical("stored-Catan.pdf"), 3, 2, _token), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetManualPageImage_ShouldReturnNull_WhenRendererUnavailable()
    {
        var manual = CreateManual(3, 5, "Catan.pdf");
        _manualRepositoryMock.Setup(x => x.GetByIdAsync(3)).ReturnsAsync(manual);
        _diskProviderMock.Setup(x => x.FileExists(Physical("stored-Catan.pdf"))).Returns(true);
        _pageRendererMock
            .Setup(x => x.RenderPageAsync(Physical("stored-Catan.pdf"), 3, 2, _token))
            .ReturnsAsync((Stream?)null);

        var result = await _manualService.GetManualPageImage(3, 2, _token);

        result.Should().BeNull();

        _manualRepositoryMock.Verify(x => x.GetByIdAsync(3), Times.Once);
        _diskProviderMock.Verify(x => x.FileExists(Physical("stored-Catan.pdf")), Times.Once);
        _pageRendererMock.Verify(x => x.RenderPageAsync(Physical("stored-Catan.pdf"), 3, 2, _token), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GameNightManuals_ShouldRefuseAnonymousVisitors_WhenRsvpsRequireSigningIn()
    {
        var linkId = Guid.NewGuid();
        _configRepositoryMock
            .Setup(x => x.GetConfigValueOrDefaultAsync(BoardGameTracker.Common.Constants.AppConfig.RsvpAuthenticationEnabled, false))
            .ReturnsAsync(true);

        var list = async () => await _manualService.GetManualsForGameNight(linkId, false);
        var download = async () => await _manualService.GetManualForGameNightDownload(linkId, 11, false);

        await list.Should().ThrowAsync<AuthenticationFailedException>();
        await download.Should().ThrowAsync<AuthenticationFailedException>();
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(BoardGameTracker.Common.Constants.AppConfig.RsvpAuthenticationEnabled, false), Times.Exactly(2));
        VerifyGameNightsEnabledChecked(2);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GameNightManuals_ShouldBeNotFound_WhenGameNightsAreDisabled()
    {
        var linkId = Guid.NewGuid();
        _configRepositoryMock
            .Setup(x => x.GetConfigValueOrDefaultAsync(BoardGameTracker.Common.Constants.AppConfig.GameNightsEnabled, true))
            .ReturnsAsync(false);

        var list = async () => await _manualService.GetManualsForGameNight(linkId, true);
        var download = async () => await _manualService.GetManualForGameNightDownload(linkId, 11, true);

        await list.Should().ThrowAsync<FeatureDisabledException>();
        await download.Should().ThrowAsync<FeatureDisabledException>();
        VerifyGameNightsEnabledChecked(2);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GameNightManuals_ShouldServeAnonymousVisitors_WhenRsvpsAreOpen()
    {
        var gameNight = NewNight();
        var linkId = gameNight.LinkId;
        _configRepositoryMock
            .Setup(x => x.GetConfigValueOrDefaultAsync(BoardGameTracker.Common.Constants.AppConfig.RsvpAuthenticationEnabled, false))
            .ReturnsAsync(false);
        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec && ForNight(s, gameNight)), CancellationToken.None)).ReturnsAsync((GameNight?)null);

        var result = await _manualService.GetManualsForGameNight(linkId, false);

        result.Should().BeEmpty();
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(BoardGameTracker.Common.Constants.AppConfig.RsvpAuthenticationEnabled, false), Times.Once);
        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec && ForNight(s, gameNight)), CancellationToken.None), Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetManualForGameNightDownload_ShouldThrow_WhenGameNightDoesNotExist()
    {
        var gameNight = NewNight();
        var linkId = gameNight.LinkId;
        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec && ForNight(s, gameNight)), CancellationToken.None)).ReturnsAsync((GameNight?)null);
        _manualRepositoryMock.Setup(x => x.GetByIdAsync(11)).ReturnsAsync(CreateManual(11, 5));

        var act = async () => await _manualService.GetManualForGameNightDownload(linkId, 11, true);

        await act.Should().ThrowAsync<EntityNotFoundException>();

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec && ForNight(s, gameNight)), CancellationToken.None), Times.Once);
        _manualRepositoryMock.Verify(x => x.GetByIdAsync(11), Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetManualForGameNightDownload_ShouldThrow_WhenManualDoesNotExist()
    {
        var gameNight = NewNight();
        var linkId = gameNight.LinkId;
        gameNight.SetSuggestedGames(new List<Game> { new("Catan") { Id = 5 } });
        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec && ForNight(s, gameNight)), CancellationToken.None)).ReturnsAsync(gameNight);
        _manualRepositoryMock.Setup(x => x.GetByIdAsync(11)).ReturnsAsync((Manual?)null);

        var act = async () => await _manualService.GetManualForGameNightDownload(linkId, 11, true);

        await act.Should().ThrowAsync<EntityNotFoundException>();

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec && ForNight(s, gameNight)), CancellationToken.None), Times.Once);
        _manualRepositoryMock.Verify(x => x.GetByIdAsync(11), Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetManualForGameNightDownload_ShouldThrow_WhenManualNotInNight()
    {
        var gameNight = NewNight();
        var linkId = gameNight.LinkId;
        gameNight.SetSuggestedGames(new List<Game> { new("Catan") { Id = 5 } });
        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec && ForNight(s, gameNight)), CancellationToken.None)).ReturnsAsync(gameNight);
        _manualRepositoryMock.Setup(x => x.GetByIdAsync(11)).ReturnsAsync(CreateManual(11, 99));

        var act = async () => await _manualService.GetManualForGameNightDownload(linkId, 11, true);

        await act.Should().ThrowAsync<EntityNotFoundException>();

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec && ForNight(s, gameNight)), CancellationToken.None), Times.Once);
        _manualRepositoryMock.Verify(x => x.GetByIdAsync(11), Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetManualForGameNightDownload_ShouldReturnStream_WhenManualBelongsToNight()
    {
        var gameNight = NewNight();
        var linkId = gameNight.LinkId;
        gameNight.SetSuggestedGames(new List<Game> { new("Catan") { Id = 5 } });
        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec && ForNight(s, gameNight)), CancellationToken.None)).ReturnsAsync(gameNight);
        _manualRepositoryMock.Setup(x => x.GetByIdAsync(11)).ReturnsAsync(CreateManual(11, 5, "Catan.pdf"));
        _diskProviderMock.Setup(x => x.FileExists(Physical("stored-Catan.pdf"))).Returns(true);
        var stream = new MemoryStream();
        _diskProviderMock.Setup(x => x.OpenRead(Physical("stored-Catan.pdf"))).Returns(stream);

        var result = await _manualService.GetManualForGameNightDownload(linkId, 11, true);

        result.FileName.Should().Be("Catan.pdf");
        result.Stream.Should().BeSameAs(stream);

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec && ForNight(s, gameNight)), CancellationToken.None), Times.Once);
        _manualRepositoryMock.Verify(x => x.GetByIdAsync(11), Times.Once);
        _diskProviderMock.Verify(x => x.FileExists(Physical("stored-Catan.pdf")), Times.Once);
        _diskProviderMock.Verify(x => x.OpenRead(Physical("stored-Catan.pdf")), Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetManualsForGameNight_ShouldGroupByGameAndSkipGamesWithoutManuals()
    {
        var gameNight = NewNight();
        var linkId = gameNight.LinkId;
        gameNight.SetSuggestedGames(new List<Game> { new("Catan") { Id = 1 }, new("Wingspan") { Id = 2 }, new("Brass") { Id = 3 } });
        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec && ForNight(s, gameNight)), CancellationToken.None)).ReturnsAsync(gameNight);
        _manualRepositoryMock
            .Setup(x => x.ListAsync(
                It.Is<ManualsByGameIdsSpec>(s => s.IsSatisfiedBy(CreateManual(1, 1)) && s.IsSatisfiedBy(CreateManual(1, 2)) && s.IsSatisfiedBy(CreateManual(1, 3)) && !s.IsSatisfiedBy(CreateManual(1, 4))),
                CancellationToken.None))
            .ReturnsAsync(new List<Manual> { CreateManual(1, 1), CreateManual(2, 1), CreateManual(3, 3) });

        var result = await _manualService.GetManualsForGameNight(linkId, true);

        result.Select(x => x.GameId).Should().Equal(1, 3);
        result[0].GameTitle.Should().Be("Catan");
        result[0].Manuals.Select(x => x.Id).Should().Equal(1, 2);
        result[1].GameTitle.Should().Be("Brass");
        result[1].Manuals.Select(x => x.Id).Should().Equal(3);

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec && ForNight(s, gameNight)), CancellationToken.None), Times.Once);
        _manualRepositoryMock.Verify(x => x.ListAsync(
            It.Is<ManualsByGameIdsSpec>(s => s.IsSatisfiedBy(CreateManual(1, 1)) && s.IsSatisfiedBy(CreateManual(1, 2)) && s.IsSatisfiedBy(CreateManual(1, 3)) && !s.IsSatisfiedBy(CreateManual(1, 4))),
            CancellationToken.None), Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetManualsForGameNight_ShouldReturnEmptyWithoutQueryingManuals_WhenNightHasNoSuggestedGames()
    {
        var gameNight = NewNight();
        var linkId = gameNight.LinkId;
        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec && ForNight(s, gameNight)), CancellationToken.None)).ReturnsAsync(gameNight);

        var result = await _manualService.GetManualsForGameNight(linkId, true);

        result.Should().BeEmpty();

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec && ForNight(s, gameNight)), CancellationToken.None), Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetManualsForGameNight_ShouldReturnEmpty_WhenNightNotFound()
    {
        var gameNight = NewNight();
        var linkId = gameNight.LinkId;
        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec && ForNight(s, gameNight)), CancellationToken.None)).ReturnsAsync((GameNight?)null);

        var result = await _manualService.GetManualsForGameNight(linkId, true);

        result.Should().BeEmpty();

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec && ForNight(s, gameNight)), CancellationToken.None), Times.Once);
        VerifyGameNightsEnabledChecked();
        VerifyNoOtherCalls();
    }

    [Fact]
    public void DeleteManualFiles_ShouldDeleteEachFileWithoutTouchingRows()
    {
        _manualService.DeleteManualFiles([CreateManual(1, 5, "a.pdf"), CreateManual(2, 5, "b.pdf")]);

        _diskProviderMock.Verify(x => x.DeleteFile(Physical("stored-a.pdf")), Times.Once);
        _diskProviderMock.Verify(x => x.DeleteFile(Physical("stored-b.pdf")), Times.Once);
        _pageRendererMock.Verify(x => x.DeleteFigures(1), Times.Once);
        _pageRendererMock.Verify(x => x.DeleteFigures(2), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void ClearAllManuals_ShouldClearManualsFolder()
    {
        _manualService.ClearAllManuals();

        _diskProviderMock.Verify(x => x.ClearFolder(PathHelper.FullManualsPath), Times.Once);
        _pageRendererMock.Verify(x => x.ClearAllFigures(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameIdsWithManuals_ShouldReturnEachGameOnce()
    {
        _manualRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<GameIdsWithManualsSpec>(), _token))
            .ReturnsAsync([1, 1, 2, 3]);

        var result = await _manualService.GetGameIdsWithManuals(_token);

        result.Should().Equal(1, 2, 3);
        _manualRepositoryMock.Verify(x => x.ListAsync(It.IsAny<GameIdsWithManualsSpec>(), _token), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteManual_ShouldKeepTheFiles_WhenTheSaveFails()
    {
        _manualRepositoryMock.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(CreateManual(7, 5));
        _manualRepositoryMock.Setup(x => x.DeleteAsync(7)).ReturnsAsync(true);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("save failed"));

        var act = async () => await _manualService.DeleteManual(7);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("save failed");
        _manualRepositoryMock.Verify(x => x.GetByIdAsync(7), Times.Once);
        _manualRepositoryMock.Verify(x => x.DeleteAsync(7), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
        VerifyNoOtherCalls();
    }
}
