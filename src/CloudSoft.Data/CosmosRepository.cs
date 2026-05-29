using Microsoft.Azure.Cosmos;
using CloudSoft.Domain;

namespace CloudSoft.Data;

public class CosmosRepository<T> : IRepository<T> where T : class, ICosmosEntity
{
    private readonly Container _container;

    public CosmosRepository(Container container)
    {
        _container = container;
    }

    public async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Cross-partition query since we don't know the partition key value.
            var query = new QueryDefinition("SELECT * FROM c WHERE c.Id = @id")
                .WithParameter("@id", id);
            using var iterator = _container.GetItemQueryIterator<T>(query);
            var results = new List<T>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                results.AddRange(response);
            }

            return results.FirstOrDefault();
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c");
        using var iterator = _container.GetItemQueryIterator<T>(query);
        var results = new List<T>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }

        return results;
    }

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        var response = await _container.CreateItemAsync(entity, new PartitionKey(entity.PartitionKey), cancellationToken: cancellationToken);
        return response.Resource;
    }

    public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _container.UpsertItemAsync(entity, new PartitionKey(entity.PartitionKey), cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            await _container.DeleteItemAsync<T>(id, new PartitionKey(entity.PartitionKey), cancellationToken: cancellationToken);
        }
    }
}
