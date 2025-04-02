using Petunio.Interfaces;

namespace Petunio.Services;

public class MemoryService(IChromaDbService chromaDbService) : IMemoryService
{
    private const string MEMORY_COLLECTION_NAME = "memories";
    
    public async Task<List<string>> GetMemoriesAsync(string message)
    {
        return await chromaDbService.QueryCollectionAsync(MEMORY_COLLECTION_NAME, message);
    }

    public async Task<string> SaveMemoryAsync(string memory)
    {
        return await chromaDbService.AddToCollectionAsync(MEMORY_COLLECTION_NAME, memory);
    }
    
    public async Task DeleteMemoryAsync(string memoryId)
    {
        await chromaDbService.DeleteFromCollectionAsync(MEMORY_COLLECTION_NAME, memoryId);
    }

    public async Task<List<string>> GetAllMemoriesAsync()
    {
        return await chromaDbService.GetCollectionAsync(MEMORY_COLLECTION_NAME);
    }
}