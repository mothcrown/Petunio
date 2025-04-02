using Discord.Commands;
using Petunio.Interfaces;

namespace Petunio.DiscordCommands;

public class DeleteMemoryCommand(ILogger<DeleteMemoryCommand> logger, IConfiguration configuration, IMemoryService memoryService) : ModuleBase<SocketCommandContext>
{
    [Command("delete-memory")]
    [Summary("Removes a memory from Petunio")]
    public async Task ExecuteAsync([Remainder][Summary("Memory Id")] string memoryId)
    {
        var ownerId = configuration.GetValue<ulong>("Discord:UserId");
        if (Context.User.Id != ownerId) await ReplyAsync("Acceso no permitido");
        memoryService.DeleteMemoryAsync(memoryId);
        logger.LogInformation($"Deleted memory {memoryId}");
        await ReplyAsync("Memoria borrada");
    }
}