using Balenthiran.LanguageVocab.Abstractions.Seeding;

namespace Balenthiran.LanguageVocab.DataModels.Seeding;

/// <inheritdoc cref="ISeedResult"/>
public record SeedResult(int Inserted, int Updated, int Unchanged) : ISeedResult
{
    public int Total => Inserted + Updated + Unchanged;
}
