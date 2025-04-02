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
    .WithHttpEndpoint(env: "CHROMA_HTTP_ENDPOINT", port: 8000, targetPort: 8000)
    .WaitFor(ollama);

// Petunio
var petunio = builder.AddProject<Petunio>("petunio").WaitFor(chroma);

// ComfyUI
var comfyUI = builder
    .AddDockerfile("comfyUI", "ComfyUI", "Dockerfile")
    .WithVolume("comfyUI", "/env")
    .WithHttpEndpoint(env: "COMFYUI_HTTP_ENDPOINT", port: 8188, targetPort: 8188, isProxied: false)
    .WithContainerRuntimeArgs("--gpus=all")
    .WaitFor(petunio);

builder.Build().Run();