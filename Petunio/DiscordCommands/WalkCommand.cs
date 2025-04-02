using Discord;
using Discord.Commands;
using Petunio.Interfaces;

namespace Petunio.DiscordCommands;

public class WalkCommand(ILogger<WalkCommand> logger, IWalkChallengeService walkChallengeService, IImageGenerationService imageGenerationService) : ModuleBase<SocketCommandContext>
{
    [Command("walk")]
    [Summary("Updates your walked distance along your active route")]
    public async Task ExecuteAsync([Remainder][Summary("Distance in km")] float distance)
    {
        if (!ValidDistance(distance)) await ReplyAsync("Incorrect value!");
        var milepost = await walkChallengeService.UpdateTrack(Context.User.Id.ToString(), distance);
        logger.LogInformation($"Track updated for user {Context.User.Id}: {milepost}");
        var image = await imageGenerationService.GenerateImageAsync("lotr, hobbits walking, " + milepost);
        var reply = new EmbedBuilder { Title = "Onwards!", Color = Color.DarkGreen, Description = milepost};
        await Context.Channel.SendFileAsync(image);
        await ReplyAsync("", false, reply.Build());
    }

    private bool ValidDistance(float distance)
    {
        return distance > 0;
    }
}