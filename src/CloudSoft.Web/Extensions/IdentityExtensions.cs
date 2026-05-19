using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CloudSoft.Data;

namespace CloudSoft.Web.Extensions;

public static class IdentityExtensions
{
    public static IServiceCollection AddCloudSoftIdentity(this IServiceCollection services,
        IConfiguration configuration, IHostEnvironment environment)
    {
        // Register DbContext (InMemory by default; switch to SQLite via config)
        var provider = configuration.GetValue<string>("IdentityStore:Provider")?.ToLowerInvariant() ?? "inmemory";

        if (provider == "sqlite")
        {
            var conn = configuration.GetConnectionString("IdentityDb")
                ?? "Data Source=cloudsoft_identity.db";
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(conn));
        }
        else
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("CloudSoftIdentity"));
        }

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = environment.IsDevelopment()
                ? CookieSecurePolicy.None
                : CookieSecurePolicy.Always;
                        options.Cookie.SameSite = SameSiteMode.Strict;
        });

        return services;
    }
}
