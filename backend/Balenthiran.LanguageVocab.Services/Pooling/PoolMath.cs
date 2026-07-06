using Balenthiran.LanguageVocab.Abstractions.Enums;

namespace Balenthiran.LanguageVocab.Services.Pooling;

/// <summary>
/// The pure, side-effect-free heart of the adaptive pool (design assumption A4).
/// Every rule that decides *what* happens — how strength moves, which word is picked,
/// when the pool grows — lives here as a deterministic function so it can be exhaustively
/// unit-tested without a database. <see cref="PoolService"/> is only the persistence shell
/// around these.
/// </summary>
public static class PoolMath
{
    /// <summary>Words the pool starts with, taken in frequency-rank order (MVP §3).</summary>
    public const int BootstrapSize = 10;

    /// <summary>Words added each time the unlock threshold is crossed.</summary>
    public const int UnlockBatchSize = 5;

    /// <summary>The pool grows once its mean strength reaches this (design assumption A4).</summary>
    public const double UnlockMeanStrengthThreshold = 3.5;

    public const int MinStrength = 0;
    public const int MaxStrength = 5;

    /// <summary>Clamps a strength into the valid 0–5 band.</summary>
    public static int Clamp(int strength) => Math.Clamp(strength, MinStrength, MaxStrength);

    /// <summary>
    /// Strength after an answer: +1 full credit, −1 miss, ±0 "almost" (design assumption A4),
    /// clamped to 0–5.
    /// </summary>
    public static int NextStrength(int current, AnswerVerdict verdict) => verdict switch
    {
        AnswerVerdict.Correct => Clamp(current + 1),
        AnswerVerdict.Wrong => Clamp(current - 1),
        AnswerVerdict.Almost => Clamp(current),
        _ => Clamp(current),
    };

    /// <summary>
    /// Weakness weight for sampling: the weakest word (strength 0) is most likely, a fully
    /// mastered word (strength 5) least likely but never zero — so mastered words are still
    /// revisited occasionally rather than falling out of rotation entirely.
    /// </summary>
    public static int Weight(int strength) => MaxStrength + 1 - Clamp(strength); // 6 (weak) … 1 (mastered)

    /// <summary>
    /// True when the pool should unlock more words: it is non-empty and its mean strength has
    /// reached <see cref="UnlockMeanStrengthThreshold"/>. An empty pool never unlocks (it must
    /// be bootstrapped first).
    /// </summary>
    public static bool ShouldUnlock(IReadOnlyCollection<int> poolStrengths)
        => poolStrengths.Count > 0
           && poolStrengths.Average(s => (double)Clamp(s)) >= UnlockMeanStrengthThreshold;

    /// <summary>
    /// Deterministic weakness-weighted index into a pool, given a <paramref name="seed"/>.
    /// The same (strengths, seed) always yields the same index; weaker words occupy a larger
    /// share of the seed space. Returns −1 for an empty pool. Caller supplies a stably-ordered
    /// list so the mapping from seed to word is reproducible.
    /// </summary>
    public static int WeightedPickIndex(IReadOnlyList<int> poolStrengths, int seed)
    {
        if (poolStrengths.Count == 0)
            return -1;

        long total = 0;
        foreach (var s in poolStrengths)
            total += Weight(s);

        // Fold the (possibly negative) seed into a non-negative target in [0, total).
        long target = (seed & 0x7fffffffL) % total;

        long cumulative = 0;
        for (int i = 0; i < poolStrengths.Count; i++)
        {
            cumulative += Weight(poolStrengths[i]);
            if (target < cumulative)
                return i;
        }

        return poolStrengths.Count - 1; // unreachable; guards against arithmetic edge cases
    }
}
