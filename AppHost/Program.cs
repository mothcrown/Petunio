using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Petunio>("petunio");

builder.Build().Run();