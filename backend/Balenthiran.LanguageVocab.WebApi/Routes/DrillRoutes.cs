using Balenthiran.LanguageVocab.Abstractions.Enums;
using Balenthiran.LanguageVocab.Abstractions.Services;

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

        group.MapGet("/next", async (DrillDirection? direction, IDrillService drill, CancellationToken ct) =>
        {
            var card = await drill.NextAsync(Owner, direction, ct);
            return card is null ? Results.NotFound() : Results.Ok(card);
        })
        .WithName("GetNextCard");

        group.MapPost("/answer", async (AnswerRequest request, IDrillService drill, CancellationToken ct) =>
        {
            if (!Enum.IsDefined(request.Direction))
                return Results.BadRequest();

            var outcome = await drill.AnswerAsync(
                Owner, request.VocabItemId, request.Direction, request.Answer ?? "", ct);
            return outcome is null ? Results.NotFound() : Results.Ok(outcome);
        })
        .WithName("AnswerCard");

        return parentGroup;
    }
}
