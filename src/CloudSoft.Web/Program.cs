using CloudSoft.Data;
using CloudSoft.Domain;
using CloudSoft.Services;
using CloudSoft.Web.Extensions;
using CloudSoft.Web.HealthChecks;
using CloudSoft.Web.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// MVC + API controllers
builder.Services.AddControllersWithViews();

// Identity
builder.Services.AddCloudSoftIdentity(builder.Configuration, builder.Environment);

// CosmosDB
builder.Services.AddCosmosDb(builder.Configuration, builder.Environment);

// Blob Storage — register AzureBlobService only when configured (prod) or connection string available (dev)
var hasBlobConfig = !string.IsNullOrEmpty(builder.Configuration.GetConnectionString("BlobStorage"))
    || !string.IsNullOrEmpty(builder.Configuration.GetValue<string>("BlobStorage__AccountUrl"));

if (hasBlobConfig)
{
    builder.Services.AddSingleton<IBlobService, AzureBlobService>();
}
else
{
    // No-op blob service for local dev without blob storage configured
    builder.Services.AddSingleton<IBlobService, NoOpBlobService>();
}

// Business logic
builder.Services.AddScoped<IJobPostingService, JobPostingService>();

// Health checks — deep probes for CosmosDB and Blob Storage
builder.Services.AddHealthChecks()
    .AddCheck<CosmosHealthCheck>("cosmosdb", tags: new[] { "cosmos" })
    .AddCheck<BlobHealthCheck>("blobstorage", tags: new[] { "blob" });

// Swagger / OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CloudSoft Recruitment API",
        Version = "v1",
        Description = "REST API for the CloudSoft Recruitment Portal",
    });

    // Add API key security definition
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Name = "X-API-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Description = "API key header value",
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey",
                },
            },
            Array.Empty<string>()
        },
    });
});

var app = builder.Build();

// Ensure CosmosDB database and container exist
await app.EnsureCosmosDbAsync();

// Seed roles and admin user
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    await IdentitySeeder.SeedAsync(
        sp.GetRequiredService<UserManager<ApplicationUser>>(),
        sp.GetRequiredService<RoleManager<IdentityRole>>(),
        builder.Configuration,
        sp.GetRequiredService<ILogger<Program>>());
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Correlation ID middleware — must run early to tag all downstream logs
app.UseMiddleware<CorrelationIdMiddleware>();

// API Key middleware — validates /api/* routes before they reach controllers
app.UseMiddleware<ApiKeyMiddleware>();

// Swagger UI (always enabled for local dev; production uses API key auth)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CloudSoft API v1");
    c.RoutePrefix = "swagger";
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
