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
        _chunkRepoMock.Verify(x => x.DeleteByManualAsync(manual.Id), Times.Once);
        _chunkWriteRepoMock.Verify(x => x.CreateRangeAsync(It.Is<List<ManualChunk>>(l => l.Count == 2)), Times.Once);
    }

    [Fact]
    public async Task IndexAsync_ManualNotFound_DoesNothing()
    {
        _manualRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Manual?)null);

        await _service.IndexAsync(999);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private void SetupEmbeddings(int count, int dimensions)
    {
        var embeddings = new GeneratedEmbeddings<Embedding<float>>(
            Enumerable.Range(0, count).Select(_ => new Embedding<float>(new float[dimensions])));
        _embedderMock
            .Setup(x => x.GenerateAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<EmbeddingGenerationOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddings);
    }

    private static Manual CreateManual()
    {
        return new Manual("Base Rules", "stored.pdf", "application/pdf", 100, 1, DateTime.UtcNow)
        {
            Id = 5
        };
    }
}
