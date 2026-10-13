namespace Balenthiran.LanguageVocab.Abstractions.Seeding;

/// <summary>
/// Idempotently loads a language word-list (JSON) into the vocabulary table, keyed on
/// (language, hanzi). Safe to run on every startup: existing rows are updated in place,
/// new rows inserted, unchanged rows left alone (design assumption A3, MVP step 1).
/// </summary>
public interface ISeedLoader
{
    /// <summary>Upserts the entries in a word-list JSON document. See <see cref="ISeedResult"/>.</summary>
    Task<ISeedResult> LoadAsync(string json, CancellationToken ct = default);
}
