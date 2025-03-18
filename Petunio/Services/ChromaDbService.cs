using ChromaDB.Client;
using ChromaDB.Client.Models;
using OllamaSharp;
using Petunio.Interfaces;

namespace Petunio.Services;

public class ChromaDbService : IChromaDbService
{

    private readonly ILogger<ChromaDbService> _logger;

    private ChromaConfigurationOptions _configOptions;
    private OllamaApiClient _ollamaApiClient;
    
    private const string MEMORY_COLLECTION_NAME = "memories";

    public ChromaDbService(ILogger<ChromaDbService> logger, IConfiguration configuration)
    {
        _logger = logger;

        _configOptions = new ChromaConfigurationOptions(configuration.GetValue<string>("Chroma:Url")!);
        _ollamaApiClient = new OllamaApiClient(new Uri(configuration.GetValue<string>("Ollama:Url")!));
        _ollamaApiClient.SelectedModel = configuration.GetValue<string>("Ollama:EmbeddingModel")!;
    }

    public async Task InitializeAsync()
    {
        using var httpClient = new HttpClient();
        var client = new ChromaClient(_configOptions, httpClient);
        await client.GetOrCreateCollection(MEMORY_COLLECTION_NAME);
    }

    public async Task<string> AddToCollectionAsync(string data)
    {
        using var httpClient = new HttpClient();
        var collectionClient = await GetCollectionClient(httpClient);
        var embeddingVector = await GetStringEmbedding(data);

        try
        {
            var id = Guid.NewGuid().ToString();
            _logger.LogInformation($"Adding new collection {id}");
            await collectionClient.Add([id], [embeddingVector],
                new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>() { ["Memory"] = data }
                });
            return id;
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw;
        }
    }

    public async Task<List<string>> QueryCollectionAsync(string query)
    {
        using var httpClient = new HttpClient();
        var collectionClient = await GetCollectionClient(httpClient);
        var embeddingVector = await GetStringEmbedding(query);

        try
        {
            _logger.LogInformation($"New query to ChromaDB");
            var queryResult = await collectionClient.Query(
                queryEmbeddings: [ embeddingVector ],
                nResults: 5,
                include: ChromaQueryInclude.Metadatas | ChromaQueryInclude.Distances);
            
            return GetCollectionQueryEntryResultList(queryResult);
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }

    private List<string> GetCollectionQueryEntryResultList(List<List<ChromaCollectionQueryEntry>> queryResult)
    {
        var list = new List<string>();

        foreach (var result in queryResult)
        {
            foreach (var item in result)
            {
                _logger.LogDebug($"Id: {item.Id}, Memory: {item.Metadata?["Memory"]}, Distance: {item.Distance}");
                list.Add(item.Metadata?["Memory"].ToString()!);
            }
        }

        return list;
    }

    private async Task<ChromaCollectionClient> GetCollectionClient(HttpClient httpClient)
    {
        var client = new ChromaClient(_configOptions, httpClient);
        var collection = await client.GetOrCreateCollection(MEMORY_COLLECTION_NAME);
        return new ChromaCollectionClient(collection, _configOptions, httpClient);
    }
    
    private async Task<float[]> GetStringEmbedding(string data)
    {
        try
        {
            _logger.LogInformation($"Fetching embedding data for new query");
            var embeddingResponse = await _ollamaApiClient.EmbedAsync(data);
            return embeddingResponse.Embeddings[0].ToArray();
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }
}