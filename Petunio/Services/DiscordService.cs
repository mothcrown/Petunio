using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Petunio.Interfaces;

namespace Petunio.Services;

public class DiscordService : IDiscordService
{
    private readonly ILogger<DiscordService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IPromptService _promptService;
    private readonly DiscordSocketClient _client;
    private readonly CommandService _commands;
    private IServiceProvider? _serviceProvider;
    private readonly ulong _discordUserId;
    
    public DiscordService(ILogger<DiscordService> logger, IConfiguration configuration, IPromptService promptService, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _configuration = configuration;
        _promptService = promptService;
        _discordUserId = _configuration.GetValue<ulong>("Discord:UserId");
        _serviceProvider = serviceProvider;

        DiscordSocketConfig config = new();
        _client = new DiscordSocketClient(config);
        _commands = new CommandService();
    }

    public async Task StartAsync()
    {
        var discordToken = _configuration.GetValue<string>("Discord:Token") 
                           ?? throw new Exception("Missing Discord token");
        
        await _commands.AddModulesAsync(Assembly.GetExecutingAssembly(), _serviceProvider);

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
        if (!IsValidMessage(message)) return;
        _logger.LogInformation($"Discord message from {message.Author.Username}#{message.Author.Id} received");
        
        var userCommand = (SocketUserMessage)message;
        int position = 0;
        bool messageIsCommand = userCommand.HasCharPrefix('!', ref position);

        using (message.Channel.EnterTypingState())
        {
            if (messageIsCommand)
            {
                await _commands.ExecuteAsync(
                    new SocketCommandContext(_client, userCommand),
                    position,
                    _serviceProvider);
            }
            else
            {
                var response = await _promptService.ProcessDiscordInputAsync(message.Content);
                if (response is not null)
                {
                    if (!string.IsNullOrEmpty(response.Response))
                    {
                        await message.Channel.SendMessageAsync(response.Response);
                    }
                    
                    if (response.Images!.Count > 0)
                    {
                        foreach (var image in response.Images!)
                        {
                            await message.Channel.SendFileAsync(image);
                        }
                    }
                }
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