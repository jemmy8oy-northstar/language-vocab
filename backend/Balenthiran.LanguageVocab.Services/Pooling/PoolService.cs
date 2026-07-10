using Balenthiran.LanguageVocab.Abstractions.Services;
using Balenthiran.LanguageVocab.Database;
using Balenthiran.LanguageVocab.EntityModels;
using Microsoft.EntityFrameworkCore;

namespace Balenthiran.LanguageVocab.Services.Pooling;

/// <summary>
/// Persistence shell around <see cref="IPoolMath"/>: loads a user's active pool from the
/// database, applies the pure rules, and writes back new membership. All decisions live in
/// the injected <see cref="IPoolMath"/>; this type only moves rows.
/// </summary>
public class PoolService(AppDbContext db, IPoolMath math) : IPoolService
{
    /// <summary>The user's active-pool rows for one language (state rows join their vocab item).</summary>
    private IQueryable<UserWordStateEntity> Pool(string userId, string language)
        => db.UserWordStates.Where(s => s.UserId == userId && s.VocabItem!.Language == language);

    public async Task<IReadOnlyList<Guid>> EnsureBootstrappedAsync(
        string userId, string language, CancellationToken ct = default)
    {
        if (await Pool(userId, language).AnyAsync(ct))
            return [];

        var firstByRank = await db.VocabItems
            .Where(v => v.Language == language)
            .OrderBy(v => v.FrequencyRank).ThenBy(v => v.Id)
            .Take(math.BootstrapSize)
            .Select(v => v.Id)
            .ToListAsync(ct);

        return await AddToPoolAsync(userId, firstByRank, ct);
    }

    public async Task<Guid?> SelectNextAsync(
        string userId, string language, int seed, CancellationToken ct = default)
    {
        // Stable ordering (rank, then id) makes the seed→word mapping reproducible.
        var pool = await Pool(userId, language)
            .OrderBy(s => s.VocabItem!.FrequencyRank).ThenBy(s => s.VocabItemId)
            .Select(s => new { s.VocabItemId, s.Strength })
            .ToListAsync(ct);

        if (pool.Count == 0)
            return null;

        var index = math.WeightedPickIndex(pool.Select(p => p.Strength).ToList(), seed);
        return pool[index].VocabItemId;
    }

    public async Task<IReadOnlyList<Guid>> TryUnlockAsync(
        string userId, string language, CancellationToken ct = default)
    {
        var strengths = await Pool(userId, language).Select(s => s.Strength).ToListAsync(ct);
        if (!math.ShouldUnlock(strengths))
            return [];

        var alreadyInPool = Pool(userId, language).Select(s => s.VocabItemId);
        var nextByRank = await db.VocabItems
            .Where(v => v.Language == language && !alreadyInPool.Contains(v.Id))
            .OrderBy(v => v.FrequencyRank).ThenBy(v => v.Id)
            .Take(math.UnlockBatchSize)
            .Select(v => v.Id)
            .ToListAsync(ct);

        return await AddToPoolAsync(userId, nextByRank, ct);
    }

    private async Task<IReadOnlyList<Guid>> AddToPoolAsync(
        string userId, IReadOnlyList<Guid> vocabItemIds, CancellationToken ct)
    {
        if (vocabItemIds.Count == 0)
            return [];

        foreach (var id in vocabItemIds)
        {
            db.UserWordStates.Add(new UserWordStateEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                VocabItemId = id,
                Strength = 0,
            });
        }

        await db.SaveChangesAsync(ct);
        return vocabItemIds;
    }
}
