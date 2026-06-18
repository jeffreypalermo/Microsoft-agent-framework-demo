using FoundryAgentLocal.Worker;
using Moq;

namespace FoundryAgentLocal.Tests;

public class AgentRunnerTests
{
    [Fact]
    public async Task FakeAgentRunner_WritesResearchFile()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            var runner = new FakeAgentRunner();
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

    [Fact]
    public async Task FakeAgentRunner_IncludesFolderNameInOutput()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), "climate policy impacts");
        Directory.CreateDirectory(tmpDir);
        try
        {
            var runner = new FakeAgentRunner();
            await runner.RunAsync(tmpDir);

            var content = await File.ReadAllTextAsync(Path.Combine(tmpDir, "research.txt"));
            Assert.Contains("climate policy impacts", content);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public async Task MockAgentRunner_CalledWithCorrectFolderPath()
    {
        var expectedPath = "/some/watch/folder/quantum computing";
        var mock = new Mock<IAgentRunner>();
        mock.Setup(r => r.RunAsync(expectedPath, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await mock.Object.RunAsync(expectedPath);

        mock.Verify(r => r.RunAsync(expectedPath, It.IsAny<CancellationToken>()), Times.Once);
    }
}
