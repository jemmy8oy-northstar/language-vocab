using Balenthiran.LanguageVocab.Abstractions.Packs;
using Balenthiran.LanguageVocab.Abstractions.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Balenthiran.LanguageVocab.WebApi.Routes;

public static class PackRoutes
{
    public static RouteGroupBuilder MapPackRoutes(this RouteGroupBuilder parentGroup)
    {
        var group = parentGroup.MapGroup("/packs");

        // BEH-PACK-1: the packs to choose from. Learning one is /drill/learn?pack=<id>.
        group.MapGet("", Ok<IReadOnlyList<IPack>> (IPackCatalog packs) => TypedResults.Ok(packs.All))
            .WithName("GetPacks");

        return parentGroup;
    }
}
