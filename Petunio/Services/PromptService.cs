using System.Globalization;
using System.Xml;
using Petunio.Interfaces;

namespace Petunio.Services;

public class PromptService : IPromptService
{
    private ILogger<PromptService> _logger;
    private IDateTime _dateTime;
    private IConfiguration _configuration;
    private IPetunioService _petunioService;
    private IDiscordService _discordService;
    
    private XmlDocument _previousResponse = new XmlDocument();
    private readonly CultureInfo _cultureInfo;
    private readonly string _ownerName;
    private readonly int _thinkingTurnsLimit;
    private int _currentThinkingTurns;
    private bool _ownerSentMessage;
    
    public PromptService(ILogger<PromptService> logger, IDateTime dateTime, IConfiguration configuration,
        IPetunioService petunioService, IDiscordService discordService)
    {
        _logger = logger;
        _dateTime = dateTime;
        _configuration = configuration;
        _petunioService = petunioService;
        _discordService = discordService;
        
        _ownerName = _configuration.GetValue<string>("OwnerName")!;
        _cultureInfo = new CultureInfo("es-ES");
        _thinkingTurnsLimit = _configuration.GetValue<int>("ThinkingTurnsLimit");
        _currentThinkingTurns = 0;
        _ownerSentMessage = false;
    }

    public async Task ProcessDiscordInputAsync(string input)
    {
        _logger.LogInformation("Discord message received");
        
        _ownerSentMessage = true;
        
        var prompt = GetDateString();
        
        prompt += $"{_ownerName} te ha mandado un mensaje!" + Environment.NewLine;
        prompt += $"<message>{input}</message>" + Environment.NewLine; 
        
        prompt += LoadActionsString();

        var response = await _petunioService.Message(prompt);
    }

    public Task ProcessQuartzInputAsync(string input)
    {
        throw new NotImplementedException();
    }

    public Task ProcessOutputAsync(string output)
    {
        _ownerSentMessage = false;
        throw new NotImplementedException();
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

        var thinkMessage = "Puedes pensar sobre algo que quieras o se te haya pedido. Si piensas NO puedes realizar otras acciones en este turno, pero en los siguientes turnos puedes ver qué has pensado previamente.";
        if (HasReachedThinkingTurnsLimit())
        {
            thinkMessage = "Has llegado al límite de usos de esta acción. No puedes usarla en este turno.";
        }
        
        actions.Add("think", thinkMessage);

        return actions;
    }

    private bool HasReachedThinkingTurnsLimit()
    {
        return IsQuietTime() || _currentThinkingTurns <= _thinkingTurnsLimit;
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