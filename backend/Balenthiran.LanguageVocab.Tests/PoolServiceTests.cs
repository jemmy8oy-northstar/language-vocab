using Balenthiran.LanguageVocab.Abstractions.Services;
using Balenthiran.LanguageVocab.Database;
using Balenthiran.LanguageVocab.EntityModels;
using Balenthiran.LanguageVocab.Services.Pooling;
using Microsoft.EntityFrameworkCore;

namespace Balenthiran.LanguageVocab.Tests;

/// <summary>
/// The persistence behaviour of <see cref="PoolService"/> against an in-memory database:
/// bootstrap picks the right words, selection stays inside the user's pool, and the unlock
/// rule grows the pool by frequency rank only when earned. The picking maths itself lives in
/// <see cref="PoolMathTests"/>.
/// </summary>
public class PoolServiceTests
{
    private const string Zh = PoolService.Language;
    private const string User = "user-1";

    private readonly IPoolMath _math = new PoolMath();

    private static AppDbContext NewDb()
    {
        // A distinct store per context keeps tests isolated.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"pool-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>Adds <paramref name="count"/> vocab items for a language, ranks 1..count.</summary>
    private static List<VocabItemEntity> SeedVocab(AppDbContext db, string language, int count)
    {
        var items = new List<VocabItemEntity>();
        for (var rank = 1; rank <= count; rank++)
        {
            var item = new VocabItemEntity
            {
                Id = Guid.NewGuid(),
                Language = language,
                Hanzi = $"{language}-{rank}",
                Pinyin = "x",
                PinyinNormalised = "x",
                Level = 1,
                FrequencyRank = rank,
            };
            items.Add(item);
            db.VocabItems.Add(item);
        }
        db.SaveChanges();
        return items;
    }

    [Fact]
    public async Task Bootstrap_creates_first_ten_by_rank()
    {
        using var db = NewDb();
        var vocab = SeedVocab(db, Zh, 20);
        var svc = new PoolService(db, _math);

        var added = await svc.EnsureBootstrappedAsync(User);

        Assert.Equal(_math.BootstrapSize, added.Count);
        var expected = vocab.OrderBy(v => v.FrequencyRank).Take(10).Select(v => v.Id).ToHashSet();
        Assert.Equal(expected, added.ToHashSet());
        Assert.Equal(10, await db.UserWordStates.CountAsync());
    }

    [Fact]
    public async Task Bootstrap_is_idempotent()
    {
        using var db = NewDb();
        SeedVocab(db, Zh, 20);
        var svc = new PoolService(db, _math);

        await svc.EnsureBootstrappedAsync(User);
        var secondCall = await svc.EnsureBootstrappedAsync(User);

        Assert.Empty(secondCall);
        Assert.Equal(10, await db.UserWordStates.CountAsync());
    }

    [Fact]
    public async Task Bootstrap_with_fewer_than_ten_items_takes_all()
    {
        using var db = NewDb();
        SeedVocab(db, Zh, 4);
        var svc = new PoolService(db, _math);

        var added = await svc.EnsureBootstrappedAsync(User);

        Assert.Equal(4, added.Count);
    }

    [Fact]
    public async Task Bootstrap_ignores_other_languages()
    {
        using var db = NewDb();
        SeedVocab(db, Zh, 20);
        SeedVocab(db, "fr", 20);
        var svc = new PoolService(db, _math);

        var added = await svc.EnsureBootstrappedAsync(User);

        Assert.All(added, id => Assert.Equal(Zh, db.VocabItems.Single(v => v.Id == id).Language));
        Assert.Equal(10, await db.UserWordStates.CountAsync());
    }

    [Fact]
    public async Task SelectNext_returns_null_for_empty_pool()
    {
        using var db = NewDb();
        SeedVocab(db, Zh, 20);
        var svc = new PoolService(db, _math);

        Assert.Null(await svc.SelectNextAsync(User, seed: 1));
    }

    [Fact]
    public async Task SelectNext_only_picks_from_the_users_pool()
    {
        using var db = NewDb();
        SeedVocab(db, Zh, 20);
        var svc = new PoolService(db, _math);
        await svc.EnsureBootstrappedAsync(User);
        var pool = await db.UserWordStates.Select(s => s.VocabItemId).ToHashSetAsync();

        for (var seed = 0; seed < 50; seed++)
        {
            var picked = await svc.SelectNextAsync(User, seed);
            Assert.NotNull(picked);
            Assert.Contains(picked!.Value, pool);
        }
    }

    [Fact]
    public async Task SelectNext_is_deterministic_for_the_same_seed()
    {
        using var db = NewDb();
        SeedVocab(db, Zh, 20);
        var svc = new PoolService(db, _math);
        await svc.EnsureBootstrappedAsync(User);

        var first = await svc.SelectNextAsync(User, seed: 424242);
        var again = await svc.SelectNextAsync(User, seed: 424242);
        Assert.Equal(first, again);
    }

    [Fact]
    public async Task TryUnlock_does_nothing_below_threshold()
    {
        using var db = NewDb();
        SeedVocab(db, Zh, 20);
        var svc = new PoolService(db, _math);
        await svc.EnsureBootstrappedAsync(User); // all strength 0

        var unlocked = await svc.TryUnlockAsync(User);

        Assert.Empty(unlocked);
        Assert.Equal(10, await db.UserWordStates.CountAsync());
    }

    [Fact]
    public async Task TryUnlock_adds_next_batch_by_rank_when_mastered()
    {
        using var db = NewDb();
        var vocab = SeedVocab(db, Zh, 20);
        var svc = new PoolService(db, _math);
        await svc.EnsureBootstrappedAsync(User);

        // Master the whole pool so mean strength clears the threshold.
        await db.UserWordStates.ForEachAsync(s => s.Strength = 5);
        await db.SaveChangesAsync();

        var unlocked = await svc.TryUnlockAsync(User);

        var expected = vocab.OrderBy(v => v.FrequencyRank).Skip(10).Take(5).Select(v => v.Id).ToHashSet();
        Assert.Equal(expected, unlocked.ToHashSet());
        Assert.Equal(15, await db.UserWordStates.CountAsync());
    }

    [Fact]
    public async Task TryUnlock_returns_empty_when_nothing_left_to_add()
    {
        using var db = NewDb();
        SeedVocab(db, Zh, 10); // exactly the bootstrap set, nothing beyond it
        var svc = new PoolService(db, _math);
        await svc.EnsureBootstrappedAsync(User);
        await db.UserWordStates.ForEachAsync(s => s.Strength = 5);
        await db.SaveChangesAsync();

        var unlocked = await svc.TryUnlockAsync(User);

        Assert.Empty(unlocked);
        Assert.Equal(10, await db.UserWordStates.CountAsync());
    }
}
