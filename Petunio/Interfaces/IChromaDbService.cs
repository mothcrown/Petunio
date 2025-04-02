using ChromaDB.Client.Models;

namespace Petunio.Interfaces;

public interface IChromaDbService
{
    public Task InitializeAsync();
    public Task<string> AddToCollectionAsync(string collection, string data);
    public Task<List<string>> QueryCollectionAsync(string collection, string query);
    public Task DeleteFromCollectionAsync(string collection, string id);
    public Task<List<string>> GetCollectionAsync(string collection);
}