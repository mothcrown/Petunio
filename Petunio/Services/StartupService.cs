using Petunio.Interfaces;

namespace Petunio.Services;

public class StartupService(ILogger<StartupService> logger, IChromaDbService chromaDbService,
    IDiscordService discordService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitChromaDbAsync();
        await StartDiscordAsync();
    }

    private async Task InitChromaDbAsync()
    {
        try
        {
            await chromaDbService.InitializeAsync();
            logger.LogInformation("Chroma Db started");
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            throw;
        }
    }

    private async Task StartDiscordAsync()
    {
        try
        {
            await discordService.StartAsync();
            logger.LogInformation("Connected to Discord");
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            throw;
        }
    }
}