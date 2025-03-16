namespace Petunio.Interfaces;

public interface IPromptService
{
    public Task<List<string?>> ProcessDiscordInputAsync(string input);
    
    public Task ProcessQuartzInputAsync(string input);
}