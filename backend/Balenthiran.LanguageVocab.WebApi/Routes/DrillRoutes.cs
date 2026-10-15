using Balenthiran.LanguageVocab.Abstractions.Drilling;
using Balenthiran.LanguageVocab.Abstractions.Enums;
using Balenthiran.LanguageVocab.Abstractions.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Balenthiran.LanguageVocab.WebApi.Routes;

public static class DrillRoutes
{
    /// <summary>
    /// The one learner (A6: single-user reality, and the template ships no auth). The services
    /// all take a user id, so real accounts later change this constant, not the drill code.
    /// </summary>
    public const string Owner = "owner";

    /// <summary>His note: "flick through maybe 5 words" before the quiz (BEH-LEARN-1).</summary>
    public const int LearnRound = 5;

    public const int MaxLearnRound = 20;

    public record AnswerRequest(Guid VocabItemId, DrillDirection Direction, string? Answer);

    public static RouteGroupBuilder MapDrillRoutes(this RouteGroupBuilder parentGroup)
    {
        var group = parentGroup.MapGroup("/drill");

        // Typed results, so the OpenAPI schema (and the generated client) carries the shapes.
        group.MapGet("/next", async Task<Results<Ok<IDrillCard>, NotFound>> (
            DrillDirection? direction, IDrillService drill, CancellationToken ct) =>
        {
            var card = await drill.NextAsync(Owner, direction, ct);
            return card is null ? TypedResults.NotFound() : TypedResults.Ok(card);
        })
        .WithName("GetNextCard");

        // Learn mode (BEH-LEARN-1): a round of words to study, everything shown. Five unless asked,
        // and never more than a round's worth, so a stray query cannot dump the whole pool.
        group.MapGet("/learn", async Task<Results<Ok<IReadOnlyList<ILearnCard>>, BadRequest>> (
            int? count, IDrillService drill, CancellationToken ct) =>
        {
            var n = count ?? LearnRound;
            if (n is < 1 or > MaxLearnRound)
                return TypedResults.BadRequest();

            return TypedResults.Ok(await drill.LearnAsync(Owner, n, ct));
        })
        .WithName("GetLearnRound");

        group.MapPost("/answer", async Task<Results<Ok<IDrillOutcome>, NotFound, BadRequest>> (
            AnswerRequest request, IDrillService drill, CancellationToken ct) =>
        {
            if (!Enum.IsDefined(request.Direction))
                return TypedResults.BadRequest();

            var outcome = await drill.AnswerAsync(
                Owner, request.VocabItemId, request.Direction, request.Answer ?? "", ct);
            return outcome is null ? TypedResults.NotFound() : TypedResults.Ok(outcome);
        })
        .WithName("AnswerCard");

        return parentGroup;
    }
}
