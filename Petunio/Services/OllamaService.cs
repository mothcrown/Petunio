using System.Text.RegularExpressions;
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
        
        response = SanitizeResponse(response);
        logger.LogDebug(response);
        
        xmlResponse.LoadXml(response);
        return xmlResponse;
    }

    private string SanitizeResponse(string response)
    {
        var reply = response;
        
        // ```xml ... ```
        if (response.StartsWith("```"))
        {
            reply = response.Remove(response.Length - 3).Remove(0, 6);
        }

        // TODO Add a way to replace <!-- xml comments -->
        reply = SanitizeNodeInnerText(reply, "message");
        reply = SanitizeNodeInnerText(reply, "think");
        
        return reply;
    }

    private string SanitizeNodeInnerText(string reply, string nodeName)
    {
        return Regex.Replace(reply, $@"<{nodeName}>(.*?)<\/think>", match =>
        {
            string innerText = match.Groups[1].Value;
            string sanitizedText = innerText.Replace("<", "&lt;").Replace(">", "&gt;");
            return $"<{nodeName}>{sanitizedText}</{nodeName}>";
        }, RegexOptions.Singleline);
    }
}