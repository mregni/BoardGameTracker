using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Specification;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Rag;
using BoardGameTracker.Core.Rag.Interfaces;
using BoardGameTracker.Core.Rag.Specifications;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Moq;
using Pgvector;
using Xunit;

namespace BoardGameTracker.Tests.Rag;

public class RagServiceTests
{
    private readonly Mock<IReadRepository<ManualChunk>> _chunkRepoMock = new();
    private readonly Mock<IRepository<Manual>> _manualRepoMock = new();
    private readonly Mock<IAiClientFactory> _factoryMock = new();
    private readonly Mock<IRagSettingsProvider> _settingsMock = new();
    private readonly Mock<IEmbeddingGenerator<string, Embedding<float>>> _embedderMock = new();
    private readonly Mock<IChatClient> _chatMock = new();
    private readonly RagService _service;

    public RagServiceTests()
    {
        _settingsMock.Setup(x => x.GetAsync())
            .ReturnsAsync(new RagSettings("ollama", "http://ollama:11434", "qwen3:4b", null, "http://ollama:11434", "bge-m3", 1024, -1, 5));
        _factoryMock.Setup(x => x.CreateEmbeddingGeneratorAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_embedderMock.Object);
        _factoryMock.Setup(x => x.CreateChatClientAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_chatMock.Object);
        _embedderMock.Setup(x => x.GenerateAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<EmbeddingGenerationOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeneratedEmbeddings<Embedding<float>>(new[] { new Embedding<float>(new float[1024]) }));

        _service = new RagService(_chunkRepoMock.Object, _manualRepoMock.Object, _factoryMock.Object, _settingsMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _chunkRepoMock.VerifyNoOtherCalls();
        _manualRepoMock.VerifyNoOtherCalls();
        _factoryMock.VerifyNoOtherCalls();
        _settingsMock.VerifyNoOtherCalls();
        _embedderMock.VerifyNoOtherCalls();
        _chatMock.VerifyNoOtherCalls();
    }

    private void VerifyRetrievalPipeline(string question)
    {
        _settingsMock.Verify(x => x.GetAsync(), Times.Once);
        _factoryMock.Verify(x => x.CreateEmbeddingGeneratorAsync(It.IsAny<CancellationToken>()), Times.Once);
        _embedderMock.Verify(
            x => x.GenerateAsync(
                It.Is<IEnumerable<string>>(v => v.Single() == question),
                It.IsAny<EmbeddingGenerationOptions?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _chunkRepoMock.Verify(
            x => x.ListAsync(It.Is<ISpecification<ManualChunk, ManualChunkMatch>>(s => s is NearestManualChunksSpec && s.Take == 5), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AskAsync_EmptyQuestion_ReturnsNoContextWithoutCallingModels()
    {
        var result = await _service.AskAsync(1, "   ");

        result.HasContext.Should().BeFalse();
        result.Answer.Should().Be("I couldn't find anything about that in the indexed rulebook(s) for this game.");
        result.Citations.Should().BeEmpty();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AskAsync_NoMatches_ReturnsNoContextWithoutCallingChat()
    {
        _chunkRepoMock
            .Setup(x => x.ListAsync(It.Is<ISpecification<ManualChunk, ManualChunkMatch>>(s => s is NearestManualChunksSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ManualChunkMatch>());

        var result = await _service.AskAsync(1, "how many cards?");

        result.HasContext.Should().BeFalse();
        result.Answer.Should().Be("I couldn't find anything about that in the indexed rulebook(s) for this game.");
        result.Citations.Should().BeEmpty();
        VerifyRetrievalPipeline("how many cards?");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AskAsync_WithMatches_ScopesByGameIdAndReturnsAnswerWithDedupedCitations()
    {
        const int gameId = 42;
        var chunk1 = CreateChunk(1, gameId, 3, "You start with 7 cards.");
        var chunk2 = CreateChunk(1, gameId, 3, "More about the draw phase.");
        var chunk3 = CreateChunk(2, gameId, 5, "Expansion setup rule.");

        NearestManualChunksSpec? capturedSpec = null;
        _chunkRepoMock
            .Setup(x => x.ListAsync(It.Is<ISpecification<ManualChunk, ManualChunkMatch>>(s => s is NearestManualChunksSpec && s.Take == 5), It.IsAny<CancellationToken>()))
            .Callback<ISpecification<ManualChunk, ManualChunkMatch>, CancellationToken>((spec, _) => capturedSpec = (NearestManualChunksSpec) spec)
            .ReturnsAsync(new List<ManualChunkMatch>
            {
                new(chunk1, 0.10),
                new(chunk2, 0.20),
                new(chunk3, 0.30)
            });
        _manualRepoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(CreateManual(1, "Base Rules"));
        _manualRepoMock.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(CreateManual(2, "Expansion Rules"));

        List<ChatMessage>? capturedMessages = null;
        ChatOptions? capturedOptions = null;
        _chatMock
            .Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>((messages, options, _) =>
            {
                capturedMessages = messages.ToList();
                capturedOptions = options;
            })
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "You start with 7 cards (page 3).")));

        var result = await _service.AskAsync(gameId, "how many cards?");

        result.HasContext.Should().BeTrue();
        result.Answer.Should().Contain("7 cards");
        result.Citations.Should().HaveCount(2);

        var baseCitation = result.Citations.Should().ContainSingle(c => c.ManualId == 1).Subject;
        baseCitation.Title.Should().Be("Base Rules");
        baseCitation.Page.Should().Be(3);
        baseCitation.Snippet.Should().Be("You start with 7 cards.");
        baseCitation.Score.Should().Be(0.9);
        baseCitation.ImageUrl.Should().Be("manual/1/page/3/image");

        var expansionCitation = result.Citations.Should().ContainSingle(c => c.ManualId == 2).Subject;
        expansionCitation.Title.Should().Be("Expansion Rules");
        expansionCitation.Page.Should().Be(5);
        expansionCitation.Snippet.Should().Be("Expansion setup rule.");
        expansionCitation.Score.Should().Be(0.7);
        expansionCitation.ImageUrl.Should().Be("manual/2/page/5/image");

        capturedMessages.Should().NotBeNull().And.HaveCount(2);
        capturedMessages![0].Role.Should().Be(ChatRole.System);
        capturedMessages[1].Role.Should().Be(ChatRole.User);
        capturedMessages[1].Text.Should().Contain("how many cards?")
            .And.Contain("[1] (page 3) You start with 7 cards.")
            .And.Contain("[2] (page 3) More about the draw phase.")
            .And.Contain("[3] (page 5) Expansion setup rule.");
        capturedOptions.Should().NotBeNull();
        capturedOptions!.Temperature.Should().Be(0.2f);

        capturedSpec.Should().NotBeNull();
        capturedSpec!.IsSatisfiedBy(CreateChunk(1, gameId, 3, "in scope")).Should().BeTrue();
        capturedSpec.IsSatisfiedBy(CreateChunk(1, gameId + 1, 3, "other game")).Should().BeFalse();

        VerifyRetrievalPipeline("how many cards?");
        _factoryMock.Verify(x => x.CreateChatClientAsync(It.IsAny<CancellationToken>()), Times.Once);
        _manualRepoMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _manualRepoMock.Verify(x => x.GetByIdAsync(2), Times.Once);
        _chatMock.Verify(
            x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AskAsync_ShouldScopeToSingleManual_WhenManualIdIsProvided()
    {
        const int gameId = 42;
        NearestManualChunksSpec? capturedSpec = null;
        _chunkRepoMock
            .Setup(x => x.ListAsync(It.Is<ISpecification<ManualChunk, ManualChunkMatch>>(s => s is NearestManualChunksSpec), It.IsAny<CancellationToken>()))
            .Callback<ISpecification<ManualChunk, ManualChunkMatch>, CancellationToken>((spec, _) => capturedSpec = (NearestManualChunksSpec) spec)
            .ReturnsAsync(new List<ManualChunkMatch>());

        var result = await _service.AskAsync(gameId, "how many cards?", manualId: 7);

        result.HasContext.Should().BeFalse();
        capturedSpec.Should().NotBeNull();
        capturedSpec!.IsSatisfiedBy(CreateChunk(7, gameId, 3, "target manual")).Should().BeTrue();
        capturedSpec.IsSatisfiedBy(CreateChunk(8, gameId, 3, "other manual")).Should().BeFalse();
        VerifyRetrievalPipeline("how many cards?");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AskAsync_ShouldBuildFallbackCitationFields_WhenManualIsMissingAndPageIsUnknown()
    {
        const int gameId = 42;
        var chunk = new ManualChunk(9, gameId, 0, new string('x', 300), null, new Vector(new float[1024]));
        _chunkRepoMock
            .Setup(x => x.ListAsync(It.Is<ISpecification<ManualChunk, ManualChunkMatch>>(s => s is NearestManualChunksSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ManualChunkMatch> { new(chunk, 0.25) });
        _manualRepoMock.Setup(x => x.GetByIdAsync(9)).ReturnsAsync((Manual?)null);

        List<ChatMessage>? capturedMessages = null;
        _chatMock
            .Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>((messages, _, _) => capturedMessages = messages.ToList())
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "answer")));

        var result = await _service.AskAsync(gameId, "how many cards?");

        result.HasContext.Should().BeTrue();
        var citation = result.Citations.Should().ContainSingle().Subject;
        citation.ManualId.Should().Be(9);
        citation.Title.Should().BeEmpty();
        citation.Page.Should().BeNull();
        citation.ImageUrl.Should().BeNull();
        citation.Snippet.Should().Be(new string('x', 240) + "…");
        citation.Score.Should().Be(0.75);
        capturedMessages.Should().NotBeNull();
        capturedMessages![1].Text.Should().Contain("(unknown page)");

        VerifyRetrievalPipeline("how many cards?");
        _factoryMock.Verify(x => x.CreateChatClientAsync(It.IsAny<CancellationToken>()), Times.Once);
        _manualRepoMock.Verify(x => x.GetByIdAsync(9), Times.Once);
        _chatMock.Verify(
            x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        VerifyNoOtherCalls();
    }

    private static ManualChunk CreateChunk(int manualId, int gameId, int page, string content) =>
        new(manualId, gameId, 0, content, page, new Vector(new float[1024]));

    private static Manual CreateManual(int id, string title)
    {
        var manual = new Manual(title, "stored.pdf", "application/pdf", 100, 1, DateTime.UtcNow)
        {
            Id = id
        };
        return manual;
    }
}
