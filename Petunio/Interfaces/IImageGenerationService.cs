namespace Petunio.Interfaces;

public interface IImageGenerationService
{
    public Task<string?> GenerateImageAsync(string description);
}