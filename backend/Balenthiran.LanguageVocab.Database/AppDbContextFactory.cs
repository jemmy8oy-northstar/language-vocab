using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Balenthiran.LanguageVocab.Database;

/// <summary>
/// Design-time factory used only by the EF Core tools (`dotnet ef migrations …`).
/// It supplies the Npgsql provider so migrations can be scaffolded without a
/// running application or a live database connection. Never used at runtime.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=language_vocab_design;Username=postgres;Password=postgres",
                b => b.MigrationsAssembly("Balenthiran.LanguageVocab.Database"))
            .Options;

        return new AppDbContext(options);
    }
}
