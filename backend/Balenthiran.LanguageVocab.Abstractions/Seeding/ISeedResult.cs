namespace Balenthiran.LanguageVocab.Abstractions.Seeding;

/// <summary>Outcome of a seed load, for logging at startup.</summary>
public interface ISeedResult
{
    int Inserted { get; }
    int Updated { get; }
    int Unchanged { get; }
    int Total { get; }
}
