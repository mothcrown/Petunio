using Discord;
using Discord.Commands;
using Petunio.Interfaces;

namespace Petunio.DiscordCommands;

public class GetMemoriesCommand(ILogger<GetMemoriesCommand> logger, IConfiguration configuration, IMemoryService memoryService) : ModuleBase<SocketCommandContext>
{
    [Command("get-memories")]
    [Summary("Gets all memories from Petunio")]
    public async Task ExecuteAsync()
    {
        var ownerId = configuration.GetValue<ulong>("Discord:UserId");
        if (Context.User.Id != ownerId) await ReplyAsync("Acceso no permitido");
        var routes = String.Join("\\n", await memoryService.GetAllMemoriesAsync());
        logger.LogInformation("Petunio's memories accessed");
        var reply = new EmbedBuilder() { Title = "Memorias", Color = Color.Teal, Description = routes };
        await ReplyAsync("", false, reply.Build());
    }
}