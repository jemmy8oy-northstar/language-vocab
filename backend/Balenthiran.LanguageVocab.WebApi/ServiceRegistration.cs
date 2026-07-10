using Balenthiran.LanguageVocab.Abstractions.Seeding;
using Balenthiran.LanguageVocab.Abstractions.Services;
using Balenthiran.LanguageVocab.Services;
using Balenthiran.LanguageVocab.Services.Grading;
using Balenthiran.LanguageVocab.Services.Pooling;
using Balenthiran.LanguageVocab.Services.Seeding;
using Balenthiran.LanguageVocab.Database;
using Microsoft.EntityFrameworkCore;

namespace Balenthiran.LanguageVocab.WebApi;

public static class ServiceRegistration
{
    public static void AddBackendServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine("[WARNING] No database connection string configured — database features are disabled.");
        }
        else
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString, b => b.MigrationsAssembly("Balenthiran.LanguageVocab.Database")));
        }

        services.AddAutoMapper(cfg => cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies()));
        services.AddScoped<IStatusService, StatusService>();

        // Adaptive-pool + seeding chain (design assumptions A3/A4). PoolMath is the pure
        // rule set, injected into PoolService; SeedLoader idempotently loads word-lists.
        services.AddSingleton<IPinyinGrader, PinyinGrader>();
        services.AddSingleton<IPoolMath, PoolMath>();
        services.AddScoped<IPoolService, PoolService>();
        services.AddScoped<ISeedLoader, SeedLoader>();
    }
}
