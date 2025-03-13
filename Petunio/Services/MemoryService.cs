using Petunio.Interfaces;

namespace Petunio.Services;

public class MemoryService(ILogger<MemoryService> logger, IChromaDbService chromaDbService) : IMemoryService
{
    public async Task<List<string>> GetMemoriesAsync(string message)
    {
        return await chromaDbService.QueryCollectionAsync(message);
    }

    public async Task<string> SaveMemoryAsync(string memory)
    {
        return await chromaDbService.AddToCollectionAsync(memory);
    }
}