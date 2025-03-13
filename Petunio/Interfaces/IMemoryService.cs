namespace Petunio.Interfaces;

public interface IMemoryService
{
    public Task<List<string>> GetMemoriesAsync(string message);
    public Task<string> SaveMemoryAsync(string memory);
}