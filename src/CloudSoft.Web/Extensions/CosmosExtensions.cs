using Microsoft.Azure.Cosmos;
using CloudSoft.Data;
using CloudSoft.Domain;

namespace CloudSoft.Web.Extensions;

public static class CosmosExtensions
{
    public static IServiceCollection AddCosmosDb(this IServiceCollection services,
        IConfiguration configuration, IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("CosmosDb")
            ?? throw new InvalidOperationException("CosmosDB connection string is not configured.");
        var databaseName = configuration.GetValue<string>("CosmosDb:DatabaseName") ?? Constants.DefaultDatabaseName;
        var containerName = configuration.GetValue<string>("CosmosDb:ContainerName") ?? Constants.DefaultContainerName;

        var options = new CosmosClientOptions
        {
            ApplicationName = "CloudSoft",
            ConnectionMode = ConnectionMode.Gateway,
        };

        if (environment.IsDevelopment())
        {
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
        }

        var client = new CosmosClient(connectionString, options);
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
        await database.Database.CreateContainerIfNotExistsAsync(containerName, Constants.PartitionKey);
    }
}
