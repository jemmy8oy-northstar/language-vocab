using System.Text.Json.Serialization;
using Scalar.AspNetCore;
using Balenthiran.LanguageVocab.WebApi;
using Balenthiran.LanguageVocab.WebApi.Routes;
using Balenthiran.LanguageVocab.Abstractions.Seeding;
using Balenthiran.LanguageVocab.Database;
using Balenthiran.LanguageVocab.Database.Seed;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddBackendServices(builder.Configuration);
// Enums travel as names ("ZhToEn", "Almost"), so the client reads them without a number table.
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
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

        // A pack's own words (BEH-PACK-1) are seeded like any list; its `includes` already were.
        foreach (var pack in SeedResources.PackJsons())
        {
            var packSeeded = await scope.ServiceProvider.GetRequiredService<ISeedLoader>().LoadAsync(pack);
            app.Logger.LogInformation("Pack seed: {Inserted} inserted, {Updated} updated, {Unchanged} unchanged",
                packSeeded.Inserted, packSeeded.Updated, packSeeded.Unchanged);
        }
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
    .MapPackRoutes()
    .WithOpenApi();

app.Run();
