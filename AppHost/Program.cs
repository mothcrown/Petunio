using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Ollama + Janus
var ollama = builder.AddOllama("ollama").WithOpenWebUI();
var petunio = ollama.AddModel("huihui_ai/phi4-abliterated");

builder.AddProject<Petunio>("petunio").WithReference(petunio).WaitFor(petunio);

builder.Build().Run();