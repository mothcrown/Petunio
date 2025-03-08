using System.Xml;
using OllamaSharp;
using Petunio.Interfaces;

namespace Petunio.Services;

public class PetunioService(ILogger<PetunioService> logger, IOllamaApiClient ollamaApiClient) : IPetunioService
{
    public async Task<XmlDocument> Message(string prompt)
    {
        var xmlResponse = new XmlDocument();
        logger.LogInformation("Petunio is generating response...");
        var response = await ollamaApiClient.GenerateAsync(prompt)
            .AggregateAsync("", (current, stream) => current + stream!.Response);
        
        xmlResponse.LoadXml(response);
        return xmlResponse;
    }
}