using FoundryAgentLocal.Worker;

var builder = Host.CreateApplicationBuilder(args);

var useRealAgent = !string.IsNullOrWhiteSpace(
    builder.Configuration["Foundry:ProjectEndpoint"]);

if (useRealAgent)
    builder.Services.AddSingleton<IAgentRunner, AgentRunner>();
else
    builder.Services.AddSingleton<IAgentRunner, FakeAgentRunner>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
