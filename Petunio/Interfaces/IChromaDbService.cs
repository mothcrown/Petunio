using ChromaDB.Client.Models;

namespace Petunio.Interfaces;

public interface IChromaDbService
{
    public Task InitializeAsync();
    public Task<string> AddToCollectionAsync(string data);
    public Task<List<string>> QueryCollectionAsync(string query);
}