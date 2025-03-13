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

    public async Task<string?> ProcessDiscordInputAsync(string input)
    {
        _ownerSentMessage = true;
        _lastOwnerMessage = input;
        
        return await ProcessAsync();
    }

    private string BuildPrompt()
    {
        var prompt = GetDateString() + Environment.NewLine;
        prompt += GetDiscordMessage() + Environment.NewLine;
        prompt += LoadActionsString();
        return prompt;
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
        var prompt = BuildPrompt();
        _logger.LogDebug(prompt);

        XmlDocument response = await GetPetunioResponse(prompt);
        
        string? reply = null;
        
        // First we resolve messages
        if (response.GetElementsByTagName("message").Count > 0)
        {
            // We only get the first one and ignore the rest, sorry Petunio
            reply = response.GetElementsByTagName("message")[0]!.InnerText;
            _ownerSentMessage = false;
        }
        
        var memoryNodes = response.GetElementsByTagName("memory");
        if (memoryNodes.Count > 0)
        {
            foreach (XmlNode memoryNode in memoryNodes)
            {
                await _memoryService.SaveMemoryAsync(memoryNode.InnerText);    
            }
            
            _ownerSentMessage = false;
        }
        
        return reply;
    }

    private async Task<XmlDocument> GetPetunioResponse(string prompt)
    {
        XmlDocument response = new XmlDocument();
        try
        {
            var strResponse = await _ollamaService.Message(prompt);
            response = LoadXmlDocument(response, strResponse);
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
        
        if (IsOwnerAvailable())
        {
            actions.Add("message", $"Mandar un mensaje a {_ownerName} o responder a un mensaje suyo.");
        }
        
        actions.Add("think", $"Puedes pensar sobre algo que quieras o se te haya pedido. {_ownerName} no va a leer esto.");
        
        actions.Add("memory", "Puedes guardar algo en tu memoria para recordarlo después. Importante: tu memoria no debe pasar de 150 caracteres.");

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
        return $"Nuevo turno: {_dateTime.Now.ToString("dd MMMM yyyy HH:mm:ss", _cultureInfo)}.";
    }
}