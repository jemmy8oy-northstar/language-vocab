using Balenthiran.LanguageVocab.Abstractions.Enums;
using Balenthiran.LanguageVocab.Abstractions.Services;

namespace Balenthiran.LanguageVocab.Services.Pooling;

/// <summary>
/// The pure, side-effect-free heart of the adaptive pool (design assumption A4).
/// Every rule that decides *what* happens — how strength moves, which word is picked,
/// when the pool grows — lives here as a deterministic function so it can be exhaustively
/// unit-tested without a database. <see cref="PoolService"/> is the persistence shell around it.
/// </summary>
public class PoolMath : IPoolMath
{
    private const double UnlockMeanStrengthThreshold = 3.5;
    private const int MinStrength = 0;
    private const int MaxStrength = 5;

    public int BootstrapSize => 10;

    public int UnlockBatchSize => 5;

    public int NextStrength(int current, AnswerVerdict verdict) => verdict switch
    {
        AnswerVerdict.Correct => Clamp(current + 1),
        AnswerVerdict.Wrong => Clamp(current - 1),
        AnswerVerdict.Almost => Clamp(current),
        _ => Clamp(current),
    };

    public int Weight(int strength) => MaxStrength + 1 - Clamp(strength); // 6 (weak) … 1 (mastered)

    public bool ShouldUnlock(IReadOnlyCollection<int> poolStrengths)
        => poolStrengths.Count > 0
           && poolStrengths.Average(s => (double)Clamp(s)) >= UnlockMeanStrengthThreshold;

    public int WeightedPickIndex(IReadOnlyList<int> poolStrengths, int seed)
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

    /// <summary>Clamps a strength into the valid 0–5 band.</summary>
    private static int Clamp(int strength) => Math.Clamp(strength, MinStrength, MaxStrength);
}
