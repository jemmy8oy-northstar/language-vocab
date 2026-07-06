using Balenthiran.LanguageVocab.Abstractions.Enums;
using Balenthiran.LanguageVocab.Services.Pooling;

namespace Balenthiran.LanguageVocab.Tests;

/// <summary>
/// The pure pool rules (design assumption A4): strength transitions, weakness weighting,
/// the unlock threshold, and deterministic weighted selection. No database here — this is the
/// logic that decides the app's adaptive behaviour, so it is tested directly and exhaustively.
/// </summary>
public class PoolMathTests
{
    // ---- NextStrength: +1 correct, -1 wrong, 0 almost, clamped 0..5 (A4) ----

    [Theory]
    [InlineData(0, AnswerVerdict.Correct, 1)]
    [InlineData(3, AnswerVerdict.Correct, 4)]
    [InlineData(5, AnswerVerdict.Correct, 5)] // clamps at max
    [InlineData(3, AnswerVerdict.Wrong, 2)]
    [InlineData(0, AnswerVerdict.Wrong, 0)]   // clamps at min
    [InlineData(3, AnswerVerdict.Almost, 3)]  // almost is neutral
    [InlineData(0, AnswerVerdict.Almost, 0)]
    [InlineData(5, AnswerVerdict.Almost, 5)]
    public void NextStrength_moves_and_clamps(int current, AnswerVerdict verdict, int expected)
        => Assert.Equal(expected, PoolMath.NextStrength(current, verdict));

    // ---- Weight: weakest highest, mastered lowest but never zero ----

    [Theory]
    [InlineData(0, 6)]
    [InlineData(3, 3)]
    [InlineData(5, 1)]
    [InlineData(-2, 6)] // out-of-band strengths are clamped before weighting
    [InlineData(9, 1)]
    public void Weight_favours_weakness_and_stays_positive(int strength, int expected)
        => Assert.Equal(expected, PoolMath.Weight(strength));

    // ---- ShouldUnlock: non-empty AND mean strength >= 3.5 ----

    [Fact]
    public void ShouldUnlock_false_when_pool_empty()
        => Assert.False(PoolMath.ShouldUnlock([]));

    [Fact]
    public void ShouldUnlock_true_at_threshold()
        => Assert.True(PoolMath.ShouldUnlock([3, 4])); // mean 3.5

    [Fact]
    public void ShouldUnlock_false_just_below_threshold()
        => Assert.False(PoolMath.ShouldUnlock([3, 3, 4])); // mean 3.33

    [Fact]
    public void ShouldUnlock_true_well_above_threshold()
        => Assert.True(PoolMath.ShouldUnlock([5, 5, 4]));

    [Fact]
    public void ShouldUnlock_clamps_out_of_band_strengths()
        => Assert.True(PoolMath.ShouldUnlock([9, 9])); // clamps to 5,5 -> mean 5 -> unlock

    // ---- WeightedPickIndex: deterministic, weakness-weighted, in-range ----

    [Fact]
    public void WeightedPick_empty_pool_returns_minus_one()
        => Assert.Equal(-1, PoolMath.WeightedPickIndex([], 12345));

    [Fact]
    public void WeightedPick_is_deterministic_for_same_seed()
    {
        var pool = new[] { 0, 2, 5, 1 };
        Assert.Equal(
            PoolMath.WeightedPickIndex(pool, 987654),
            PoolMath.WeightedPickIndex(pool, 987654));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    [InlineData(-1)]           // negative seeds fold to a valid, non-negative target
    [InlineData(int.MinValue)] // most-negative int must not throw or go out of range
    public void WeightedPick_always_in_range(int seed)
    {
        var pool = new[] { 0, 3, 5 };
        var index = PoolMath.WeightedPickIndex(pool, seed);
        Assert.InRange(index, 0, pool.Length - 1);
    }

    [Fact]
    public void WeightedPick_seed_zero_hits_first_slot()
    {
        // target = 0 lands in the first weight band regardless of strengths.
        Assert.Equal(0, PoolMath.WeightedPickIndex([5, 0, 0], 0));
    }

    [Fact]
    public void WeightedPick_walks_cumulative_weight_bands()
    {
        // Strengths [5,5,0] -> weights [1,1,6], total 8, cumulative bounds [1,2,8).
        // seed 0 -> band 0; seed 1 -> band 1; seed 2 -> band 2 (the weak word).
        var pool = new[] { 5, 5, 0 };
        Assert.Equal(0, PoolMath.WeightedPickIndex(pool, 0));
        Assert.Equal(1, PoolMath.WeightedPickIndex(pool, 1));
        Assert.Equal(2, PoolMath.WeightedPickIndex(pool, 2));
        Assert.Equal(2, PoolMath.WeightedPickIndex(pool, 7)); // still inside the weak word's wide band
    }

    [Fact]
    public void WeightedPick_weak_word_dominates_the_distribution()
    {
        // One weak (strength 0, weight 6) among mastered (strength 5, weight 1) words.
        // Across the whole seed space the weak word should be picked far more often — this is
        // the "weak words dominate sampling" behaviour from A4.
        var pool = new[] { 5, 5, 0, 5, 5 }; // weights 1,1,6,1,1 -> total 10
        int weakPicks = 0;
        for (int seed = 0; seed < 10; seed++)
            if (PoolMath.WeightedPickIndex(pool, seed) == 2)
                weakPicks++;

        Assert.Equal(6, weakPicks); // exactly its weight share of the 10-wide seed space
    }
}
