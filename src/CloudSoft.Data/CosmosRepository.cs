using Microsoft.Azure.Cosmos;
using CloudSoft.Domain;

namespace CloudSoft.Data;

public class CosmosRepository<T> : IRepository<T> where T : class
{
    private readonly Container _container;
    private readonly string _partitionKeyPath;

    public CosmosRepository(Container container, string partitionKeyPath = "/PartitionKey")
    {
        _container = container;
        _partitionKeyPath = partitionKeyPath;
    }

    public async Task<T?> GetByIdAsync(string id)
    {
        try
        {
            var response = await _container.ReadItemAsync<T>(id, new PartitionKey(_partitionKeyPath));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        var query = new QueryDefinition("SELECT * FROM c");
        using var iterator = _container.GetItemQueryIterator<T>(query);
        var results = new List<T>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    public async Task<T> AddAsync(T entity)
    {
        var response = await _container.CreateItemAsync(entity, new PartitionKey(_partitionKeyPath));
        return response.Resource;
    }

    public async Task UpdateAsync(T entity)
    {
        await _container.UpsertItemAsync(entity, new PartitionKey(_partitionKeyPath));
    }

    public async Task DeleteAsync(string id)
    {
        await _container.DeleteItemAsync<T>(id, new PartitionKey(_partitionKeyPath));
    }
}
