using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Petunio.Interfaces;

namespace Petunio.Services;

public class PromptService : IPromptService
{
    private readonly ILogger<PromptService> _logger;
    private readonly IDateTime _dateTime;
    private readonly IConfiguration _configuration;
    private readonly IOllamaService _ollamaService;
    private readonly IMemoryService _memoryService;
    
    private readonly CultureInfo _cultureInfo;
    private readonly string _ownerName;
    private bool _ownerSentMessage;
    private string? _lastOwnerMessage;
    
    public PromptService(ILogger<PromptService> logger, IDateTime dateTime, IConfiguration configuration,
        IOllamaService ollamaService, IMemoryService memoryService)
    {
        _logger = logger;
        _dateTime = dateTime;
        _configuration = configuration;
        _ollamaService = ollamaService;
        _memoryService = memoryService;
        
        _ownerName = _configuration.GetValue<string>("OwnerName")!;
        _cultureInfo = new CultureInfo("es-ES");
        _ownerSentMessage = false;
    }

    public async Task<string?> ProcessDiscordInputAsync(string input)
    {
        _ownerSentMessage = true;
        _lastOwnerMessage = input;
        
        return await ProcessAsync();
    }

    private async Task<string> BuildPrompt()
    {
        var prompt = GetDateString() + Environment.NewLine;
        if (_ownerSentMessage)
        {
            prompt += GetDiscordMessage();
        }

        prompt += await GetMemoryString();
        prompt += LoadActionsString();
        return prompt;
    }

    private async Task<string> GetMemoryString()
    {
        string memoryString = "";
        string query = GetMemoryQueryString();
        var memoryList = await _memoryService.GetMemoriesAsync(query);
        if (memoryList.Count > 0)
        {
            memoryString += "Tienes memorias relacionadas:" + Environment.NewLine;
            foreach (var memory in memoryList)
            {
                memoryString += $"* {memory}" + Environment.NewLine;
            }
            
            memoryString += Environment.NewLine;
        }

        return memoryString + Environment.NewLine;
    }

    private string GetMemoryQueryString()
    {
        var memoryString = new StringBuilder();
        memoryString.Append(GetDateString());
        if (_ownerSentMessage)
        {
            memoryString.Append($" Mensaje de {_ownerName}: {_lastOwnerMessage}");
        }
        
        return memoryString.ToString();
    }

    private string GetDiscordMessage()
    {
        var prompt = $"{_ownerName} te ha mandado un mensaje!";
        prompt += Environment.NewLine;
        prompt += $"<message>{_lastOwnerMessage}</message>" + Environment.NewLine;
        return prompt;
    }

    public Task ProcessQuartzInputAsync(string input)
    {
        throw new NotImplementedException();
    }

    private async Task<string?> ProcessAsync()
    {
        var prompt = await BuildPrompt();
        _logger.LogDebug(prompt);

        var response = await GetPetunioResponse(prompt);
        
        // Get memories
        Match match = Regex.Match(response, @"<memory>(.*?)</memory>");
        if (match.Success)
        {
            var groups = match.Groups;
            var groupsLenght = groups.Count;
            // Don't ask me why
            for (int i = 0; i < groupsLenght; i++)
            {
                if (i % 2 != 0)
                {
                    await _memoryService.SaveMemoryAsync(groups[i].Value);
                }
            }
        }

        response = FormatResponse(response);
        
        _ownerSentMessage = false;
        return response;
    }

    private string FormatResponse(string response)
    {
        // Remove <think> tags from response
        // response = Regex.Replace(response, @"<memory>.*?</memory>", "");

        // Remove <think> tags from response
        // response = Regex.Replace(response, @"<think>.*?</think>", "");
        
        // .Replace ' with \'

        var result = new StringBuilder();
        var InCodeBlock = false;
        

        return response;
    }

    private async Task<string> GetPetunioResponse(string prompt)
    {
        string response;
        
        try
        {
            response = await _ollamaService.Message(prompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw;
        }

        return response;
    }

    private string LoadActionsString()
    {
        var actionsString = "Herramientas de asistente:" + Environment.NewLine;
        
        var actions = LoadActions();
        foreach (var action in actions)
        {
            actionsString += action + Environment.NewLine;
        }
        
        return actionsString;
    }

    private List<string> LoadActions()
    {
        List<string> actions = [];
        // <think>
        actions.Add("Si necesitas pensar algo, usa la etiqueta think: por ejemplo <think>Estoy pensando!</think>");
        // <memory>
        actions.Add("Si necesitas guardar algo en tu memoria, usa la etiqueta memory: por ejemplo <memory>A Marcos le gustan los juegos de rol</memory>");

        return actions;
    }

    private bool IsOwnerAvailable()
    {
        return IsQuietTime() || _ownerSentMessage;
    }

    private bool IsQuietTime()
    {
        var startTime = TimeSpan.Parse(_configuration.GetValue<string>("QuietTime:StartHour24h")!);
        var endTime = TimeSpan.Parse(_configuration.GetValue<string>("QuietTime:EndHour24h")!);
        if (startTime == endTime) return false;

        var now = _dateTime.Now.TimeOfDay;
        
        // QuietTime: 13:00 - 16:00
        if (startTime < endTime)
        {
            return startTime <= now && now <= endTime;
        }
        
        // QuietTime: 22:00 - 06:00
        return !(endTime < now && now < startTime);

    }

    private string GetDateString()
    {
        return $"Es {_dateTime.Now.ToString("dd MMMM yyyy HH:mm:ss", _cultureInfo)}.";
    }
}