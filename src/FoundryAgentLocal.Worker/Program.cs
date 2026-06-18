using FoundryAgentLocal.Worker;
using Microsoft.Extensions.AI;
using OllamaSharp;

var builder = Host.CreateApplicationBuilder(args);
var config = builder.Configuration;

if (!string.IsNullOrWhiteSpace(config["Foundry:ProjectEndpoint"]))
{
    builder.Services.AddSingleton<IAgentRunner, AgentRunner>();
}
else if (!string.IsNullOrWhiteSpace(config["Ollama:Endpoint"]))
{
    builder.Services.AddSingleton<IChatClient>(sp =>
    {
        var cfg = sp.GetRequiredService<IConfiguration>();
        var model = cfg["Ollama:Model"] ?? "gemma3:12b";
        return new OllamaApiClient(new Uri(cfg["Ollama:Endpoint"]!), model);
    });
    builder.Services.AddSingleton<IAgentRunner, OllamaAgentRunner>();
}
else
{
    builder.Services.AddSingleton<IAgentRunner, FakeAgentRunner>();
}

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
