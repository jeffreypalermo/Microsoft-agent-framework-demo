namespace FoundryAgentLocal.Worker;

public interface IAgentRunner
{
    Task RunAsync(string folderPath, CancellationToken cancellationToken = default);
}
