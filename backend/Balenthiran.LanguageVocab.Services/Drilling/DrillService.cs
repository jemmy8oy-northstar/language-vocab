using Balenthiran.LanguageVocab.Abstractions.Drilling;
using Balenthiran.LanguageVocab.Abstractions.Enums;
using Balenthiran.LanguageVocab.Abstractions.Services;
using Balenthiran.LanguageVocab.Database;
using Balenthiran.LanguageVocab.DataModels.Drilling;
using Balenthiran.LanguageVocab.EntityModels;
using Microsoft.EntityFrameworkCore;

namespace Balenthiran.LanguageVocab.Services.Drilling;

/// <summary>
/// Composes the pool, the grader and the strength rule into the drill loop. Selection and
/// unlocking stay in <see cref="IPoolService"/>, tone grading in <see cref="IPinyinGrader"/>,
/// and the strength arithmetic in <see cref="IPoolMath"/>; this type reads and writes the rows
/// that join them.
/// </summary>
public class DrillService(AppDbContext db, IPoolService pool, IPoolMath math, IPinyinGrader grader) : IDrillService
{
    public async Task<IDrillCard?> NextAsync(string userId, DrillDirection? direction, CancellationToken ct = default)
    {
        await pool.EnsureBootstrappedAsync(userId, ct);

        // A fresh seed per draw (kit#118): the seed exists so the sampler is unit-testable,
        // not as a client contract, so every draw is independent.
        var id = await pool.SelectNextAsync(userId, Random.Shared.Next(), ct);
        if (id is null)
            return null;

        var item = await db.VocabItems.SingleAsync(v => v.Id == id, ct);
        var dir = direction ?? (Random.Shared.Next(2) == 0 ? DrillDirection.ZhToEn : DrillDirection.EnToZh);

        return dir == DrillDirection.ZhToEn
            ? new DrillCard(item.Id, dir, item.Hanzi, item.Pinyin, null)
            : new DrillCard(item.Id, dir, null, null, item.Glosses);
    }

    public async Task<IReadOnlyList<ILearnCard>> LearnAsync(string userId, int count, CancellationToken ct = default)
    {
        await pool.EnsureBootstrappedAsync(userId, ct);

        // "New" is least-seen in the active pool: a word you have never answered comes first, and
        // rank breaks ties so the commonest words are taught first. Nothing outside the pool, so
        // learning never runs ahead of unlocking.
        return await db.UserWordStates
            .Where(s => s.UserId == userId && s.VocabItem != null)
            .OrderBy(s => s.TimesSeen)
            .ThenBy(s => s.VocabItem!.FrequencyRank)
            .Take(count)
            .Select(s => new LearnCard(s.VocabItemId, s.VocabItem!.Hanzi, s.VocabItem.Pinyin, s.VocabItem.Glosses))
            .ToListAsync(ct);
    }

    public async Task<IDrillOutcome?> AnswerAsync(
        string userId, Guid vocabItemId, DrillDirection direction, string answer, CancellationToken ct = default)
    {
        var state = await db.UserWordStates
            .Include(s => s.VocabItem)
            .SingleOrDefaultAsync(s => s.UserId == userId && s.VocabItemId == vocabItemId, ct);
        if (state?.VocabItem is not { } item)
            return null;

        var verdict = direction == DrillDirection.ZhToEn
            ? GradeEnglish(item.Glosses, answer)
            : grader.Grade(item.Pinyin, answer).Verdict;

        state.Strength = math.NextStrength(state.Strength, verdict);
        state.TimesSeen++;
        if (verdict == AnswerVerdict.Correct)
            state.TimesCorrect++;
        state.LastSeen = DateTime.UtcNow;

        db.AnswerLogs.Add(new AnswerLogEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            VocabItemId = vocabItemId,
            Direction = direction,
            Given = answer,
            Verdict = verdict,
            At = state.LastSeen.Value,
        });
        await db.SaveChangesAsync(ct);

        var unlocked = await pool.TryUnlockAsync(userId, ct);
        return new DrillOutcome(verdict, item.Hanzi, item.Pinyin, item.Glosses, state.Strength, unlocked.Count);
    }

    /// <summary>
    /// An English answer is right if it matches ANY accepted gloss once both are trimmed,
    /// case-folded and have their inner whitespace collapsed (A7). There is no "almost" in
    /// this direction: soft grading is about tones, and English has none.
    /// </summary>
    private static AnswerVerdict GradeEnglish(IEnumerable<string> glosses, string answer)
    {
        var given = NormaliseEnglish(answer);
        return given.Length > 0 && glosses.Any(g => NormaliseEnglish(g) == given)
            ? AnswerVerdict.Correct
            : AnswerVerdict.Wrong;
    }

    private static string NormaliseEnglish(string text)
        => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
}
