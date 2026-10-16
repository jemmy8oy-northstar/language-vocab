using System.Text.Json;
using Balenthiran.LanguageVocab.Abstractions.Packs;
using Balenthiran.LanguageVocab.Abstractions.Services;
using Balenthiran.LanguageVocab.DataModels.Packs;

namespace Balenthiran.LanguageVocab.Services.Packs;

/// <summary>
/// Reads pack files (BEH-PACK-1). A pack's words are its <c>includes</c> (words another list
/// already seeds, e.g. HSK1's numbers) followed by its own <c>entries</c>, which the seed loader
/// adds like any list. Membership lives in the file, not the database, so a pack needs no schema.
/// </summary>
public class PackCatalog : IPackCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PackCatalog(IEnumerable<string> packJsons)
    {
        All = packJsons.Select(Parse).ToList();

        var duplicate = All.GroupBy(p => p.Id).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Two pack files both declare pack '{duplicate.Key}'.");
    }

    public IReadOnlyList<IPack> All { get; }

    public IPack? Find(string id) => All.FirstOrDefault(p => p.Id == id);

    private static Pack Parse(string json)
    {
        var file = JsonSerializer.Deserialize<PackFile>(json, JsonOptions)
                   ?? throw new InvalidOperationException("Pack JSON deserialised to null.");
        if (string.IsNullOrWhiteSpace(file.Pack) || string.IsNullOrWhiteSpace(file.Title))
            throw new InvalidOperationException("A pack file needs a 'pack' id and a 'title'.");

        var hanzi = (file.Includes ?? []).Concat((file.Entries ?? []).Select(e => e.Hanzi)).Distinct().ToList();
        if (hanzi.Count == 0)
            throw new InvalidOperationException($"Pack '{file.Pack}' has no words.");

        return new Pack(file.Pack, file.Title, hanzi);
    }

    private sealed record PackFile(string Pack, string Title, List<string>? Includes, List<PackEntry>? Entries);

    private sealed record PackEntry(string Hanzi);
}
