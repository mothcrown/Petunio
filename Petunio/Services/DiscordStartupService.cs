using Petunio.Interfaces;

namespace Petunio.Services;

public class DiscordStartupService(ILogger<DiscordStartupService> logger, IDiscordService discordService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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