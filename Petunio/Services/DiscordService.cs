using Discord.Commands;
using Discord.WebSocket;
using Petunio.Interfaces;

namespace Petunio.Services;

public class DiscordService(ILogger<DiscordService> logger, IConfiguration configuration,
    DiscordSocketClient discordSocketClient, CommandService commandService) : IDiscordService
{
    public Task StartAsync(ServiceProvider services)
    {
        throw new NotImplementedException();
    }

    public Task StopAsync()
    {
        throw new NotImplementedException();
    }
    
    public Task ReceiveMessage(string message)
    {
        throw new NotImplementedException();
    }

    public Task SendMessage(string message)
    {
        throw new NotImplementedException();
    }
}