using Balenthiran.LanguageVocab.Abstractions.Services;
using Balenthiran.LanguageVocab.Abstractions.Stats;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Balenthiran.LanguageVocab.WebApi.Routes;

public static class StatsRoutes
{
    public static RouteGroupBuilder MapStatsRoutes(this RouteGroupBuilder parentGroup)
    {
        var group = parentGroup.MapGroup("/stats");

        // BEH-STATS-1: pool size, per-word strength and accuracy over time, for the one learner.
        group.MapGet("", async Task<Ok<IStatsReport>> (IStatsService stats, CancellationToken ct) =>
                TypedResults.Ok(await stats.GetStatsAsync(DrillRoutes.Owner, ct)))
            .WithName("GetStats");

        return parentGroup;
    }
}
