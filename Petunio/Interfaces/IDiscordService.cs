namespace Petunio.Interfaces;

public interface IDiscordService
{
    public Task StartAsync(ServiceProvider services);
    
    public Task StopAsync();
    
    public Task SendMessage(string message);
}