using Petunio.Interfaces;
using Petunio.Services;
using DateTime = Petunio.Services.DateTime;

namespace Petunio;

public static class DependencyInjection
{
    public static void AddServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IDateTime, DateTime>();
        builder.Services.AddSingleton<IDiscordService, DiscordService>();
        builder.Services.AddSingleton<IOllamaService, OllamaService>();
        builder.Services.AddSingleton<IChromaDbService, ChromaDbService>();
        builder.Services.AddSingleton<IMemoryService, MemoryService>();
        builder.Services.AddSingleton<IPromptService, PromptService>();
        builder.Services.AddHostedService<StartupService>();
    }
}