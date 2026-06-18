using FoundryAgentLocal.Worker;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FoundryAgentLocal.Tests;

public class OllamaAgentRunnerTests
{
    [Fact]
    public async Task OllamaAgentRunner_WritesResearchFile_WhenChatClientResponds()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            var mockChat = new Mock<IChatClient>();
            mockChat
                .Setup(c => c.GetResponseAsync(
                    It.IsAny<IEnumerable<ChatMessage>>(),
                    It.IsAny<ChatOptions?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ChatResponse(
                    new ChatMessage(ChatRole.Assistant, "Mocked research output for test.")));

            var runner = new OllamaAgentRunner(mockChat.Object, NullLogger<OllamaAgentRunner>.Instance);
            await runner.RunAsync(tmpDir);

            var outputFile = Path.Combine(tmpDir, "research.txt");
            Assert.True(File.Exists(outputFile));
            var content = await File.ReadAllTextAsync(outputFile);
            Assert.Contains("Mocked research output for test.", content);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public async Task OllamaAgentRunner_CallsChatClient_WithNonEmptyMessages()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), "quantum computing trends");
        Directory.CreateDirectory(tmpDir);
        try
        {
            IEnumerable<ChatMessage>? capturedMessages = null;

            var mockChat = new Mock<IChatClient>();
            mockChat
                .Setup(c => c.GetResponseAsync(
                    It.IsAny<IEnumerable<ChatMessage>>(),
                    It.IsAny<ChatOptions?>(),
                    It.IsAny<CancellationToken>()))
                .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>(
                    (msgs, _, _) => capturedMessages = msgs)
                .ReturnsAsync(new ChatResponse(
                    new ChatMessage(ChatRole.Assistant, "Some output")));

            var runner = new OllamaAgentRunner(mockChat.Object, NullLogger<OllamaAgentRunner>.Instance);
            await runner.RunAsync(tmpDir);

            Assert.NotNull(capturedMessages);
            Assert.NotEmpty(capturedMessages);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact(Skip = "Integration test — requires Ollama running at localhost:11434")]
    [Trait("Category", "Integration")]
    public async Task OllamaAgentRunner_Integration_WritesResearchFile()
    {
        using var client = new OllamaSharp.OllamaApiClient(
            new Uri("http://localhost:11434"), "gemma3:12b");

        var tmpDir = Path.Combine(Path.GetTempPath(), "climate policy");
        Directory.CreateDirectory(tmpDir);
        try
        {
            var runner = new OllamaAgentRunner(client, NullLogger<OllamaAgentRunner>.Instance);
            await runner.RunAsync(tmpDir);

            var outputFile = Path.Combine(tmpDir, "research.txt");
            Assert.True(File.Exists(outputFile));
            var content = await File.ReadAllTextAsync(outputFile);
            Assert.NotEmpty(content);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }
}
