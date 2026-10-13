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
