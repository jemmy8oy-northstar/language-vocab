using Balenthiran.LanguageVocab.Abstractions.Enums;
using Balenthiran.LanguageVocab.Database;
using Balenthiran.LanguageVocab.EntityModels;
using Balenthiran.LanguageVocab.Services.Pooling;
using Balenthiran.LanguageVocab.Services.Stats;
using Microsoft.EntityFrameworkCore;

namespace Balenthiran.LanguageVocab.Tests;

/// <summary>
/// The Stats report (BEH-STATS-1) against an in-memory database: pool size and strengths come
/// from the pool, accuracy from the answer log bucketed by UTC day, and other users and
/// languages never leak in.
/// </summary>
public class StatsServiceTests
{
    private const string User = "user-1";

    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"stats-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static VocabItemEntity AddWord(AppDbContext db, int rank, string language = PoolService.Language)
    {
        var item = new VocabItemEntity
        {
            Id = Guid.NewGuid(),
            Language = language,
            Hanzi = $"{language}-{rank}",
            Pinyin = "x",
            PinyinNormalised = "x",
            Glosses = [$"word {rank}"],
            Level = 1,
            FrequencyRank = rank,
        };
        db.VocabItems.Add(item);
        db.SaveChanges();
        return item;
    }

    private static void AddState(AppDbContext db, VocabItemEntity item, int strength, string user = User)
    {
        db.UserWordStates.Add(new UserWordStateEntity
            { Id = Guid.NewGuid(), UserId = user, VocabItemId = item.Id, Strength = strength });
        db.SaveChanges();
    }

    private static void AddAnswer(
        AppDbContext db, VocabItemEntity item, AnswerVerdict verdict, DateTime at, string user = User)
    {
        db.AnswerLogs.Add(new AnswerLogEntity
        {
            Id = Guid.NewGuid(),
            UserId = user,
            VocabItemId = item.Id,
            Direction = DrillDirection.ZhToEn,
            Given = "x",
            Verdict = verdict,
            At = at,
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task Empty_user_gets_an_empty_report_with_the_total_still_known()
    {
        using var db = NewDb();
        AddWord(db, 1);
        AddWord(db, 2);

        var report = await new StatsService(db).GetStatsAsync(User);

        Assert.Equal(0, report.PoolSize);
        Assert.Equal(2, report.TotalWords);
        Assert.Empty(report.Words);
        Assert.Empty(report.Accuracy);
    }

    [Fact]
    public async Task Pool_size_and_strengths_list_the_pool_weakest_first()
    {
        using var db = NewDb();
        var a = AddWord(db, 1);
        var b = AddWord(db, 2);
        AddWord(db, 3);
        AddState(db, a, 4);
        AddState(db, b, 1);

        var report = await new StatsService(db).GetStatsAsync(User);

        Assert.Equal(2, report.PoolSize);
        Assert.Equal(3, report.TotalWords);
        Assert.Equal([1, 4], report.Words.Select(w => w.Strength));
        Assert.Equal(b.Hanzi, report.Words[0].Hanzi);
        Assert.Equal(["word 2"], report.Words[0].Glosses);
    }

    [Fact]
    public async Task Accuracy_buckets_by_utc_day_oldest_first_and_counts_only_Correct()
    {
        using var db = NewDb();
        var w = AddWord(db, 1);
        AddAnswer(db, w, AnswerVerdict.Correct, new DateTime(2026, 10, 16, 23, 59, 0, DateTimeKind.Utc));
        AddAnswer(db, w, AnswerVerdict.Wrong, new DateTime(2026, 10, 15, 9, 0, 0, DateTimeKind.Utc));
        AddAnswer(db, w, AnswerVerdict.Almost, new DateTime(2026, 10, 16, 0, 1, 0, DateTimeKind.Utc));
        AddAnswer(db, w, AnswerVerdict.Correct, new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc));

        var report = await new StatsService(db).GetStatsAsync(User);

        Assert.Equal(["2026-10-15", "2026-10-16"], report.Accuracy.Select(d => d.Date));
        Assert.Equal((1, 0), (report.Accuracy[0].Answered, report.Accuracy[0].Correct));
        Assert.Equal((3, 2), (report.Accuracy[1].Answered, report.Accuracy[1].Correct));
    }

    [Fact]
    public async Task Other_users_and_other_languages_are_left_out()
    {
        using var db = NewDb();
        var mine = AddWord(db, 1);
        var french = AddWord(db, 1, "fr");
        AddState(db, mine, 2);
        AddState(db, french, 5);
        AddState(db, mine, 3, "someone-else");
        var at = new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);
        AddAnswer(db, mine, AnswerVerdict.Correct, at);
        AddAnswer(db, french, AnswerVerdict.Correct, at);
        AddAnswer(db, mine, AnswerVerdict.Correct, at, "someone-else");

        var report = await new StatsService(db).GetStatsAsync(User);

        Assert.Equal(1, report.PoolSize);
        Assert.Equal(1, report.TotalWords);
        Assert.Equal(2, report.Words.Single().Strength);
        Assert.Equal(1, report.Accuracy.Single().Answered);
    }
}
