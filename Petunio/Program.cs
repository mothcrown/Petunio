using Petunio;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.AddServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => "OK")
    .WithName("CheckHealth");

app.Run();