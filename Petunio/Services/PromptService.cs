using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Petunio.Interfaces;
using Petunio.Models;

namespace Petunio.Services;

public class PromptService : IPromptService
{
    private readonly ILogger<PromptService> _logger;
    private readonly IDateTime _dateTime;
    private readonly IConfiguration _configuration;
    private readonly IOllamaService _ollamaService;
    private readonly IMemoryService _memoryService;
    private readonly IImageGenerationService _imageGenerationService;
    
    private readonly CultureInfo _cultureInfo;
    private readonly string _ownerName;
    private bool _ownerSentMessage;
    private string? _lastOwnerMessage;
    
    public PromptService(ILogger<PromptService> logger, IDateTime dateTime, IConfiguration configuration,
        IOllamaService ollamaService, IMemoryService memoryService, IImageGenerationService imageGenerationService)
    {
        _logger = logger;
        _dateTime = dateTime;
        _configuration = configuration;
        _ollamaService = ollamaService;
        _memoryService = memoryService;
        _imageGenerationService = imageGenerationService;
        
        _ownerName = _configuration.GetValue<string>("OwnerName")!;
        _cultureInfo = new CultureInfo("es-ES");
        _ownerSentMessage = false;
    }

    public async Task<PetunioResponse?> ProcessDiscordInputAsync(string input)
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

    private async Task<PetunioResponse?> ProcessAsync()
    {
        var prompt = await BuildPrompt();
        _logger.LogDebug(prompt);

        var response = await GetPetunioResponse(prompt);
        await ProcessMemoryTags(response);
        List<string> imagePaths = await ProcessImageTags(response);
        
        var petunioResponse = new PetunioResponse();
        petunioResponse.Response = FormatResponse(response);
        petunioResponse.Images = imagePaths;
        
        _ownerSentMessage = false;
        return petunioResponse;
    }

    private async Task<List<string>> ProcessImageTags(string response)
    {
        List<string> imagePaths = new List<string>();
        
        var imageDescriptions = ProcessTags(response, "image");
        foreach (var description in imageDescriptions)
        {
            var imagePath = await _imageGenerationService.GenerateImageAsync(description);
            if (imagePath != null) imagePaths.Add(imagePath);
        }
        
        return imagePaths;
    }

    private async Task ProcessMemoryTags(string response)
    {
        var memories = ProcessTags(response, "memory");
        foreach (var memory in memories)
        {
            await _memoryService.SaveMemoryAsync(memory);
        }
    }
    
    private List<string> ProcessTags(string response, string tag)
    {
        List<string> values = new List<string>();
        
        Match match = Regex.Match(response, $"<{tag}>(.*?)</{tag}>");
        if (match.Success)
        {
            var groups = match.Groups;
            var groupsLenght = groups.Count;
            // Don't ask me why
            for (int i = 0; i < groupsLenght; i++)
            {
                if (i % 2 != 0)
                {
                    values.Add(groups[i].Value);
                }
            }
        }

        return values;
    }

    private string FormatResponse(string response)
    {
        // Remove <think> tags from response
        // response = Regex.Replace(response, @"<memory>.*?</memory>", "");

        // Remove <think> tags from response
        // response = Regex.Replace(response, @"<think>.*?</think>", "");

        return ToLowerCase(response);
    }
    
    private static string ToLowerCase(string response)
    {
        return Regex.Replace(response, "(```.*?```)|([^`]+)", match =>
        {
            if (match.Groups[1].Success)
            {
                return match.Groups[1].Value;
            }
            
            return match.Groups[2].Value.ToLower();
        }, RegexOptions.Singleline);
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
        actions.Add("Si quieres pensar algo, usa la etiqueta think: por ejemplo <think>Estoy pensando!</think>");
        // <memory>
        actions.Add("Si quieres guardar algo en tu memoria, usa la etiqueta memory: por ejemplo <memory>A Marcos le gustan los juegos de rol</memory>");
        // <image>
        actions.Add("Si quieres generar una imagen, usa la etiqueta image: por ejemplo <image>a samurai white cat</image>");

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