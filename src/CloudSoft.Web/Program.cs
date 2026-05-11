using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Azure.Cosmos;
using CloudSoft.Domain;
using CloudSoft.Data;
using CloudSoft.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register CosmosDB
var cosmosConnectionString = builder.Configuration.GetConnectionString("CosmosDb")
    ?? throw new InvalidOperationException("CosmosDB connection string is not configured.");
var databaseName = builder.Configuration.GetValue<string>("CosmosDb:DatabaseName") ?? "CloudSoft";
var containerName = builder.Configuration.GetValue<string>("CosmosDb:ContainerName") ?? "JobPostings";

builder.Services.AddSingleton(new CosmosClient(cosmosConnectionString));
builder.Services.AddSingleton<IRepository<JobPosting>>(sp =>
{
    var client = sp.GetRequiredService<CosmosClient>();
    var database = client.GetDatabase(databaseName);
    var container = database.GetContainer(containerName);
    return new CosmosRepository<JobPosting>(container);
});

// Register services
builder.Services.AddScoped<IJobPostingService, JobPostingService>();

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

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
