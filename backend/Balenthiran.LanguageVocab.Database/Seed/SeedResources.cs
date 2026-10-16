using System.Reflection;

namespace Balenthiran.LanguageVocab.Database.Seed;

/// <summary>
/// Accessor for the word-list seed data embedded in this assembly. Keeping the JSON in the
/// assembly means the seed loader works identically in tests, locally, and in the container
/// with no file-path or working-directory assumptions.
/// </summary>
public static class SeedResources
{
    private const string Hsk1ResourceName = "hsk1.json";

    /// <summary>The HSK1 word-list JSON, as embedded at build time.</summary>
    public static string Hsk1Json()
    {
        var assembly = typeof(SeedResources).Assembly;
        using var stream = assembly.GetManifestResourceStream(Hsk1ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded seed resource '{Hsk1ResourceName}' not found. Available: "
                + string.Join(", ", assembly.GetManifestResourceNames()));
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Every vocab pack (BEH-PACK-1) embedded from <c>Seed/packs/*.json</c>, by name order. A pack
    /// is a seed file plus <c>pack</c>, <c>title</c> and <c>includes</c>: its <c>entries</c> are new
    /// words seeded like any list, and <c>includes</c> names words another list already seeds.
    /// </summary>
    public static IReadOnlyList<string> PackJsons()
    {
        var assembly = typeof(SeedResources).Assembly;
        return assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith("pack.", StringComparison.Ordinal) && n.EndsWith(".json", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .Select(n =>
            {
                using var stream = assembly.GetManifestResourceStream(n)!;
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            })
            .ToList();
    }
}
