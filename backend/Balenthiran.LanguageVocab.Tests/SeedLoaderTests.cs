using Balenthiran.LanguageVocab.Abstractions.Enums;
using Balenthiran.LanguageVocab.Database;
using Balenthiran.LanguageVocab.Database.Seed;
using Balenthiran.LanguageVocab.Services.Grading;
using Balenthiran.LanguageVocab.Services.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Balenthiran.LanguageVocab.Tests;

/// <summary>
/// The seed loader must be safe to run on every startup (design assumption A3): idempotent
/// upserts keyed on (language, hanzi), with normalised pinyin derived the same way the grader
/// derives it. Exercised against an in-memory database and the real embedded HSK1 resource.
/// </summary>
public class SeedLoaderTests
{
    private const string SampleJson = """
    {
      "language": "zh",
      "list": "TEST",
      "version": 1,
      "entries": [
        { "hanzi": "我", "pinyin": "wǒ", "glosses": ["I", "me"], "level": 1, "frequencyRank": 1 },
        { "hanzi": "你", "pinyin": "nǐ", "glosses": ["you"], "level": 1, "frequencyRank": 2 }
      ]
    }
    """;

    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"seed-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Load_inserts_all_entries_on_a_fresh_db()
    {
        using var db = NewDb();
        var result = await new SeedLoader(db).LoadAsync(SampleJson);

        Assert.Equal(2, result.Inserted);
        Assert.Equal(0, result.Updated);
        Assert.Equal(2, result.Total);
        Assert.Equal(2, await db.VocabItems.CountAsync());
    }

    [Fact]
    public async Task Load_derives_normalised_pinyin_via_the_grader()
    {
        using var db = NewDb();
        await new SeedLoader(db).LoadAsync(SampleJson);

        var wo = await db.VocabItems.SingleAsync(v => v.Hanzi == "我");
        Assert.Equal(PinyinGrader.Normalise("wǒ"), wo.PinyinNormalised);
        Assert.Equal("wo", wo.PinyinNormalised); // toneless comparison key
    }

    [Fact]
    public async Task Load_is_idempotent_second_run_changes_nothing()
    {
        using var db = NewDb();
        var loader = new SeedLoader(db);

        await loader.LoadAsync(SampleJson);
        var second = await loader.LoadAsync(SampleJson);

        Assert.Equal(0, second.Inserted);
        Assert.Equal(0, second.Updated);
        Assert.Equal(2, second.Unchanged);
        Assert.Equal(2, await db.VocabItems.CountAsync()); // no duplicates
    }

    [Fact]
    public async Task Load_updates_changed_rows_in_place()
    {
        using var db = NewDb();
        await new SeedLoader(db).LoadAsync(SampleJson);

        const string edited = """
        {
          "language": "zh", "list": "TEST", "version": 2,
          "entries": [
            { "hanzi": "我", "pinyin": "wǒ", "glosses": ["I", "me", "myself"], "level": 1, "frequencyRank": 1 },
            { "hanzi": "你", "pinyin": "nǐ", "glosses": ["you"], "level": 1, "frequencyRank": 2 }
          ]
        }
        """;
        var result = await new SeedLoader(db).LoadAsync(edited);

        Assert.Equal(0, result.Inserted);
        Assert.Equal(1, result.Updated);   // 我 gained a gloss
        Assert.Equal(1, result.Unchanged); // 你 untouched
        var wo = await db.VocabItems.SingleAsync(v => v.Hanzi == "我");
        Assert.Contains("myself", wo.Glosses);
        Assert.Equal(2, await db.VocabItems.CountAsync());
    }

    [Fact]
    public async Task Load_defaults_item_type_to_word()
    {
        using var db = NewDb();
        await new SeedLoader(db).LoadAsync(SampleJson);
        Assert.All(await db.VocabItems.ToListAsync(), v => Assert.Equal(VocabItemType.Word, v.Type));
    }

    [Fact]
    public async Task Load_throws_on_missing_language()
    {
        using var db = NewDb();
        const string noLang = """{ "list": "TEST", "version": 1, "entries": [] }""";
        await Assert.ThrowsAsync<InvalidOperationException>(() => new SeedLoader(db).LoadAsync(noLang));
    }

    // ---- the real embedded HSK1 resource ----

    [Fact]
    public void EmbeddedHsk1_resource_is_present_and_parses()
    {
        var json = SeedResources.Hsk1Json();
        Assert.Contains("\"language\": \"zh\"", json);
        Assert.Contains("HSK1", json);
    }

    [Fact]
    public async Task Load_the_real_hsk1_list_inserts_150_words()
    {
        using var db = NewDb();
        var result = await new SeedLoader(db).LoadAsync(SeedResources.Hsk1Json());

        Assert.Equal(150, result.Inserted);
        Assert.Equal(150, await db.VocabItems.CountAsync());
        // Every row got a non-empty normalised key and at least one gloss.
        Assert.All(await db.VocabItems.ToListAsync(), v =>
        {
            Assert.False(string.IsNullOrEmpty(v.PinyinNormalised));
            Assert.NotEmpty(v.Glosses);
        });
    }
}
