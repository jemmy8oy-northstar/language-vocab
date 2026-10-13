using Balenthiran.LanguageVocab.Abstractions.Enums;

namespace Balenthiran.LanguageVocab.Abstractions.Services;

/// <summary>
/// The pure, side-effect-free rules of the adaptive pool (design assumption A4):
/// how strength moves, how words are weighted, when the pool grows, and which word a
/// given seed selects. A DI service (not a static class) so it can be injected into
/// <see cref="IPoolService"/>, mocked, and evolved without touching every call site.
/// </summary>
public interface IPoolMath
{
    /// <summary>Words the pool starts with, taken in frequency-rank order (MVP §3).</summary>
    int BootstrapSize { get; }

    /// <summary>Words added each time the unlock threshold is crossed.</summary>
    int UnlockBatchSize { get; }

    /// <summary>
    /// Strength after an answer: +1 full credit, −1 miss, ±0 "almost" (A4), clamped to 0–5.
    /// </summary>
    int NextStrength(int current, AnswerVerdict verdict);

    /// <summary>
    /// Weakness weight for sampling: the weakest word is most likely, a fully mastered word
    /// least likely but never zero — so mastered words are still revisited occasionally.
    /// </summary>
    int Weight(int strength);

    /// <summary>
    /// True when the pool should unlock more words: non-empty and its mean strength has reached
    /// the unlock threshold. An empty pool never unlocks (it must be bootstrapped first).
    /// </summary>
    bool ShouldUnlock(IReadOnlyCollection<int> poolStrengths);

    /// <summary>
    /// Deterministic weakness-weighted index into a pool, given a <paramref name="seed"/>.
    /// The same (strengths, seed) always yields the same index; weaker words occupy a larger
    /// share of the seed space. Returns −1 for an empty pool.
    /// </summary>
    int WeightedPickIndex(IReadOnlyList<int> poolStrengths, int seed);
}
