using System.Diagnostics;
using System.Dynamic;
using System.Text;
using System.Text.Json;
using Discord;
using Json.More;
using Newtonsoft.Json;
using Petunio.Interfaces;
using Petunio.Models;
using Serilog.Settings.Configuration;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Petunio.Services;

public class ImageGenerationService : IImageGenerationService
{
    private const string PROMPT_JSON_FILEPATH = "petunio_prompt.json";
    private const string EMPTY_QUEUE = "{\"queue_running\": [], \"queue_pending\": []}";
    private const string GENERATED_IMAGES_DIR = "GeneratedImages";
    
    private ILogger<ImageGenerationService> _logger;
    private IConfiguration _configuration;
    private IDateTime _dateTime;
    private string _comfyUIUrl;

    public ImageGenerationService(ILogger<ImageGenerationService> logger, IConfiguration configuration, IDateTime dateTime)
    {
        _logger = logger;
        _configuration = configuration;
        _dateTime = dateTime;
        _comfyUIUrl = _configuration.GetValue<string>("ComfyUI:Url");
    }

    public async Task<string?> GenerateImageAsync(string description)
    {
        _logger.LogInformation($"Generating image: {description}");
        description = AddLoraTags(description);
        var json = await GetImageGenerationJsonString(description);
        return await ProcessImageAsync(json);
    }

    private string AddLoraTags(string description)
    {
        return description + ", playstation 1 graphics, PS1 Game";
    }

    private async Task<string> ProcessImageAsync(string json)
    {
        PromptResponse? promptResponse = await PostImagePrompt(json);
        if (promptResponse is null) throw new Exception("Connection problems");
        await WaitForImageToProcess(promptResponse);
        var imageUrl = await GetImageUrl(promptResponse);
        return await SaveImage(imageUrl);
    }

    private async Task<string> SaveImage(string imageUrl)
    {
        string filePath = "";
        
        try
        {
            using HttpClient client = new HttpClient();
            byte[] imageBytes = await client.GetByteArrayAsync(imageUrl);
        
            var generatedImagesPath = Path.Combine(Directory.GetCurrentDirectory(), GENERATED_IMAGES_DIR);
            if (!Directory.Exists(generatedImagesPath))
            {
                Directory.CreateDirectory(generatedImagesPath);
            }
        
            var fileName = Guid.NewGuid() + "_" + _dateTime.Now.ToString("yyyyMMddHHmmss") + ".png";
            filePath = Path.Combine(generatedImagesPath, fileName);
            await File.WriteAllBytesAsync(filePath, imageBytes);
            _logger.LogInformation($"Image saved to: {filePath}");
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw new Exception(e.Message);
        }
        

        return filePath;
    }

    private async Task<string> GetImageUrl(PromptResponse promptResponse)
    {
        string imageUrl = "";
        
        try
        {
            using HttpClient client = new HttpClient();
            var promptInfoResponse = await client.GetAsync($"{_comfyUIUrl}/history/{promptResponse.PromptId}");
            var promptInfoContent = await promptInfoResponse.Content.ReadAsStringAsync();
            var promptInfoContentObject = JsonSerializer.Deserialize<Dictionary<string,PromptHistory>>(promptInfoContent);
            promptInfoContentObject!.TryGetValue(promptResponse.PromptId, out var promptHistory);
            var imageFileName = promptHistory!.Outputs!.Container!.Images!.FirstOrDefault()!.FileName!;
            imageUrl = $"{_comfyUIUrl}/view?filename={imageFileName}";
            _logger.LogInformation($"Image URL: {imageUrl}");
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw new Exception(e.Message);
        }
        
        return imageUrl;
    }

    private async Task WaitForImageToProcess(PromptResponse promptResponse)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start(); 
        
        var response = "";
        using HttpClient client = new HttpClient();
        {
            while (response != EMPTY_QUEUE)
            {
                var queueResponse = await client.GetAsync($"{_comfyUIUrl}/queue");
                response = await queueResponse.Content.ReadAsStringAsync();
            
                // Feo. Pero más feo es jugar con while true
                if (stopwatch.ElapsedMilliseconds >= 60000)
                {
                    _logger.LogInformation("Took too much time to generate the image");
                    break;
                }

                
            }
        }
    }

    private async Task<PromptResponse?> PostImagePrompt(string json)
    {
        PromptResponse? promptResponse = null;
        
        try
        {
            using HttpClient client = new HttpClient();
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(_comfyUIUrl + "/prompt", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            promptResponse = JsonConvert.DeserializeObject<PromptResponse>(responseContent);
            _logger.LogInformation($"Prompt ID: {promptResponse!.PromptId}");
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw new Exception(e.Message);
        }
        
        return promptResponse;
    }

    private async Task<string> GetImageGenerationJsonString(string description)
    {
        if (!File.Exists(PROMPT_JSON_FILEPATH))
        {
            var error = $"File {PROMPT_JSON_FILEPATH} not found.";
            _logger.LogError(error);
            throw new Exception(error);
        }
        
        var jsonString = await File.ReadAllTextAsync(PROMPT_JSON_FILEPATH);
        return jsonString.Replace("[[description]]", description);
    }
}