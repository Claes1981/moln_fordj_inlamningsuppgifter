using Azure.Identity;
using Microsoft.Azure.Cosmos;
using CloudSoft.Data;
using CloudSoft.Domain;

namespace CloudSoft.Web.Extensions;

public static class CosmosExtensions
{
    public static IServiceCollection AddCosmosDb(this IServiceCollection services,
        IConfiguration configuration, IHostEnvironment environment)
    {
        var databaseName = configuration.GetValue<string>("CosmosDb:DatabaseName") ?? Constants.DefaultDatabaseName;
        var containerName = configuration.GetValue<string>("CosmosDb:ContainerName") ?? Constants.DefaultContainerName;

        var options = new CosmosClientOptions
        {
            ApplicationName = "CloudSoft",
            ConnectionMode = ConnectionMode.Gateway,
        };

        CosmosClient client;

        if (environment.IsDevelopment())
        {
            // Development: use connection string (CosmosDB emulator).
            var connectionString = configuration.GetConnectionString("CosmosDb")
                ?? throw new InvalidOperationException("CosmosDB connection string is not configured for development.");

            // Disable SocketsHttpHandler to allow custom HttpClientHandler cert validation.
#pragma warning disable SYSLIB0014
            AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);
#pragma warning restore SYSLIB0014

            options.HttpClientFactory = () =>
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                };
                return new HttpClient(handler);
            };

            client = new CosmosClient(connectionString, options);
        }
        else
        {
            // Production: use Managed Identity (DefaultAzureCredential) with endpoint URI.
            var endpoint = configuration.GetValue<string>("CosmosDb:Endpoint")
                ?? throw new InvalidOperationException("CosmosDb:Endpoint is not configured for production.");

            var credential = new DefaultAzureCredential();
            client = new CosmosClient(endpoint, credential, options);
        }

        services.AddSingleton(client);

        services.AddSingleton<IRepository<JobPosting>>(sp =>
        {
            var cosmos = sp.GetRequiredService<CosmosClient>();
            var database = cosmos.GetDatabase(databaseName);
            var container = database.GetContainer(containerName);
            return new CosmosRepository<JobPosting>(container);
        });

        return services;
    }

    /// <summary>
    /// Ensures the CosmosDB database and container exist. Call during application startup.
    /// </summary>
    public static async Task EnsureCosmosDbAsync(this WebApplication app)
    {
        var client = app.Services.GetRequiredService<CosmosClient>();
        var configuration = app.Services.GetRequiredService<IConfiguration>();

        var databaseName = configuration.GetValue<string>("CosmosDb:DatabaseName") ?? Constants.DefaultDatabaseName;
        var containerName = configuration.GetValue<string>("CosmosDb:ContainerName") ?? Constants.DefaultContainerName;

        var database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
        await database.Database.CreateContainerIfNotExistsAsync(containerName, Constants.PartitionKeyPath);
    }
}
