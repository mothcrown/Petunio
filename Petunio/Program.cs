using Petunio;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/petunio.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Services.AddOpenApi();
builder.AddServices();

builder.Host.UseSerilog();

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