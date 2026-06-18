using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace FoundryAgentLocal.Worker;

public sealed class OllamaAgentRunner : IAgentRunner
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<OllamaAgentRunner> _logger;

    public OllamaAgentRunner(IChatClient chatClient, ILogger<OllamaAgentRunner> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task RunAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        var folderName = Path.GetFileName(folderPath);
        _logger.LogInformation("Running Ollama agent for topic: {Topic}", folderName);

        var agent = new ChatClientAgent(
            chatClient: _chatClient,
            instructions: ResearchPromptBuilder.SystemInstructions,
            name: "ResearchAgent",
            description: "Local Ollama research assistant");

        var response = await agent.RunAsync(
            ResearchPromptBuilder.Build(folderName),
            session: null,
            options: null,
            cancellationToken);

        var outputPath = Path.Combine(folderPath, "research.txt");
        await File.WriteAllTextAsync(outputPath, response.Text, cancellationToken);
        _logger.LogInformation("Research written to: {Path}", outputPath);
    }
}
