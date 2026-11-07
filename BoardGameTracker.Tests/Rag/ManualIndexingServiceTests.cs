using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Disk.Interfaces;
using BoardGameTracker.Core.Rag;
using BoardGameTracker.Core.Rag.Interfaces;
using BoardGameTracker.Core.Rag.Specifications;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Rag;

public class ManualIndexingServiceTests
{
    private static readonly DateTime Now = new(2026, 9, 11, 10, 0, 0, DateTimeKind.Utc);
    private static readonly string PdfPath = PathHelper.MapManualFileToPhysical("stored.pdf");

    private readonly Mock<IRepository<Manual>> _manualRepoMock = new();
    private readonly Mock<IRepository<ManualChunk>> _chunkWriteRepoMock = new();
    private readonly Mock<IManualChunkRepository> _chunkRepoMock = new();
    private readonly Mock<IPdfTextExtractor> _extractorMock = new();
    private readonly Mock<IRulebookChunker> _chunkerMock = new();
    private readonly Mock<IAiClientFactory> _factoryMock = new();
    private readonly Mock<IManualIndexingQueue> _queueMock = new();
    private readonly Mock<IDiskProvider> _diskProviderMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDbContextTransaction> _transactionMock = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();
    private readonly Mock<IEmbeddingGenerator<string, Embedding<float>>> _embedderMock = new();
    private readonly Mock<ILogger<ManualIndexingService>> _loggerMock = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly CancellationToken _token;
    private readonly MemoryStream _pdfStream = new();
    private readonly List<PdfPageText> _pages = [new(1, "content")];
    private readonly ManualIndexingService _service;

