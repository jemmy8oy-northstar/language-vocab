using System.Text.Json;
using System.Text.Json.Serialization;
using Balenthiran.LanguageVocab.Abstractions.Enums;
using Balenthiran.LanguageVocab.Abstractions.Seeding;
using Balenthiran.LanguageVocab.Database;
using Balenthiran.LanguageVocab.EntityModels;
using Balenthiran.LanguageVocab.Services.Grading;
using Microsoft.EntityFrameworkCore;

namespace Balenthiran.LanguageVocab.Services.Seeding;

/// <summary>
/// Idempotent word-list loader (design assumption A3). Upserts vocabulary keyed on
/// (language, hanzi): inserts new words, updates changed ones in place, leaves identical
/// rows untouched — so it is safe to run on every startup and re-seeding never duplicates.
/// <see cref="VocabItemEntity.PinyinNormalised"/> is derived here via
/// <see cref="PinyinGrader.Normalise"/> so lookups and grading share one notion of "same syllables".
/// </summary>
public class SeedLoader(AppDbContext db) : ISeedLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task<SeedResult> LoadAsync(string json, CancellationToken ct = default)
    {
        var file = JsonSerializer.Deserialize<SeedFile>(json, JsonOptions)
                   ?? throw new InvalidOperationException("Seed JSON deserialised to null.");
        if (string.IsNullOrWhiteSpace(file.Language))
            throw new InvalidOperationException("Seed file is missing a top-level 'language'.");

        int inserted = 0, updated = 0, unchanged = 0;

        // Load the existing rows for this language once, keyed by hanzi, to avoid a query per entry.
        var existing = await db.VocabItems
            .Where(v => v.Language == file.Language)
            .ToDictionaryAsync(v => v.Hanzi, ct);

        foreach (var entry in file.Entries)
        {
            var normalised = PinyinGrader.Normalise(entry.Pinyin);

            if (existing.TryGetValue(entry.Hanzi, out var row))
            {
                if (ApplyTo(row, file.Language, entry, normalised))
                    updated++;
                else
                    unchanged++;
            }
            else
            {
                var fresh = new VocabItemEntity { Id = Guid.NewGuid(), Language = file.Language };
                ApplyTo(fresh, file.Language, entry, normalised);
                db.VocabItems.Add(fresh);
                inserted++;
            }
        }

        await db.SaveChangesAsync(ct);
        return new SeedResult(inserted, updated, unchanged);
    }

    /// <summary>Copies entry fields onto a row; returns true if anything actually changed.</summary>
    private static bool ApplyTo(VocabItemEntity row, string language, SeedEntry entry, string normalised)
    {
        var type = entry.Type ?? VocabItemType.Word;
        var changed = row.Language != language
            || row.Hanzi != entry.Hanzi
            || row.Pinyin != entry.Pinyin
            || row.PinyinNormalised != normalised
            || row.Level != entry.Level
            || row.FrequencyRank != entry.FrequencyRank
            || row.Type != type
            || !row.Glosses.SequenceEqual(entry.Glosses);

        row.Language = language;
        row.Hanzi = entry.Hanzi;
        row.Pinyin = entry.Pinyin;
        row.PinyinNormalised = normalised;
        row.Glosses = entry.Glosses;
        row.Level = entry.Level;
        row.FrequencyRank = entry.FrequencyRank;
        row.Type = type;
        return changed;
    }

    private sealed record SeedFile(
        string Language,
        string List,
        int Version,
        List<SeedEntry> Entries);

    private sealed record SeedEntry(
        string Hanzi,
        string Pinyin,
        List<string> Glosses,
        int Level,
        int FrequencyRank,
        [property: JsonPropertyName("type")] VocabItemType? Type);
}
