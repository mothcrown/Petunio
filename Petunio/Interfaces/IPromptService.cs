using Petunio.Models;

namespace Petunio.Interfaces;

public interface IPromptService
{
    public Task<PetunioResponse?> ProcessDiscordInputAsync(string input);
    
    public Task ProcessQuartzInputAsync(string input);
}