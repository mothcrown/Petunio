using Discord.WebSocket;

namespace Petunio.Interfaces;

public interface IDiscordService
{
    public Task StartAsync();
    
    public Task StopAsync();

    public Task MessageReceivedAsync(SocketMessage socketMessage);
}