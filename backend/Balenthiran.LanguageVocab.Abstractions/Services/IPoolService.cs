namespace Balenthiran.LanguageVocab.Abstractions.Services;

/// <summary>
/// Manages a user's active vocabulary pool (design assumption A4): bootstrap,
/// weakness-weighted selection, and the grow-on-mastery unlock rule. Selection is
/// deterministic given the pool state and the supplied <c>seed</c>, so the picker is
/// unit-testable and reproducible. Item identity is exchanged as <see cref="Guid"/>
/// so this contract stays free of persistence types. The app drills one language, fixed in
/// the implementation; the <c>Language</c> column stays on every vocab row, so a second
/// language later is a signature change here, not a data migration.
/// </summary>
public interface IPoolService
{
    /// <summary>
    /// Ensures the user has an active pool. If they have none, seeds
    /// it with the first <c>PoolMath.BootstrapSize</c> items by frequency rank. Returns
    /// the ids added (empty if a pool already existed). Idempotent.
    /// </summary>
    Task<IReadOnlyList<Guid>> EnsureBootstrappedAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Picks the next item to drill from the active pool, weakness-weighted and
    /// deterministic for a given <paramref name="seed"/>. Returns <c>null</c> if the
    /// pool is empty.
    /// </summary>
    Task<Guid?> SelectNextAsync(string userId, int seed, CancellationToken ct = default);

    /// <summary>
    /// If the active pool's mean strength has reached the unlock threshold, adds the
    /// next items by frequency rank (up to <c>PoolMath.UnlockBatchSize</c>). Returns the
    /// ids newly unlocked (empty if the threshold is unmet or nothing is left to add).
    /// </summary>
    Task<IReadOnlyList<Guid>> TryUnlockAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Adds whichever of <paramref name="vocabItemIds"/> are not yet in the user's pool, at zero
    /// strength — how a pack (BEH-PACK-1) brings its words in ahead of the rank order. Returns
    /// the ids newly added. Idempotent.
    /// </summary>
    Task<IReadOnlyList<Guid>> EnsureInPoolAsync(
        string userId, IReadOnlyCollection<Guid> vocabItemIds, CancellationToken ct = default);
}
