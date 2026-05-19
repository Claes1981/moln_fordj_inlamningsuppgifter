using CloudSoft.Data;
using CloudSoft.Services;
using CloudSoft.Web.Extensions;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddCloudSoftIdentity(builder.Configuration, builder.Environment);
builder.Services.AddCosmosDb(builder.Configuration, builder.Environment);
builder.Services.AddScoped<IJobPostingService, JobPostingService>();

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
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
