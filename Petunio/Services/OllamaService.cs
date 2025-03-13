using System.Text.RegularExpressions;
using System.Xml;
using OllamaSharp;
using Petunio.Interfaces;

namespace Petunio.Services;

public class OllamaService(ILogger<OllamaService> logger, IConfiguration configuration) : IOllamaService
{
    public async Task<string> Message(string prompt)
    {
        // TODO: load this from DependencyInjection class!
        var ollama = new OllamaApiClient(new Uri(configuration.GetValue<string>("Ollama:Url")!));
        ollama.SelectedModel = configuration.GetValue<string>("Ollama:Model")!;
        
        logger.LogInformation("Petunio is generating a response...");
        var response = "";
        await foreach (var stream in ollama.GenerateAsync(prompt))
        {
            response += stream!.Response;
        }
        
        logger.LogDebug(response);
        return response;
    }
}