    public ManualIndexingServiceTests()
    {
        _token = _cts.Token;
        _diskProviderMock.Setup(x => x.OpenRead(PdfPath)).Returns(_pdfStream);
        _extractorMock.Setup(x => x.Extract(_pdfStream)).Returns(_pages);
        _factoryMock.Setup(x => x.EnsureModelsAvailableAsync(_token)).Returns(Task.CompletedTask);
        _factoryMock.Setup(x => x.CreateEmbeddingGeneratorAsync(_token)).ReturnsAsync(_embedderMock.Object);
        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync(_token)).ReturnsAsync(_transactionMock.Object);
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(Now);

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
            _dateTimeProviderMock.Object,
            _loggerMock.Object);
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
        _transactionMock.VerifyNoOtherCalls();
        _dateTimeProviderMock.VerifyNoOtherCalls();
        _embedderMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
    }

    private void SetupChunks(params TextChunk[] chunks)
    {
        _chunkerMock
            .Setup(x => x.Chunk(It.Is<IReadOnlyList<PdfPageText>>(pages => pages.SequenceEqual(_pages))))
            .Returns(chunks);
    }

    private void SetupEmbeddings(string[] contents, params Embedding<float>[] embeddings)
    {
        _embedderMock
            .Setup(x => x.GenerateAsync(It.Is<IEnumerable<string>>(v => v.SequenceEqual(contents)), null, _token))
            .ReturnsAsync(new GeneratedEmbeddings<Embedding<float>>(embeddings));
    }

    private void VerifyEmbedded(string[] contents)
    {
        _embedderMock.Verify(
            x => x.GenerateAsync(It.Is<IEnumerable<string>>(v => v.SequenceEqual(contents)), null, _token),
            Times.Once);
    }

    private static Embedding<float> EmbeddingOf(float firstValue, int dimensions = 1024)
    {
        var vector = new float[dimensions];
        vector[0] = firstValue;
        return new Embedding<float>(vector);
    }

    private void VerifyExtractionPipeline(Manual manual)
    {
        _manualRepoMock.Verify(x => x.GetByIdAsync(manual.Id), Times.Once);
        _factoryMock.Verify(x => x.EnsureModelsAvailableAsync(_token), Times.Once);
        _diskProviderMock.Verify(x => x.OpenRead(PdfPath), Times.Once);
        _extractorMock.Verify(x => x.Extract(_pdfStream), Times.Once);
        _chunkerMock.Verify(x => x.Chunk(It.Is<IReadOnlyList<PdfPageText>>(pages => pages.SequenceEqual(_pages))), Times.Once);
    }

    private void VerifyStoredInTransaction(Manual manual, int chunkCount)
    {
        _chunkRepoMock.Verify(x => x.DeleteByManualAsync(manual.Id), Times.Once);
        _chunkWriteRepoMock.Verify(x => x.CreateRangeAsync(It.Is<List<ManualChunk>>(l => l.Count == chunkCount)), Times.Once);
        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(_token), Times.Once);
        _dateTimeProviderMock.Verify(x => x.UtcNow, Times.Once);
        _transactionMock.Verify(x => x.DisposeAsync(), Times.Once);
        _factoryMock.Verify(x => x.CreateEmbeddingGeneratorAsync(_token), Times.Once);
    }

    [Fact]
    public async Task IndexAsync_NoExtractableText_MarksFailedWithoutEmbedding()
    {
        var manual = CreateManual();
        _manualRepoMock.Setup(x => x.GetByIdAsync(manual.Id)).ReturnsAsync(manual);
        SetupChunks();

        await _service.IndexAsync(manual.Id, _token);

        manual.IndexStatus.Should().Be(ManualIndexStatus.Failed);
        manual.IndexError.Should().Be("No extractable text found (the PDF may be scanned or image-only).");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(_token), Times.Exactly(2));
        VerifyExtractionPipeline(manual);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IndexAsync_EmbeddingDimensionMismatch_MarksFailed()
    {
        var manual = CreateManual();
        _manualRepoMock.Setup(x => x.GetByIdAsync(manual.Id)).ReturnsAsync(manual);
        SetupChunks(new TextChunk(0, "chunk", 1));
        SetupEmbeddings(["chunk"], EmbeddingOf(1, dimensions: 768));

        await _service.IndexAsync(manual.Id, _token);

        manual.IndexStatus.Should().Be(ManualIndexStatus.Failed);
        manual.IndexError.Should().Be("Embedding dimension mismatch: expected 1024, got 768. Check the configured embedding model.");
        _factoryMock.Verify(x => x.CreateEmbeddingGeneratorAsync(_token), Times.Once);
        VerifyEmbedded(["chunk"]);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(_token), Times.Exactly(2));
        VerifyExtractionPipeline(manual);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IndexAsync_ShouldStoreNothing_WhenALaterEmbeddingHasTheWrongDimension()
    {
        var manual = CreateManual();
        _manualRepoMock.Setup(x => x.GetByIdAsync(manual.Id)).ReturnsAsync(manual);
        SetupChunks(new TextChunk(0, "first", 1), new TextChunk(1, "second", 2));
        SetupEmbeddings(["first", "second"], EmbeddingOf(1), EmbeddingOf(2, dimensions: 1025));

        await _service.IndexAsync(manual.Id, _token);

        manual.IndexStatus.Should().Be(ManualIndexStatus.Failed);
        manual.IndexError.Should().Be("Embedding dimension mismatch: expected 1024, got 1025. Check the configured embedding model.");
        _factoryMock.Verify(x => x.CreateEmbeddingGeneratorAsync(_token), Times.Once);
        VerifyEmbedded(["first", "second"]);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(_token), Times.Exactly(2));
        VerifyExtractionPipeline(manual);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IndexAsync_HappyPath_MarksIndexedAndPersistsChunks()
    {
        var manual = CreateManual();
        var calls = new List<string>();
        var statuses = new List<ManualIndexStatus>();
        List<ManualChunk>? saved = null;
        _manualRepoMock.Setup(x => x.GetByIdAsync(manual.Id)).ReturnsAsync(manual);
        SetupChunks(new TextChunk(0, "first", 1), new TextChunk(1, "second", null));
        SetupEmbeddings(["first", "second"], EmbeddingOf(0.25f), EmbeddingOf(0.5f));
        _unitOfWorkMock
            .Setup(x => x.BeginTransactionAsync(_token))
            .Callback(() => calls.Add("begin"))
            .ReturnsAsync(_transactionMock.Object);
        _chunkRepoMock
            .Setup(x => x.DeleteByManualAsync(manual.Id))
            .Callback(() => calls.Add("delete"))
            .Returns(Task.CompletedTask);
        _chunkWriteRepoMock
            .Setup(x => x.CreateRangeAsync(It.Is<List<ManualChunk>>(l => l.Count == 2)))
            .Callback((List<ManualChunk> chunks) =>
            {
                saved = chunks;
                calls.Add("insert");
            })
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(_token))
            .Callback(() =>
            {
                statuses.Add(manual.IndexStatus);
                calls.Add("save");
            })
            .ReturnsAsync(1);
        _transactionMock
            .Setup(x => x.CommitAsync(_token))
            .Callback(() => calls.Add("commit"))
            .Returns(Task.CompletedTask);

        await _service.IndexAsync(manual.Id, _token);

        manual.IndexStatus.Should().Be(ManualIndexStatus.Indexed);
        manual.IndexedChunkCount.Should().Be(2);
        manual.IndexedDate.Should().Be(Now);
        manual.IndexError.Should().BeNull();
        calls.Should().Equal("save", "begin", "delete", "insert", "save", "commit");
        statuses.Should().Equal(ManualIndexStatus.Indexing, ManualIndexStatus.Indexed);
        saved.Should().HaveCount(2);
        saved.Should().OnlyContain(c => c.ManualId == manual.Id && c.GameId == manual.GameId);
        saved!.Select(c => c.ChunkIndex).Should().Equal(0, 1);
        saved!.Select(c => c.Content).Should().Equal("first", "second");
        saved!.Select(c => c.PageNumber).Should().Equal(1, null);
        saved!.Select(c => c.Embedding.ToArray()[0]).Should().Equal(0.25f, 0.5f);
        VerifyStoredInTransaction(manual, 2);
        _transactionMock.Verify(x => x.CommitAsync(_token), Times.Once);
        VerifyEmbedded(["first", "second"]);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(_token), Times.Exactly(2));
        _loggerMock.VerifyLog(LogLevel.Information, "Indexed manual 5 into 2 chunk(s)");
        VerifyExtractionPipeline(manual);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EnqueuePendingAsync_ShouldEnqueueEachManualId_WhenManualsArePending()
    {
        var first = CreateManual(5);
        var second = CreateManual(8);
        _manualRepoMock
            .Setup(x => x.ListAsync(It.IsAny<ManualsToIndexSpec>(), _token))
            .ReturnsAsync(new List<Manual> { first, second });

        await _service.EnqueuePendingAsync(_token);

        first.IndexStatus.Should().Be(ManualIndexStatus.Pending);
        _queueMock.Verify(x => x.Enqueue(5), Times.Once);
        _queueMock.Verify(x => x.Enqueue(8), Times.Once);
        _manualRepoMock.Verify(x => x.ListAsync(It.IsAny<ManualsToIndexSpec>(), _token), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Enqueued 2 manual(s) for indexing");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EnqueuePendingAsync_ShouldLogASingleManual()
    {
        _manualRepoMock
            .Setup(x => x.ListAsync(It.IsAny<ManualsToIndexSpec>(), _token))
            .ReturnsAsync(new List<Manual> { CreateManual(5) });

        await _service.EnqueuePendingAsync(_token);

        _queueMock.Invocations.Should().ContainSingle();
        _queueMock.Verify(x => x.Enqueue(5), Times.Once);
        _manualRepoMock.Verify(x => x.ListAsync(It.IsAny<ManualsToIndexSpec>(), _token), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Enqueued 1 manual(s) for indexing");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EnqueuePendingAsync_ShouldNotEnqueue_WhenNoManualsArePending()
    {
        _manualRepoMock
            .Setup(x => x.ListAsync(It.IsAny<ManualsToIndexSpec>(), _token))
            .ReturnsAsync(new List<Manual>());

        var act = () => _service.EnqueuePendingAsync(_token);

        await act.Should().NotThrowAsync();
        _manualRepoMock.Verify(x => x.ListAsync(It.IsAny<ManualsToIndexSpec>(), _token), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IndexAsync_ShouldMarkFailedWithExceptionMessage_WhenPdfReadThrows()
    {
        var manual = CreateManual();
        var failure = new IOException("disk unreadable");
        _manualRepoMock.Setup(x => x.GetByIdAsync(manual.Id)).ReturnsAsync(manual);
        _diskProviderMock.Setup(x => x.OpenRead(PdfPath)).Throws(failure);

        await _service.IndexAsync(manual.Id, _token);

        manual.IndexStatus.Should().Be(ManualIndexStatus.Failed);
        manual.IndexError.Should().Be("disk unreadable");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(_token), Times.Exactly(2));
        _manualRepoMock.Verify(x => x.GetByIdAsync(manual.Id), Times.Once);
        _factoryMock.Verify(x => x.EnsureModelsAvailableAsync(_token), Times.Once);
        _diskProviderMock.Verify(x => x.OpenRead(PdfPath), Times.Once);
        _unitOfWorkMock.Verify(x => x.DiscardPendingInserts(), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Error, "Failed to index manual 5", failure);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IndexAsync_ShouldNotThrow_WhenPersistingTheFailureAlsoThrows()
    {
        var manual = CreateManual();
        var failure = new InvalidOperationException("models unavailable");
        var saveFailure = new InvalidOperationException("database gone");
        _manualRepoMock.Setup(x => x.GetByIdAsync(manual.Id)).ReturnsAsync(manual);
        _factoryMock
            .Setup(x => x.EnsureModelsAvailableAsync(_token))
            .ThrowsAsync(failure);
        _unitOfWorkMock
            .SetupSequence(x => x.SaveChangesAsync(_token))
            .ReturnsAsync(1)
            .ThrowsAsync(saveFailure);

        var act = () => _service.IndexAsync(manual.Id, _token);

        await act.Should().NotThrowAsync();
        manual.IndexStatus.Should().Be(ManualIndexStatus.Failed);
        manual.IndexError.Should().Be("models unavailable");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(_token), Times.Exactly(2));
        _manualRepoMock.Verify(x => x.GetByIdAsync(manual.Id), Times.Once);
        _factoryMock.Verify(x => x.EnsureModelsAvailableAsync(_token), Times.Once);
        _unitOfWorkMock.Verify(x => x.DiscardPendingInserts(), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Error, "Failed to index manual 5", failure);
        _loggerMock.VerifyLog(LogLevel.Error, "Failed to record indexing failure for manual 5", saveFailure);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IndexAsync_ShouldMarkFailedWithoutOpeningFile_WhenStoredFileNameEscapesManualsDirectory()
    {
        var manual = new Manual("Base Rules", Path.Combine("..", "evil.pdf"), "application/pdf", 100, 1, Now)
        {
            Id = 5
        };
        _manualRepoMock.Setup(x => x.GetByIdAsync(manual.Id)).ReturnsAsync(manual);

        await _service.IndexAsync(manual.Id, _token);

        manual.IndexStatus.Should().Be(ManualIndexStatus.Failed);
        manual.IndexError.Should().Contain("evil.pdf").And.Contain("not found");
        _manualRepoMock.Verify(x => x.GetByIdAsync(manual.Id), Times.Once);
        _factoryMock.Verify(x => x.EnsureModelsAvailableAsync(_token), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(_token), Times.Exactly(2));
        _unitOfWorkMock.Verify(x => x.DiscardPendingInserts(), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Error, "Failed to index manual 5");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IndexAsync_ManualNotFound_DoesNothing()
    {
        _manualRepoMock.Setup(x => x.GetByIdAsync(999)).ReturnsAsync((Manual?)null);

        var act = () => _service.IndexAsync(999, _token);

        await act.Should().NotThrowAsync();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(_token), Times.Never);
        _manualRepoMock.Verify(x => x.GetByIdAsync(999), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Warning, "Manual 999 not found for indexing");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IndexAsync_ShouldEmbedInBatches_WhenManualHasManyChunks()
    {
        var manual = CreateManual();
        _manualRepoMock.Setup(x => x.GetByIdAsync(manual.Id)).ReturnsAsync(manual);
        var chunks = Enumerable.Range(0, 70).Select(i => new TextChunk(i, $"chunk {i}", i + 1)).ToArray();
        SetupChunks(chunks);
        var batches = chunks.Chunk(32).ToList();
        foreach (var batch in batches)
        {
            SetupEmbeddings(batch.Select(c => c.Content).ToArray(), batch.Select(c => EmbeddingOf(c.Index)).ToArray());
        }

        List<ManualChunk>? saved = null;
        _chunkWriteRepoMock
            .Setup(x => x.CreateRangeAsync(It.Is<List<ManualChunk>>(l => l.Count == 70)))
            .Callback((List<ManualChunk> stored) => saved = stored)
            .Returns(Task.CompletedTask);

        await _service.IndexAsync(manual.Id, _token);

        manual.IndexStatus.Should().Be(ManualIndexStatus.Indexed);
        manual.IndexedChunkCount.Should().Be(70);
        batches.Select(b => b.Length).Should().Equal(32, 32, 6);
        foreach (var batch in batches)
        {
            VerifyEmbedded(batch.Select(c => c.Content).ToArray());
        }

        saved!.Select(c => c.ChunkIndex).Should().Equal(Enumerable.Range(0, 70));
        saved!.Select(c => c.Embedding.ToArray()[0]).Should().Equal(Enumerable.Range(0, 70).Select(i => (float)i));
        VerifyStoredInTransaction(manual, 70);
        _transactionMock.Verify(x => x.CommitAsync(_token), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(_token), Times.Exactly(2));
        _loggerMock.VerifyLog(LogLevel.Information, "Indexed manual 5 into 70 chunk(s)");
        VerifyExtractionPipeline(manual);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IndexAsync_ShouldEmbedOneBatch_WhenTheChunksFitExactly()
    {
        var manual = CreateManual();
        _manualRepoMock.Setup(x => x.GetByIdAsync(manual.Id)).ReturnsAsync(manual);
        var chunks = Enumerable.Range(0, 32).Select(i => new TextChunk(i, $"chunk {i}", 1)).ToArray();
        var contents = chunks.Select(c => c.Content).ToArray();
        SetupChunks(chunks);
        SetupEmbeddings(contents, chunks.Select(c => EmbeddingOf(c.Index)).ToArray());

        await _service.IndexAsync(manual.Id, _token);

        manual.IndexedChunkCount.Should().Be(32);
        VerifyEmbedded(contents);
        VerifyStoredInTransaction(manual, 32);
        _transactionMock.Verify(x => x.CommitAsync(_token), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(_token), Times.Exactly(2));
        _loggerMock.VerifyLog(LogLevel.Information, "Indexed manual 5 into 32 chunk(s)");
        VerifyExtractionPipeline(manual);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IndexAsync_ShouldRethrowAndLeaveManualIndexing_WhenCancelledDuringShutdown()
    {
        var manual = CreateManual();
        _manualRepoMock.Setup(x => x.GetByIdAsync(manual.Id)).ReturnsAsync(manual);
        SetupChunks(new TextChunk(0, "chunk", 1));
        _embedderMock
            .Setup(x => x.GenerateAsync(It.Is<IEnumerable<string>>(v => v.SequenceEqual(new[] { "chunk" })), null, _token))
            .Returns(() =>
            {
                _cts.Cancel();
                throw new OperationCanceledException(_token);
            });

        var act = async () => await _service.IndexAsync(manual.Id, _token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        manual.IndexStatus.Should().Be(ManualIndexStatus.Indexing);
        manual.IndexError.Should().BeNull();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(_token), Times.Once);
        _factoryMock.Verify(x => x.CreateEmbeddingGeneratorAsync(_token), Times.Once);
        VerifyEmbedded(["chunk"]);
        _loggerMock.VerifyLog(LogLevel.Information, "Indexing of manual 5 was interrupted and will resume on the next start");
        VerifyExtractionPipeline(manual);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IndexAsync_ShouldMarkFailed_WhenARequestTimesOutWithoutTheCallerCancelling()
    {
        var manual = CreateManual();
        var timeout = new TaskCanceledException("The request timed out");
        _manualRepoMock.Setup(x => x.GetByIdAsync(manual.Id)).ReturnsAsync(manual);
        SetupChunks(new TextChunk(0, "chunk", 1));
        _embedderMock
            .Setup(x => x.GenerateAsync(It.Is<IEnumerable<string>>(v => v.SequenceEqual(new[] { "chunk" })), null, _token))
            .ThrowsAsync(timeout);

        await _service.IndexAsync(manual.Id, _token);

        manual.IndexStatus.Should().Be(ManualIndexStatus.Failed);
        manual.IndexError.Should().Be("The request timed out");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(_token), Times.Exactly(2));
        _unitOfWorkMock.Verify(x => x.DiscardPendingInserts(), Times.Once);
        _factoryMock.Verify(x => x.CreateEmbeddingGeneratorAsync(_token), Times.Once);
        VerifyEmbedded(["chunk"]);
        _loggerMock.VerifyLog(LogLevel.Error, "Failed to index manual 5", timeout);
        VerifyExtractionPipeline(manual);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IndexAsync_ShouldNotCommit_WhenSavingChunksFails()
    {
        var manual = CreateManual();
        var failure = new InvalidOperationException("db boom");
        _manualRepoMock.Setup(x => x.GetByIdAsync(manual.Id)).ReturnsAsync(manual);
        SetupChunks(new TextChunk(0, "chunk", 1));
        SetupEmbeddings(["chunk"], EmbeddingOf(1));
        _unitOfWorkMock
            .SetupSequence(x => x.SaveChangesAsync(_token))
            .ReturnsAsync(1)
            .ThrowsAsync(failure)
            .ReturnsAsync(1);

        await _service.IndexAsync(manual.Id, _token);

        manual.IndexStatus.Should().Be(ManualIndexStatus.Failed);
        manual.IndexError.Should().Be("db boom");
        _transactionMock.Verify(x => x.CommitAsync(_token), Times.Never);
        _unitOfWorkMock.Verify(x => x.DiscardPendingInserts(), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(_token), Times.Exactly(3));
        VerifyStoredInTransaction(manual, 1);
        VerifyEmbedded(["chunk"]);
        _loggerMock.VerifyLog(LogLevel.Error, "Failed to index manual 5", failure);
        VerifyExtractionPipeline(manual);
        VerifyNoOtherCalls();
    }

    private static Manual CreateManual(int id = 5)
    {
        return new Manual("Base Rules", "stored.pdf", "application/pdf", 100, 3, Now)
        {
            Id = id
        };
    }
}
