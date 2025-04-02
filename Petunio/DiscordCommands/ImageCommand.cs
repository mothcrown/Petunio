using Discord;
using Discord.Commands;
using Petunio.Interfaces;

namespace Petunio.DiscordCommands;

public class ImageCommand(ILogger<RouteCommand> logger, IImageGenerationService imageGenerationService) : ModuleBase<SocketCommandContext>
{
    
    [Command("image")]
    [Summary("Generates an image")]
    public async Task ExecuteAsync([Remainder][Summary("Description of image")] string description)
    {
        var image = await imageGenerationService.GenerateImageAsync(description);
        logger.LogInformation("Generated image");
        await Context.Channel.SendFileAsync(image);
    }
}