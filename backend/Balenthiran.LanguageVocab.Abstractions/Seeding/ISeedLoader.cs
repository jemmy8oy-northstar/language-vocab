namespace Balenthiran.LanguageVocab.Abstractions.Seeding;

/// <summary>
/// Idempotently loads a language word-list (JSON) into the vocabulary table, keyed on
/// (language, hanzi). Safe to run on every startup: existing rows are updated in place,
/// new rows inserted, unchanged rows left alone (design assumption A3, MVP step 1).
/// </summary>
public interface ISeedLoader
{
    /// <summary>Upserts the entries in a word-list JSON document. See <see cref="SeedResult"/>.</summary>
    Task<SeedResult> LoadAsync(string json, CancellationToken ct = default);
}

/// <summary>Outcome of a seed load, for logging at startup.</summary>
public readonly record struct SeedResult(int Inserted, int Updated, int Unchanged)
{
    public int Total => Inserted + Updated + Unchanged;
}
