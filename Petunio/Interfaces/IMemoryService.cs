namespace Petunio.Interfaces;

public interface IMemoryService
{
    public Task<List<string>> GetMemoriesAsync(string message);
    public Task<string> SaveMemoryAsync(string memory);
    public Task DeleteMemoryAsync(string memoryId);
    public Task<List<string>> GetAllMemoriesAsync();
}