using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI.Foundry;

namespace FoundryAgentLocal.Worker;

public sealed class AgentRunner : IAgentRunner
{
    private readonly string _projectEndpoint;
    private readonly string _modelDeploymentName;
    private readonly ILogger<AgentRunner> _logger;

    public AgentRunner(IConfiguration configuration, ILogger<AgentRunner> logger)
    {
        _projectEndpoint = configuration["Foundry:ProjectEndpoint"]
            ?? throw new InvalidOperationException("Foundry:ProjectEndpoint is not configured.");
        _modelDeploymentName = configuration["Foundry:ModelDeploymentName"] ?? "gpt-4o-mini";
        _logger = logger;
    }

    public async Task RunAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        var folderName = Path.GetFileName(folderPath);
        _logger.LogInformation("Running agent for topic: {Topic}", folderName);

        var client = new AIProjectClient(new Uri(_projectEndpoint), new AzureCliCredential());
        var agent = client.AsAIAgent(
            model: _modelDeploymentName,
            instructions: ResearchPromptBuilder.SystemInstructions,
            name: "ResearchAgent");

        var userMessage = ResearchPromptBuilder.Build(folderName);
        var response = await agent.RunAsync(userMessage, session: null, options: null, cancellationToken);

        var outputPath = Path.Combine(folderPath, "research.txt");
        await File.WriteAllTextAsync(outputPath, response.Text, cancellationToken);
        _logger.LogInformation("Research written to: {Path}", outputPath);
    }
}
