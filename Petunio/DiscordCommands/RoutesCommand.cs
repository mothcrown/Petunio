using Discord;
using Discord.Commands;
using Petunio.Interfaces;

namespace Petunio.DiscordCommands;

public class RoutesCommand(ILogger<RoutesCommand> logger, IWalkChallengeService walkChallengeService) : ModuleBase<SocketCommandContext>
{
    
    [Command("routes")]
    [Summary("Gets list of routes")]
    public async Task ExecuteAsync()
    {
        var routes = String.Join("\\n", walkChallengeService.GetRoutes());
        var reply = new EmbedBuilder() { Title = "Available routes", Color = Color.DarkGreen, Description = routes };
        await ReplyAsync("", false, reply.Build());
    }
}