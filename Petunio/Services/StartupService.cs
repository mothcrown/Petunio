using Petunio.Interfaces;

namespace Petunio.Services;

public class StartupService(ILogger<StartupService> logger, IChromaDbService chromaDbService,
    IDiscordService discordService) : BackgroundService
{
    private const string PROMPT_JSON_FILENAME = "petunio_prompt.json";
    private const string PROMPT_JSON_URL =
        "https://raw.githubusercontent.com/mothcrown/docker-comfyui-petunio/refs/heads/master/petunio_prompt.json";
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitChromaDbAsync();
        await StartDiscordAsync();
        await DownloadImageGenerationPromptJson();
    }

    private async Task InitChromaDbAsync()
    {
        try
        {
            await chromaDbService.InitializeAsync();
            logger.LogInformation("Chroma Db started");
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            throw;
        }
    }

    private async Task StartDiscordAsync()
    {
        try
        {
            await discordService.StartAsync();
            logger.LogInformation("Connected to Discord");
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            throw;
        }
    }
    
    private async Task DownloadImageGenerationPromptJson()
    {
        try
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), PROMPT_JSON_FILENAME);

            if (!File.Exists(filePath))
            {
                using var client = new HttpClient();
                var response = await client.GetAsync(PROMPT_JSON_URL);
                response.EnsureSuccessStatusCode();

                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(filePath, fileBytes);
                logger.LogInformation("Image Generation Prompt JSON was successfully downloaded");
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            throw;
        }
    }
}