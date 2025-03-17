using Discord.Commands;
using Petunio.Interfaces;

namespace Petunio.DiscordCommands;

public class RouteCommand(ILogger<RouteCommand> logger, IWalkChallengeService walkChallengeService) : ModuleBase<SocketCommandContext>
{
    
    [Command("route")]
    [Summary("Sets route as active")]
    public async Task ExecuteAsync([Remainder][Summary("Name of route")] string route)
    {
        if (!walkChallengeService.RouteExists(route)) await ReplyAsync($"Route '{route}' does not exist");
        walkChallengeService.SetRouteActive(Context.User.Id.ToString(), route);
        await ReplyAsync($"Route '{route}' is now active!");
    }
}