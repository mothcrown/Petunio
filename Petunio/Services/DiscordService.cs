using Discord;
using Discord.WebSocket;
using Petunio.Interfaces;

namespace Petunio.Services;

public class DiscordService : IDiscordService
{
    private readonly ILogger<DiscordService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IPromptService _promptService;
    private readonly DiscordSocketClient _client;
    private readonly ulong _discordUserId;
    
    public DiscordService(ILogger<DiscordService> logger, IConfiguration configuration, IPromptService promptService)
    {
        _logger = logger;
        _configuration = configuration;
        _promptService = promptService;
        _discordUserId = _configuration.GetValue<ulong>("Discord:UserId");

        DiscordSocketConfig config = new();
        _client = new DiscordSocketClient(config);
    }

    public async Task StartAsync()
    {
        var discordToken = _configuration.GetValue<string>("Discord:Token") 
                           ?? throw new Exception("Missing Discord token");

        await _client.LoginAsync(TokenType.Bot, discordToken);
        await _client.StartAsync();

        _client.MessageReceived += MessageReceivedAsync;
    }

    public async Task StopAsync()
    {
        await _client.LogoutAsync();
        await _client.StopAsync();
    }

    public async Task MessageReceivedAsync(SocketMessage message)
    {
        _logger.LogInformation("Discord message received");
        if (!IsValidMessage(message)) return;

        using (message.Channel.EnterTypingState());
        var responses = await _promptService.ProcessDiscordInputAsync(message.Content);

        foreach (var response in responses)
        {
            if (!string.IsNullOrEmpty(response))
            {
                await message.Channel.SendMessageAsync(response);
            }
        }
    }

    private bool IsValidMessage(SocketMessage message)
    {
        // Ignore message from bots
        if (message.Author.IsBot || message is not SocketUserMessage) return false;

        // Petunio only listengs to DMs
        if (message.Channel is not IDMChannel) return false;
        
        // Only the owner is allowed to DM Petunio
        if (message.Author.Id != _discordUserId) return false;

        return true;
    }
}