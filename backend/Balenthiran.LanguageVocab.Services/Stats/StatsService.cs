using Balenthiran.LanguageVocab.Abstractions.Enums;
using Balenthiran.LanguageVocab.Abstractions.Services;
using Balenthiran.LanguageVocab.Abstractions.Stats;
using Balenthiran.LanguageVocab.Database;
using Balenthiran.LanguageVocab.DataModels.Stats;
using Balenthiran.LanguageVocab.Services.Pooling;
using Microsoft.EntityFrameworkCore;

namespace Balenthiran.LanguageVocab.Services.Stats;

/// <summary>
/// Reads the pool and the answer log back as a report (BEH-STATS-1). It writes nothing, and, like
/// every pool operation (BEH-LANG-1), ignores rows in any language but <see cref="PoolService.Language"/>.
/// </summary>
public class StatsService(AppDbContext db) : IStatsService
{
    public async Task<IStatsReport> GetStatsAsync(string userId, CancellationToken ct = default)
    {
        var rows = await db.UserWordStates
            .Where(s => s.UserId == userId && s.VocabItem!.Language == PoolService.Language)
            .OrderBy(s => s.Strength).ThenBy(s => s.VocabItem!.FrequencyRank)
            .Select(s => new { s.VocabItem!.Hanzi, s.VocabItem.Pinyin, s.VocabItem.Glosses, s.Strength })
            .ToListAsync(ct);
        var words = rows
            .Select(r => (IWordStrength)new WordStrength(r.Hanzi, r.Pinyin, r.Glosses, r.Strength))
            .ToList();

        var totalWords = await db.VocabItems.CountAsync(v => v.Language == PoolService.Language, ct);

        var answers = await db.AnswerLogs
            .Where(a => a.UserId == userId && a.VocabItem!.Language == PoolService.Language)
            .Select(a => new { a.At, a.Verdict })
            .ToListAsync(ct);

        // "Correct" is AnswerVerdict.Correct alone: the same test DrillService uses for
        // TimesCorrect. "Almost" (right syllables, wrong tones) neither counts as correct here
        // nor moves strength, so it counts as answered but not correct. Days are UTC, as At is.
        var accuracy = answers
            .GroupBy(a => DateOnly.FromDateTime(a.At))
            .OrderBy(g => g.Key)
            .Select(g => (IDailyAccuracy)new DailyAccuracy(
                g.Key.ToString("yyyy-MM-dd"), g.Count(), g.Count(a => a.Verdict == AnswerVerdict.Correct)))
            .ToList();

        return new StatsReport(words.Count, totalWords, words, accuracy);
    }
}
