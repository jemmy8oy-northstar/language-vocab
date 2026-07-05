using Scalar.AspNetCore;
using Balenthiran.LanguageVocab.WebApi;
using Balenthiran.LanguageVocab.WebApi.Routes;
using Balenthiran.LanguageVocab.Database;
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
        dbContext.Database.Migrate();
}

app.UseHttpsRedirection();

app.MapGroup("/api")
    .MapStatusRoutes()
    .WithOpenApi();

app.Run();
