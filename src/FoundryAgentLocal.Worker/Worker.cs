namespace FoundryAgentLocal.Worker;

public sealed class Worker : BackgroundService
{
    private readonly IAgentRunner _agentRunner;
    private readonly IConfiguration _configuration;
    private readonly ILogger<Worker> _logger;
    private readonly HashSet<string> _recentlyProcessed = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _debounceLock = new();
    private FileSystemWatcher? _watcher;

    public Worker(IAgentRunner agentRunner, IConfiguration configuration, ILogger<Worker> logger)
    {
        _agentRunner = agentRunner;
        _configuration = configuration;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var watchPath = _configuration["WatchFolder:Path"]
            ?? throw new InvalidOperationException("WatchFolder:Path is not configured.");

        if (!Directory.Exists(watchPath))
            Directory.CreateDirectory(watchPath);

        _watcher = new FileSystemWatcher(watchPath)
        {
            NotifyFilter = NotifyFilters.DirectoryName,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };

        _watcher.Created += (_, e) => OnDirectoryCreated(e.FullPath, stoppingToken);
        _logger.LogInformation("Watching for new folders in: {Path}", watchPath);

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _watcher?.Dispose();
        _watcher = null;
        base.Dispose();
    }

    private void OnDirectoryCreated(string fullPath, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(fullPath))
            return;

        bool shouldProcess;
        lock (_debounceLock)
        {
            shouldProcess = _recentlyProcessed.Add(fullPath);
        }

        if (!shouldProcess)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await _agentRunner.RunAsync(fullPath, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Agent run failed for folder: {Path}", fullPath);
            }
            finally
            {
                await Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None);
                lock (_debounceLock)
                {
                    _recentlyProcessed.Remove(fullPath);
                }
            }
        }, cancellationToken);
    }
}
