using System.Globalization;
using System.Text;
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

    public async Task<List<string?>> ProcessDiscordInputAsync(string input)
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
            prompt += GetDiscordMessage() + Environment.NewLine;
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
        if (memoryList.Any())
        {
            memoryString += "Tienes memorias relacionadas:" + Environment.NewLine;
            foreach (var memory in memoryList)
            {
                memoryString += $"* {memory}" + Environment.NewLine;
            }
        }

        return memoryString;
    }

    private string GetMemoryQueryString()
    {
        return $"{GetDateString()} Mensaje de {_ownerName}: {_lastOwnerMessage}";
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

    private async Task<List<string?>> ProcessAsync()
    {
        var prompt = await BuildPrompt();
        _logger.LogDebug(prompt);

        XmlDocument response = await GetPetunioResponse(prompt);
        
        var replies = new List<string?>();
        
        // First we resolve messages
        var messageNodes = response.GetElementsByTagName("message");
        if (messageNodes.Count > 0)
        {
            foreach (XmlNode messageNode in messageNodes)
            {
                replies.Add(messageNode.InnerText);
            }
            
            _ownerSentMessage = false;
        }
        
        var memoryNodes = response.GetElementsByTagName("memory");
        if (memoryNodes.Count > 0)
        {
            foreach (XmlNode memoryNode in memoryNodes)
            {
                await _memoryService.SaveMemoryAsync(memoryNode.InnerText);    
            }
        }
        
        return replies;
    }

    private async Task<XmlDocument> GetPetunioResponse(string prompt)
    {
        XmlDocument response = new XmlDocument();
        try
        {
            var strResponse = await _ollamaService.Message(prompt);
            response = LoadXmlDocument(response, $"<response>{strResponse}</response>");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw;
        }

        return response;
    }

    private XmlDocument LoadXmlDocument(XmlDocument response, string strResponse)
    {
        using StringWriter sw = new StringWriter();
        using XmlTextWriter writer = new XmlTextWriter(sw);
        writer.Formatting = Formatting.Indented;

        response.LoadXml(strResponse);
        response.WriteTo(writer);

        return response;
    }

    private string LoadActionsString()
    {
        var actionsString = "Tus acciones disponibles:" + Environment.NewLine;
        
        var actions = LoadActions();
        foreach (var action in actions)
        {
            actionsString += $"<{action.Key}> - {action.Value}" + Environment.NewLine;
        }
        
        return actionsString;
    }

    private Dictionary<string, string> LoadActions()
    {
        Dictionary<string, string> actions = new Dictionary<string, string>();
        
        // <message>
        if (IsOwnerAvailable())
        {
            actions.Add("message", $"Mandar un mensaje a {_ownerName} o responder a un mensaje suyo. {_ownerName} no va a leer nada que no esté dentro de <message>. Ej. <message>Hola!</message>");
        }
        
        // <think>
        actions.Add("think", $"Puedes pensar sobre algo que quieras o se te haya pedido. {_ownerName} no va a leer esto. Ej. <think>Uhmmm...</think>");
        
        // <memory>
        actions.Add("memory", $"Puedes guardar algo en tu memoria para recordarlo después, acuérdale de decirle a {_ownerName} que has guardado la memoria! Importante: tu memoria no debe pasar de 150 caracteres. Ej. <memory>A Marcos le gustan los juegos de rol</memory>");

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
        return $"{_dateTime.Now.ToString("dd MMMM yyyy HH:mm:ss", _cultureInfo)}.";
    }
}