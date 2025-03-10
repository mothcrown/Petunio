using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Ollama
var ollama = builder
    .AddDockerfile("ollama", "Ollama", "Ollama.Dockerfile")
    .WithHttpEndpoint(env: "OLLAMA_HTTP_ENDPOINT", port: 11434, targetPort: 11434);

builder.AddProject<Petunio>("petunio").WaitFor(ollama);

builder.Build().Run();