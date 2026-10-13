using Scalar.AspNetCore;
using Balenthiran.LanguageVocab.WebApi;
using Balenthiran.LanguageVocab.WebApi.Routes;
using Balenthiran.LanguageVocab.Abstractions.Seeding;
using Balenthiran.LanguageVocab.Database;
using Balenthiran.LanguageVocab.Database.Seed;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddBackendServices(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/scalar/v1");
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetService<AppDbContext>();
    if (dbContext is null)
        app.Logger.LogWarning("Skipping database migration — no connection string configured.");
    else
    {
        dbContext.Database.Migrate();
        // Idempotent (MVP §1), so every startup brings the vocabulary up to the embedded list.
        var seeded = await scope.ServiceProvider.GetRequiredService<ISeedLoader>().LoadAsync(SeedResources.Hsk1Json());
        app.Logger.LogInformation("HSK1 seed: {Inserted} inserted, {Updated} updated, {Unchanged} unchanged",
            seeded.Inserted, seeded.Updated, seeded.Unchanged);
    }
}

// The ingress routes /language-vocab/api here without stripping the prefix, so the app has
// to know it is hosted under one. Unset locally, where the API sits at the root.
var pathBase = builder.Configuration["PathBase"];
if (!string.IsNullOrWhiteSpace(pathBase))
    app.UsePathBase(pathBase);

app.UseHttpsRedirection();

app.MapGroup("/api")
    .MapStatusRoutes()
    .MapDrillRoutes()
    .WithOpenApi();

app.Run();
