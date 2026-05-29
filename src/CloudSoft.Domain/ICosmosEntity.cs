namespace CloudSoft.Domain;

/// <summary>
/// Minimal contract for entities stored in CosmosDB.
/// </summary>
public interface ICosmosEntity
{
    /// <summary>Unique identifier for the entity.</summary>
    string Id { get; set; }

    /// <summary>Value used for the CosmosDB partition key.</summary>
    string PartitionKey { get; set; }
}

