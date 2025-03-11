using System.Xml;
using OllamaSharp;
using Petunio.Interfaces;

namespace Petunio.Services;

public class OllamaService(ILogger<OllamaService> logger, IConfiguration configuration) : IOllamaService
{
    public async Task<XmlDocument> Message(string prompt)
    {
        // TODO: load this from DependencyInjection class!
        var ollama = new OllamaApiClient(new Uri(configuration.GetValue<string>("Ollama:Url")!));
        ollama.SelectedModel = configuration.GetValue<string>("Ollama:Model")!;
        
        var xmlResponse = new XmlDocument();
        logger.LogInformation("Petunio is generating a response...");
        var response = "";
        await foreach (var stream in ollama.GenerateAsync(prompt))
        {
            response += stream!.Response;
        }
        
        logger.LogInformation(response);
        
        xmlResponse.LoadXml(response);
        return xmlResponse;
    }
}