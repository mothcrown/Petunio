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
    
    private List<XmlNode> _thinkingNodes = [];
    private readonly CultureInfo _cultureInfo;
    private readonly string _ownerName;
    private readonly int _thinkingTurnsLimit;
    private bool _ownerSentMessage;
    private string? _lastOwnerMessage;
    
    public PromptService(ILogger<PromptService> logger, IDateTime dateTime, IConfiguration configuration,
        IOllamaService ollamaService)
    {
        _logger = logger;
        _dateTime = dateTime;
        _configuration = configuration;
        _ollamaService = ollamaService;
        
        _ownerName = _configuration.GetValue<string>("OwnerName")!;
        _cultureInfo = new CultureInfo("es-ES");
        _thinkingTurnsLimit = _configuration.GetValue<int>("ThinkingTurnsLimit");
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
        prompt += GetChainOfThought() + Environment.NewLine;
        prompt += GetDiscordMessage() + Environment.NewLine;
        prompt += LoadActionsString();
        return prompt;
    }

    private string GetChainOfThought()
    {
        var chain = new StringBuilder();
        if (_thinkingNodes.Count == 0) return chain.ToString();
        
        chain.Append("Has estado pensando:");
        chain.Append(Environment.NewLine);
        foreach (var thinkingNode in _thinkingNodes)
        {
            chain.Append(thinkingNode.OuterXml);
            chain.Append(Environment.NewLine);
        }
        
        return chain.ToString();
    }

    private string GetDiscordMessage()
    {
        var prompt = $"{_ownerName} te ha mandado un mensaje!";
        if (_thinkingNodes.Count > 0) prompt += "Todavía no le has respondido!";
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
        XmlDocument response;
        try
        {
            response = await _ollamaService.Message(prompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw;
        }
        
        string? reply = null;
        
        // First we resolve messages
        if (response.GetElementsByTagName("message").Count > 0)
        {
            reply = response.GetElementsByTagName("message")[0]!.InnerText;
            _ownerSentMessage = false;
            _thinkingNodes = [];
        }
        
        // Next we resolve thinking nodes
        if (string.IsNullOrEmpty(reply))
        {
            reply = await ThinkingAction(response);
        }
        
        return reply;
    }

    private async Task<string?> ThinkingAction(XmlDocument response)
    {
        string? reply = null;
        var thinkingNodes = response.GetElementsByTagName("think");
        if (thinkingNodes.Count > 0 && !HasReachedThinkingTurnsLimit())
        {
            _thinkingNodes.Add(thinkingNodes[0]!);
            reply = await ProcessAsync();
        }

        return reply;
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
        
        if (!HasReachedThinkingTurnsLimit())
        {
            var thinkMessage = "Puedes pensar sobre algo que quieras o se te haya pedido. Si piensas NO puedes realizar otras acciones en este turno, pero en los siguientes turnos puedes ver qué has pensado previamente.";
            actions.Add("think", thinkMessage);
        }
        
        actions.Add("noAction", "Elige no realizar ninguna acción, ej. </noAction>");

        return actions;
    }

    private bool HasReachedThinkingTurnsLimit()
    {
        return !IsQuietTime() && _thinkingNodes.Count > _thinkingTurnsLimit;
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