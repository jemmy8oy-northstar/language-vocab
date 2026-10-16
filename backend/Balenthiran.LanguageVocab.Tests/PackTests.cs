using System.Text.Json;
using Balenthiran.LanguageVocab.Database;
using Balenthiran.LanguageVocab.Database.Seed;
using Balenthiran.LanguageVocab.DataModels.Packs;
using Balenthiran.LanguageVocab.EntityModels;
using Balenthiran.LanguageVocab.Services.Drilling;
using Balenthiran.LanguageVocab.Services.Grading;
using Balenthiran.LanguageVocab.Services.Packs;
using Balenthiran.LanguageVocab.Services.Pooling;
using Balenthiran.LanguageVocab.Services.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Balenthiran.LanguageVocab.Tests;

/// <summary>
/// Vocab packs (BEH-PACK-1): learning a pack teaches only its words, pulling them into the pool
/// ahead of the rank order, and the shipped pack files agree with the list they lean on.
/// </summary>
public class PackTests
{
    private const string User = "user-1";

    private static AppDbContext NewDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase($"pack-{Guid.NewGuid()}")
        .Options);

    private static DrillService NewService(AppDbContext db)
    {
        var math = new PoolMath();
        return new DrillService(db, new PoolService(db, math), math, new PinyinGrader());
    }

    /// <summary>Thirty words ranked 1..30, so the pack's words (ranks 28-30) sit outside a fresh pool.</summary>
    private static List<VocabItemEntity> Seed(AppDbContext db)
    {
        var items = Enumerable.Range(1, 30).Select(rank => new VocabItemEntity
        {
            Id = Guid.NewGuid(),
            Hanzi = $"字{rank}",
            Pinyin = "zì",
            PinyinNormalised = "zi",
            Glosses = [$"word {rank}"],
            Level = 1,
            FrequencyRank = rank,
        }).ToList();
        db.VocabItems.AddRange(items);
        db.SaveChanges();
        return items;
    }

    [Fact]
    public async Task Learning_a_pack_teaches_only_its_words_in_the_packs_order_and_pools_them()
    {
        await using var db = NewDb();
        Seed(db);
        var pack = new Pack("test", "Test", ["字30", "字28", "字29"]);

        var round = await NewService(db).LearnPackAsync(User, pack, 5);

        Assert.Equal(["字30", "字28", "字29"], round.Select(c => c.Hanzi));
        var pooled = await db.UserWordStates.Where(s => s.UserId == User).Select(s => s.VocabItem!.Hanzi).ToListAsync();
        Assert.Contains("字30", pooled);
        Assert.Contains("字1", pooled); // the ordinary bootstrap still happened
    }

    [Fact]
    public async Task A_pack_word_already_seen_comes_after_the_unseen_ones()
    {
        await using var db = NewDb();
        Seed(db);
        var svc = NewService(db);
        var pack = new Pack("test", "Test", ["字28", "字29"]);
        await svc.LearnPackAsync(User, pack, 5);
        var seen = await db.UserWordStates.SingleAsync(s => s.UserId == User && s.VocabItem!.Hanzi == "字28");
        seen.TimesSeen = 3;
        await db.SaveChangesAsync();

        var round = await svc.LearnPackAsync(User, pack, 5);

        Assert.Equal(["字29", "字28"], round.Select(c => c.Hanzi));
    }

    [Fact]
    public async Task Learning_a_pack_twice_does_not_pool_its_words_twice()
    {
        await using var db = NewDb();
        Seed(db);
        var svc = NewService(db);
        var pack = new Pack("test", "Test", ["字30"]);

        await svc.LearnPackAsync(User, pack, 5);
        await svc.LearnPackAsync(User, pack, 5);

        Assert.Equal(1, await db.UserWordStates.CountAsync(s => s.UserId == User && s.VocabItem!.Hanzi == "字30"));
    }

    [Fact]
    public void The_shipped_mahjong_pack_has_the_numbers_suits_winds_and_dragons()
    {
        var mahjong = new PackCatalog(SeedResources.PackJsons()).Find("mahjong");

        Assert.NotNull(mahjong);
        Assert.Equal("Mahjong", mahjong.Title);
        foreach (var h in new[] { "一", "九", "万", "条", "饼", "东风", "南风", "西风", "北风", "红中", "发财", "白板" })
            Assert.Contains(h, mahjong.Hanzi);
    }

    [Fact]
    public async Task Every_pack_word_exists_once_both_seeds_have_loaded()
    {
        // Seeds the real lists the way Program.cs does, then checks every pack word landed. An
        // `includes` typo would otherwise ship a pack that quietly teaches fewer words than it lists.
        await using var db = NewDb();
        var loader = new SeedLoader(db);
        await loader.LoadAsync(SeedResources.Hsk1Json());
        foreach (var json in SeedResources.PackJsons())
            await loader.LoadAsync(json);

        var known = (await db.VocabItems.Select(v => v.Hanzi).ToListAsync()).ToHashSet();
        foreach (var pack in new PackCatalog(SeedResources.PackJsons()).All)
            Assert.All(pack.Hanzi, h => Assert.Contains(h, known));
    }

    [Fact]
    public void A_pack_entry_never_redefines_a_word_HSK1_already_has()
    {
        // The seed loader upserts by hanzi, so a pack entry for an HSK1 word would overwrite its
        // glosses and rank. Shared words belong in `includes`, which only names them.
        var hsk = JsonDocument.Parse(SeedResources.Hsk1Json()).RootElement.GetProperty("entries")
            .EnumerateArray().Select(e => e.GetProperty("hanzi").GetString()).ToHashSet();

        foreach (var json in SeedResources.PackJsons())
        {
            var entries = JsonDocument.Parse(json).RootElement.GetProperty("entries").EnumerateArray();
            Assert.All(entries, e => Assert.DoesNotContain(e.GetProperty("hanzi").GetString(), hsk));
        }
    }

    [Fact]
    public void Two_files_declaring_one_pack_are_refused()
    {
        const string json = """{ "pack": "p", "title": "P", "includes": ["一"] }""";

        Assert.Throws<InvalidOperationException>(() => new PackCatalog([json, json]));
    }
}
