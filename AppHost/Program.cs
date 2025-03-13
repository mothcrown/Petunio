using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Ollama
var ollama = builder
    .AddDockerfile("ollama", "Ollama", "Ollama.Dockerfile")
    .WithVolume("ollama-models", "/root/.ollama")
    .WithHttpEndpoint(env: "OLLAMA_HTTP_ENDPOINT", port: 11434, targetPort: 11434)
    .WithContainerRuntimeArgs("--gpus=all");

// Chroma
var chroma = builder
    .AddDockerfile("chroma", "Chroma", "Chroma.Dockerfile")
    .WithVolume("chroma-data", "/data")
    .WithHttpEndpoint(env: "CHROMA_HTTP_ENDPOINT", port: 11434, targetPort: 11434);

// Petunio
builder.AddProject<Petunio>("petunio").WaitFor(ollama).WaitFor(chroma);

builder.Build().Run();