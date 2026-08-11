using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Disk.Interfaces;
using BoardGameTracker.Core.GameNights.Specifications;
using BoardGameTracker.Core.Manuals;
using BoardGameTracker.Core.Manuals.Specifications;
using BoardGameTracker.Core.Rag.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class ManualServiceTests
{
    private readonly Mock<IRepository<Manual>> _manualRepositoryMock;
    private readonly Mock<IDiskProvider> _diskProviderMock;
    private readonly Mock<IReadRepository<GameNight>> _gameNightRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IManualIndexingQueue> _indexingQueueMock;
    private readonly Mock<IPdfPageRenderer> _pageRendererMock;
    private readonly Mock<IEnvironmentProvider> _environmentProviderMock;
    private readonly Mock<ILogger<ManualService>> _loggerMock;
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
        _loggerMock = new Mock<ILogger<ManualService>>();

        _manualService = new ManualService(
            _manualRepositoryMock.Object,
            _diskProviderMock.Object,
            _gameNightRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _indexingQueueMock.Object,
            _pageRendererMock.Object,
            _environmentProviderMock.Object,
            _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _manualRepositoryMock.VerifyNoOtherCalls();
        _diskProviderMock.VerifyNoOtherCalls();
        _gameNightRepositoryMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
        _pageRendererMock.VerifyNoOtherCalls();
    }

    private static IFormFile CreateFormFile(string fileName = "rulebook.pdf", string contentType = "application/pdf", long length = 1024)
    {
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns(fileName);
        file.Setup(f => f.ContentType).Returns(contentType);
        file.Setup(f => f.Length).Returns(length);
        file.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(new byte[length]));
        return file.Object;
    }

    private static Manual CreateManual(int id, int gameId, string title = "rulebook.pdf")
    {
        return new Manual(title, $"stored-{title}", "application/pdf", 1024, gameId, DateTime.UtcNow) { Id = id };
    }

    [Fact]
    public async Task UploadManuals_ShouldWriteFilesAndPersist_WhenFilesAreValid()
    {
        var files = new List<IFormFile> { CreateFormFile("a.pdf"), CreateFormFile("b.pdf") };
        _diskProviderMock
            .Setup(x => x.WriteFile(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((Stream _, string fileName, string _) => $"stored-{fileName}");

        var result = await _manualService.UploadManuals(1, files);

        result.Should().HaveCount(2);
        result.Select(m => m.Title).Should().BeEquivalentTo("a.pdf", "b.pdf");
        result.Should().OnlyContain(m => m.GameId == 1 && m.ContentType == "application/pdf");

        _diskProviderMock.Verify(x => x.WriteFile(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        _manualRepositoryMock.Verify(x => x.CreateRangeAsync(It.Is<List<Manual>>(l => l.Count == 2)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadManuals_ShouldThrow_WhenNoFilesProvided()
    {
        var act = async () => await _manualService.UploadManuals(1, new List<IFormFile>());

        await act.Should().ThrowAsync<ValidationException>();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadManuals_ShouldThrowAndWriteNothing_WhenAFileIsNotPdf()
    {
        var files = new List<IFormFile> { CreateFormFile("a.pdf"), CreateFormFile("b.txt", "text/plain") };

        var act = async () => await _manualService.UploadManuals(1, files);

        await act.Should().ThrowAsync<ValidationException>();

        _diskProviderMock.Verify(x => x.WriteFile(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadManuals_ShouldThrow_WhenFileExceedsMaxSize()
    {
        var files = new List<IFormFile> { CreateFormFile("big.pdf", length: 201L * 1024 * 1024) };

        var act = async () => await _manualService.UploadManuals(1, files);

        await act.Should().ThrowAsync<ValidationException>();

        _diskProviderMock.Verify(x => x.WriteFile(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadManuals_ShouldDeleteWrittenFilesAndNotPersist_WhenWriteFailsMidBatch()
    {
        var files = new List<IFormFile> { CreateFormFile("a.pdf"), CreateFormFile("b.pdf") };
        _diskProviderMock
            .SetupSequence(x => x.WriteFile(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("stored-a.pdf")
            .ThrowsAsync(new IOException("disk full"));

        var act = async () => await _manualService.UploadManuals(1, files);

        await act.Should().ThrowAsync<IOException>();

        _diskProviderMock.Verify(x => x.WriteFile(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        _diskProviderMock.Verify(x => x.DeleteFile(It.Is<string>(p => p.EndsWith("stored-a.pdf"))), Times.Once);
        _manualRepositoryMock.Verify(x => x.CreateRangeAsync(It.IsAny<List<Manual>>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetManualsForGame_ShouldReturnRepositoryResult()
    {
        var manuals = new List<Manual> { CreateManual(1, 5), CreateManual(2, 5) };
        _manualRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ManualsByGameIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(manuals);

        var result = await _manualService.GetManualsForGame(5);

        result.Should().BeEquivalentTo(manuals);
        _manualRepositoryMock.Verify(x => x.ListAsync(It.IsAny<ManualsByGameIdSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteManual_ShouldDeleteFileAndRow_WhenManualExists()
    {
        var manual = CreateManual(7, 5);
        _manualRepositoryMock.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(manual);
        _manualRepositoryMock.Setup(x => x.DeleteAsync(7)).ReturnsAsync(true);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _manualService.DeleteManual(7);

        _manualRepositoryMock.Verify(x => x.GetByIdAsync(7), Times.Once);
        _diskProviderMock.Verify(x => x.DeleteFile(It.Is<string>(p => p.EndsWith("stored-rulebook.pdf"))), Times.Once);
        _pageRendererMock.Verify(x => x.DeleteFigures(7), Times.Once);
        _manualRepositoryMock.Verify(x => x.DeleteAsync(7), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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
    public async Task GetManualForDownload_ShouldReturnStream_WhenManualExists()
    {
        var manual = CreateManual(3, 5, "Catan.pdf");
        _manualRepositoryMock.Setup(x => x.GetByIdAsync(3)).ReturnsAsync(manual);
        _diskProviderMock.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
        _diskProviderMock.Setup(x => x.OpenRead(It.IsAny<string>())).Returns(new MemoryStream());

        var result = await _manualService.GetManualForDownload(3);

        result.FileName.Should().Be("Catan.pdf");
        result.ContentType.Should().Be("application/pdf");
        result.Stream.Should().NotBeNull();

        _manualRepositoryMock.Verify(x => x.GetByIdAsync(3), Times.Once);
        _diskProviderMock.Verify(x => x.FileExists(It.IsAny<string>()), Times.Once);
        _diskProviderMock.Verify(x => x.OpenRead(It.IsAny<string>()), Times.Once);
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
    public async Task GetManualPageImage_ShouldReturnPng_WhenRendered()
    {
        var manual = CreateManual(3, 5, "Catan.pdf");
        _manualRepositoryMock.Setup(x => x.GetByIdAsync(3)).ReturnsAsync(manual);
        _diskProviderMock.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
        _pageRendererMock
            .Setup(x => x.RenderPageAsync(It.IsAny<string>(), 3, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream());

        var result = await _manualService.GetManualPageImage(3, 2);

        result.Should().NotBeNull();
        result!.ContentType.Should().Be("image/png");
        result.FileName.Should().Be("page-2.png");

        _manualRepositoryMock.Verify(x => x.GetByIdAsync(3), Times.Once);
        _diskProviderMock.Verify(x => x.FileExists(It.IsAny<string>()), Times.Once);
        _pageRendererMock.Verify(x => x.RenderPageAsync(It.IsAny<string>(), 3, 2, It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetManualPageImage_ShouldReturnNull_WhenRendererUnavailable()
    {
        var manual = CreateManual(3, 5, "Catan.pdf");
        _manualRepositoryMock.Setup(x => x.GetByIdAsync(3)).ReturnsAsync(manual);
        _diskProviderMock.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
        _pageRendererMock
            .Setup(x => x.RenderPageAsync(It.IsAny<string>(), 3, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);

        var result = await _manualService.GetManualPageImage(3, 2);

        result.Should().BeNull();

        _manualRepositoryMock.Verify(x => x.GetByIdAsync(3), Times.Once);
        _diskProviderMock.Verify(x => x.FileExists(It.IsAny<string>()), Times.Once);
        _pageRendererMock.Verify(x => x.RenderPageAsync(It.IsAny<string>(), 3, 2, It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetManualForGameNightDownload_ShouldThrow_WhenManualNotInNight()
    {
        var linkId = Guid.NewGuid();
        var gameNight = GameNight.Create("Night", "", DateTime.UtcNow, 1, 1);
        gameNight.SetSuggestedGames(new List<Game> { new("Catan") { Id = 5 } });
        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec), It.IsAny<CancellationToken>())).ReturnsAsync(gameNight);
        _manualRepositoryMock.Setup(x => x.GetByIdAsync(11)).ReturnsAsync(CreateManual(11, 99));

        var act = async () => await _manualService.GetManualForGameNightDownload(linkId, 11);

        await act.Should().ThrowAsync<EntityNotFoundException>();

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        _manualRepositoryMock.Verify(x => x.GetByIdAsync(11), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetManualForGameNightDownload_ShouldReturnStream_WhenManualBelongsToNight()
    {
        var linkId = Guid.NewGuid();
        var gameNight = GameNight.Create("Night", "", DateTime.UtcNow, 1, 1);
        gameNight.SetSuggestedGames(new List<Game> { new("Catan") { Id = 5 } });
        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec), It.IsAny<CancellationToken>())).ReturnsAsync(gameNight);
        _manualRepositoryMock.Setup(x => x.GetByIdAsync(11)).ReturnsAsync(CreateManual(11, 5, "Catan.pdf"));
        _diskProviderMock.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
        _diskProviderMock.Setup(x => x.OpenRead(It.IsAny<string>())).Returns(new MemoryStream());

        var result = await _manualService.GetManualForGameNightDownload(linkId, 11);

        result.FileName.Should().Be("Catan.pdf");

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        _manualRepositoryMock.Verify(x => x.GetByIdAsync(11), Times.Once);
        _diskProviderMock.Verify(x => x.FileExists(It.IsAny<string>()), Times.Once);
        _diskProviderMock.Verify(x => x.OpenRead(It.IsAny<string>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetManualsForGameNight_ShouldGroupByGameAndSkipGamesWithoutManuals()
    {
        var linkId = Guid.NewGuid();
        var gameNight = GameNight.Create("Night", "", DateTime.UtcNow, 1, 1);
        gameNight.SetSuggestedGames(new List<Game> { new("Catan") { Id = 1 }, new("Wingspan") { Id = 2 } });
        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec), It.IsAny<CancellationToken>())).ReturnsAsync(gameNight);
        _manualRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ManualsByGameIdsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Manual> { CreateManual(1, 1), CreateManual(2, 1) });

        var result = await _manualService.GetManualsForGameNight(linkId);

        result.Should().HaveCount(1);
        result[0].GameId.Should().Be(1);
        result[0].GameTitle.Should().Be("Catan");
        result[0].Manuals.Should().HaveCount(2);

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        _manualRepositoryMock.Verify(x => x.ListAsync(It.IsAny<ManualsByGameIdsSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetManualsForGameNight_ShouldReturnEmpty_WhenNightNotFound()
    {
        var linkId = Guid.NewGuid();
        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec), It.IsAny<CancellationToken>())).ReturnsAsync((GameNight?)null);

        var result = await _manualService.GetManualsForGameNight(linkId);

        result.Should().BeEmpty();

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteManualFilesForGame_ShouldDeleteEachFileWithoutTouchingRows()
    {
        _manualRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ManualsByGameIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Manual> { CreateManual(1, 5, "a.pdf"), CreateManual(2, 5, "b.pdf") });

        await _manualService.DeleteManualFilesForGame(5);

        _manualRepositoryMock.Verify(x => x.ListAsync(It.IsAny<ManualsByGameIdSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        _diskProviderMock.Verify(x => x.DeleteFile(It.Is<string>(p => p.EndsWith("stored-a.pdf"))), Times.Once);
        _diskProviderMock.Verify(x => x.DeleteFile(It.Is<string>(p => p.EndsWith("stored-b.pdf"))), Times.Once);
        _pageRendererMock.Verify(x => x.DeleteFigures(1), Times.Once);
        _pageRendererMock.Verify(x => x.DeleteFigures(2), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void ClearAllManuals_ShouldClearManualsFolder()
    {
        _manualService.ClearAllManuals();

        _diskProviderMock.Verify(x => x.ClearFolder(It.IsAny<string>()), Times.Once);
        _pageRendererMock.Verify(x => x.ClearAllFigures(), Times.Once);
        VerifyNoOtherCalls();
    }
}
