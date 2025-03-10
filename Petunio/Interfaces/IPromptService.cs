namespace Petunio.Interfaces;

public interface IPromptService
{
    public Task<string> ProcessDiscordInputAsync(string input);
    
    public Task ProcessQuartzInputAsync(string input);
    
    public Task ProcessOutputAsync(string output);
}