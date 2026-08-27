using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Disk.Interfaces;
using BoardGameTracker.Core.Rag;
using BoardGameTracker.Core.Rag.Interfaces;
using BoardGameTracker.Core.Rag.Specifications;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Rag;

public class ManualIndexingServiceTests
{
    private readonly Mock<IRepository<Manual>> _manualRepoMock = new();
    private readonly Mock<IRepository<ManualChunk>> _chunkWriteRepoMock = new();
    private readonly Mock<IManualChunkRepository> _chunkRepoMock = new();
    private readonly Mock<IPdfTextExtractor> _extractorMock = new();
    private readonly Mock<IRulebookChunker> _chunkerMock = new();
    private readonly Mock<IAiClientFactory> _factoryMock = new();
    private readonly Mock<IManualIndexingQueue> _queueMock = new();
    private readonly Mock<IDiskProvider> _diskProviderMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IEmbeddingGenerator<string, Embedding<float>>> _embedderMock = new();
    private readonly ManualIndexingService _service;

    public ManualIndexingServiceTests()
    {
        _diskProviderMock.Setup(x => x.OpenRead(It.IsAny<string>())).Returns(() => new MemoryStream());
        _extractorMock.Setup(x => x.Extract(It.IsAny<Stream>())).Returns(new List<PdfPageText> { new(1, "content") });
        _factoryMock.Setup(x => x.EnsureModelsAvailableAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _factoryMock.Setup(x => x.CreateEmbeddingGeneratorAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_embedderMock.Object);

        _service = new ManualIndexingService(
            _manualRepoMock.Object,
            _chunkWriteRepoMock.Object,
            _chunkRepoMock.Object,
            _extractorMock.Object,
            _chunkerMock.Object,
            _factoryMock.Object,
            _queueMock.Object,
            _diskProviderMock.Object,
            _unitOfWorkMock.Object,
            Mock.Of<ILogger<ManualIndexingService>>());
    }

    private void VerifyNoOtherCalls()
    {
        _manualRepoMock.VerifyNoOtherCalls();
        _chunkWriteRepoMock.VerifyNoOtherCalls();
        _chunkRepoMock.VerifyNoOtherCalls();
        _extractorMock.VerifyNoOtherCalls();
        _chunkerMock.VerifyNoOtherCalls();
        _factoryMock.VerifyNoOtherCalls();
        _queueMock.VerifyNoOtherCalls();
        _diskProviderMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
        _embedderMock.VerifyNoOtherCalls();
    }

    private void VerifyExtractionPipeline(Manual manual)
    {
        _manualRepoMock.Verify(x => x.GetByIdAsync(manual.Id), Times.Once);
        _factoryMock.Verify(x => x.EnsureModelsAvailableAsync(It.IsAny<CancellationToken>()), Times.Once);
        _diskProviderMock.Verify(x => x.OpenRead(It.IsAny<string>()), Times.Once);
        _extractorMock.Verify(x => x.Extract(It.IsAny<Stream>()), Times.Once);
        _chunkerMock.Verify(x => x.Chunk(It.IsAny<IReadOnlyList<PdfPageText>>()), Times.Once);
    }

    [Fact]
    public async Task IndexAsync_NoExtractableText_MarksFailedWithoutEmbedding()
    {
        var manual = CreateManual();
        _manualRepoMock.Setup(x => x.GetByIdAsync(manual.Id)).ReturnsAsync(manual);
        _chunkerMock.Setup(x => x.Chunk(It.IsAny<IReadOnlyList<PdfPageText>>())).Returns(new List<TextChunk>());

        await _service.IndexAsync(manual.Id);

        manual.IndexStatus.Should().Be(ManualIndexStatus.Failed);
        manual.IndexError.Should().NotBeNullOrEmpty();
        _embedderMock.Verify(
            x => x.GenerateAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<EmbeddingGenerationOptions?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _chunkRepoMock.Verify(x => x.DeleteByManualAsync(It.IsAny<int>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        VerifyExtractionPipeline(manual);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IndexAsync_EmbeddingDimensionMismatch_MarksFailed()
    {
        var manual = CreateManual();
        _manualRepoMock.Setup(x => x.GetByIdAsync(manual.Id)).ReturnsAsync(manual);
        _chunkerMock.Setup(x => x.Chunk(It.IsAny<IReadOnlyList<PdfPageText>>()))
            .Returns(new List<TextChunk> { new(0, "chunk", 1) });
        SetupEmbeddings(count: 1, dimensions: 768);

        await _service.IndexAsync(manual.Id);

        manual.IndexStatus.Should().Be(ManualIndexStatus.Failed);
        manual.IndexError.Should().Contain("dimension");
        _chunkWriteRepoMock.Verify(x => x.CreateRangeAsync(It.IsAny<List<ManualChunk>>()), Times.Never);
        _factoryMock.Verify(x => x.CreateEmbeddingGeneratorAsync(It.IsAny<CancellationToken>()), Times.Once);
        _embedderMock.Verify(
            x => x.GenerateAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<EmbeddingGenerationOptions?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _chunkRepoMock.Verify(x => x.DeleteByManualAsync(manual.Id), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        VerifyExtractionPipeline(manual);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IndexAsync_HappyPath_MarksIndexedAndPersistsChunks()
    {
        var manual = CreateManual();
        _manualRepoMock.Setup(x => x.GetByIdAsync(manual.Id)).ReturnsAsync(manual);
        _chunkerMock.Setup(x => x.Chunk(It.IsAny<IReadOnlyList<PdfPageText>>()))
            .Returns(new List<TextChunk> { new(0, "first", 1), new(1, "second", 2) });
        SetupEmbeddings(count: 2, dimensions: 1024);

        await _service.IndexAsync(manual.Id);

        manual.IndexStatus.Should().Be(ManualIndexStatus.Indexed);
        manual.IndexedChunkCount.Should().Be(2);
        manual.IndexError.Should().BeNull();
        _chunkRepoMock.Verify(x => x.DeleteByManualAsync(manual.Id), Times.Once);
        _chunkWriteRepoMock.Verify(x => x.CreateRangeAsync(It.Is<List<ManualChunk>>(l =>
            l.Count == 2 &&
            l[0].ManualId == manual.Id && l[0].GameId == manual.GameId &&
            l[0].ChunkIndex == 0 && l[0].Content == "first" && l[0].PageNumber == 1 &&
            l[1].ManualId == manual.Id && l[1].GameId == manual.GameId &&
            l[1].ChunkIndex == 1 && l[1].Content == "second" && l[1].PageNumber == 2)), Times.Once);
        _factoryMock.Verify(x => x.CreateEmbeddingGeneratorAsync(It.IsAny<CancellationToken>()), Times.Once);
        _embedderMock.Verify(
            x => x.GenerateAsync(
                It.Is<IEnumerable<string>>(v => v.SequenceEqual(new[] { "first", "second" })),
                It.IsAny<EmbeddingGenerationOptions?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        VerifyExtractionPipeline(manual);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EnqueuePendingAsync_ShouldEnqueueEachManualId_WhenManualsArePending()
    {
        var first = CreateManual(5);
        var second = CreateManual(8);
        _manualRepoMock
            .Setup(x => x.ListAsync(It.IsAny<ManualsToIndexSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Manual> { first, second });

        await _service.EnqueuePendingAsync();

        _queueMock.Verify(x => x.Enqueue(5), Times.Once);
        _queueMock.Verify(x => x.Enqueue(8), Times.Once);
        _manualRepoMock.Verify(x => x.ListAsync(It.IsAny<ManualsToIndexSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EnqueuePendingAsync_ShouldNotEnqueue_WhenNoManualsArePending()
    {
        _manualRepoMock
            .Setup(x => x.ListAsync(It.IsAny<ManualsToIndexSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Manual>());

        await _service.EnqueuePendingAsync();

        _manualRepoMock.Verify(x => x.ListAsync(It.IsAny<ManualsToIndexSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IndexAsync_ShouldMarkFailedWithExceptionMessage_WhenPdfReadThrows()
    {
        var manual = CreateManual();
        _manualRepoMock.Setup(x => x.GetByIdAsync(manual.Id)).ReturnsAsync(manual);
        _diskProviderMock.Setup(x => x.OpenRead(It.IsAny<string>())).Throws(new IOException("disk unreadable"));

        await _service.IndexAsync(manual.Id);

        manual.IndexStatus.Should().Be(ManualIndexStatus.Failed);
        manual.IndexError.Should().Be("disk unreadable");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _chunkWriteRepoMock.Verify(x => x.CreateRangeAsync(It.IsAny<List<ManualChunk>>()), Times.Never);
        _manualRepoMock.Verify(x => x.GetByIdAsync(manual.Id), Times.Once);
        _factoryMock.Verify(x => x.EnsureModelsAvailableAsync(It.IsAny<CancellationToken>()), Times.Once);
        _diskProviderMock.Verify(x => x.OpenRead(It.IsAny<string>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IndexAsync_ShouldNotThrow_WhenPersistingTheFailureAlsoThrows()
    {
        var manual = CreateManual();
        _manualRepoMock.Setup(x => x.GetByIdAsync(manual.Id)).ReturnsAsync(manual);
        _factoryMock
            .Setup(x => x.EnsureModelsAvailableAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("models unavailable"));
        _unitOfWorkMock
            .SetupSequence(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .ThrowsAsync(new InvalidOperationException("database gone"));

        var act = () => _service.IndexAsync(manual.Id);

        await act.Should().NotThrowAsync();
        manual.IndexStatus.Should().Be(ManualIndexStatus.Failed);
        manual.IndexError.Should().Be("models unavailable");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _manualRepoMock.Verify(x => x.GetByIdAsync(manual.Id), Times.Once);
        _factoryMock.Verify(x => x.EnsureModelsAvailableAsync(It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IndexAsync_ShouldMarkFailedWithoutOpeningFile_WhenStoredFileNameEscapesManualsDirectory()
    {
        var manual = new Manual("Base Rules", Path.Combine("..", "evil.pdf"), "application/pdf", 100, 1, DateTime.UtcNow)
        {
            Id = 5
        };
        _manualRepoMock.Setup(x => x.GetByIdAsync(manual.Id)).ReturnsAsync(manual);

        await _service.IndexAsync(manual.Id);

        manual.IndexStatus.Should().Be(ManualIndexStatus.Failed);
        manual.IndexError.Should().Contain("evil.pdf").And.Contain("not found");
        _diskProviderMock.Verify(x => x.OpenRead(It.IsAny<string>()), Times.Never);
        _chunkWriteRepoMock.Verify(x => x.CreateRangeAsync(It.IsAny<List<ManualChunk>>()), Times.Never);
        _manualRepoMock.Verify(x => x.GetByIdAsync(manual.Id), Times.Once);
        _factoryMock.Verify(x => x.EnsureModelsAvailableAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IndexAsync_ManualNotFound_DoesNothing()
    {
        _manualRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Manual?)null);

        await _service.IndexAsync(999);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _manualRepoMock.Verify(x => x.GetByIdAsync(999), Times.Once);
        VerifyNoOtherCalls();
    }

    private void SetupEmbeddings(int count, int dimensions)
    {
        var embeddings = new GeneratedEmbeddings<Embedding<float>>(
            Enumerable.Range(0, count).Select(_ => new Embedding<float>(new float[dimensions])));
        _embedderMock
            .Setup(x => x.GenerateAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<EmbeddingGenerationOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddings);
    }

    private static Manual CreateManual(int id = 5)
    {
        return new Manual("Base Rules", "stored.pdf", "application/pdf", 100, 1, DateTime.UtcNow)
        {
            Id = id
        };
    }
}
