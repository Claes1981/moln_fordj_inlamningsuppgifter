namespace CloudSoft.Domain;

/// <summary>
/// Application-wide constants to avoid magic strings scattered across the codebase.
/// </summary>
public static class Constants
{
    /// <summary>CosmosDB container partition key path.</summary>
    /// <summary>Partition key path for CosmosDB containers.</summary>
    public const string PartitionKeyPath = "/PartitionKey";

    /// <summary>Default CosmosDB database name.</summary>
    public const string DefaultDatabaseName = "CloudSoft";

    /// <summary>Default CosmosDB container name.</summary>
    public const string DefaultContainerName = "JobPostings";

    /// <summary>Administrator role name.</summary>
    public const string AdministratorRole = "Administrator";

    /// <summary>Candidate role name.</summary>
    public const string CandidateRole = "Candidate";

    /// <summary>All application roles.</summary>
    public static readonly string[] Roles = { AdministratorRole, CandidateRole };
}
