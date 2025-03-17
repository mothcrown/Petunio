using Discord.Commands;

namespace Petunio.DiscordCommands;

public class WalkCommand : ModuleBase<SocketCommandContext>
{
    [Command("walk")]
    [Summary("Updates your walked distance along your active route")]
    public async Task ExecuteAsync([Remainder][Summary("Distance in km")] float distance)
    {
        
    }
}