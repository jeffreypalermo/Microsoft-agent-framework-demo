using FoundryAgentLocal.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WorkerService = FoundryAgentLocal.Worker.Worker;

namespace FoundryAgentLocal.Tests;

public class WorkerTests
{
    [Fact]
    public async Task Worker_OnNewDirectory_CallsAgentRunner()
    {
        var watchPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(watchPath);

        var called = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        var mockRunner = new Mock<IAgentRunner>();
        mockRunner
            .Setup(r => r.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((p, _) => called.TrySetResult(p))
            .Returns(Task.CompletedTask);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["WatchFolder:Path"] = watchPath
            })
            .Build();

        var worker = new WorkerService(mockRunner.Object, config, NullLogger<WorkerService>.Instance);

        using var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);
        // On .NET 10, BackgroundService.StartAsync dispatches ExecuteAsync via Task.Run,
        // so the FileSystemWatcher isn't set up until after StartAsync returns.
        await Task.Delay(200);

        var newFolder = Path.Combine(watchPath, "test topic");
        Directory.CreateDirectory(newFolder);

        var capturedPath = await called.Task.WaitAsync(TimeSpan.FromSeconds(10));
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(newFolder, capturedPath);
        mockRunner.Verify(
            r => r.RunAsync(It.Is<string>(p => p == newFolder), It.IsAny<CancellationToken>()),
            Times.Once);

        Directory.Delete(watchPath, recursive: true);
    }

    [Fact]
    public async Task Worker_FileCreated_DoesNotCallAgentRunner()
    {
        var watchPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(watchPath);

        var mockRunner = new Mock<IAgentRunner>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["WatchFolder:Path"] = watchPath
            })
            .Build();

        var worker = new WorkerService(mockRunner.Object, config, NullLogger<WorkerService>.Instance);

        using var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);

        await File.WriteAllTextAsync(Path.Combine(watchPath, "somefile.txt"), "hello");

        await Task.Delay(500);
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        mockRunner.Verify(
            r => r.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        Directory.Delete(watchPath, recursive: true);
    }
}
