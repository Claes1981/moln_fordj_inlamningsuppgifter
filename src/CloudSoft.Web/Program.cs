using System.Net.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Azure.Cosmos;
using CloudSoft.Domain;
using CloudSoft.Data;
using CloudSoft.Services;

// Accept emulator's self-signed certificate globally in development
if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
{
#pragma warning disable SYSLIB0014
    AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);
#pragma warning restore SYSLIB0014
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// ----------------------------------------------------------------
// Identity (EF Core InMemory by default; switch to SQLite via config)
// ----------------------------------------------------------------
var identityProvider = builder.Configuration.GetValue<string>("IdentityStore:Provider")?.ToLowerInvariant() ?? "inmemory";

if (identityProvider == "sqlite")
{
    var sqliteConn = builder.Configuration.GetConnectionString("IdentityDb")
        ?? "Data Source=cloudsoft_identity.db";
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(sqliteConn));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseInMemoryDatabase("CloudSoftIdentity"));
}

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.None
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// ----------------------------------------------------------------
// Register CosmosDB
// ----------------------------------------------------------------
var cosmosConnectionString = builder.Configuration.GetConnectionString("CosmosDb")
    ?? throw new InvalidOperationException("CosmosDB connection string is not configured.");
var databaseName = builder.Configuration.GetValue<string>("CosmosDb:DatabaseName") ?? "CloudSoft";
var containerName = builder.Configuration.GetValue<string>("CosmosDb:ContainerName") ?? "JobPostings";

var cosmosClientOptions = new CosmosClientOptions
{
    ApplicationName = "CloudSoft",
    ConnectionMode = ConnectionMode.Gateway
};

// Accept emulator's self-signed certificate in development
if (builder.Environment.IsDevelopment())
{
    cosmosClientOptions.HttpClientFactory = () =>
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        return new HttpClient(handler);
    };
}

var cosmosClient = new CosmosClient(cosmosConnectionString, cosmosClientOptions);
builder.Services.AddSingleton(cosmosClient);

// Create database and container if they don't exist
var databaseResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
var databaseObj = databaseResponse.Database;
await databaseObj.CreateContainerIfNotExistsAsync(containerName, "/PartitionKey");

builder.Services.AddSingleton<IRepository<JobPosting>>(sp =>
{
    var client = sp.GetRequiredService<CosmosClient>();
    var database = client.GetDatabase(databaseName);
    var container = database.GetContainer(containerName);
    return new CosmosRepository<JobPosting>(container);
});

// Register services
builder.Services.AddScoped<IJobPostingService, JobPostingService>();

var app = builder.Build();

// Seed admin user
using (var scope = app.Services.CreateScope())
{
    await IdentitySeeder.SeedAsync(scope.ServiceProvider, builder.Configuration,
        scope.ServiceProvider.GetRequiredService<ILogger<Program>>());
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
