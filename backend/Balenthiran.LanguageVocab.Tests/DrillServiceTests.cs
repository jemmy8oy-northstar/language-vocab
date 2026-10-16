using Balenthiran.LanguageVocab.Abstractions.Enums;
using Balenthiran.LanguageVocab.Database;
using Balenthiran.LanguageVocab.EntityModels;
using Balenthiran.LanguageVocab.Services.Drilling;
using Balenthiran.LanguageVocab.Services.Grading;
using Balenthiran.LanguageVocab.Services.Pooling;
using Microsoft.EntityFrameworkCore;

namespace Balenthiran.LanguageVocab.Tests;

/// <summary>
/// The drill loop against an in-memory database: a card hides its own answer, an answer is
/// graded the right way for its direction, and its consequences (strength, counters, the log,
/// unlocking) land. The pool and grading rules themselves are covered by their own suites.
/// </summary>
public class DrillServiceTests
{
    private const string User = "user-1";

    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"drill-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static DrillService NewService(AppDbContext db)
    {
        var math = new PoolMath();
        return new DrillService(db, new PoolService(db, math), math, new PinyinGrader());
    }

    /// <summary>Adds <paramref name="count"/> zh words, ranks 1..count; the first is 你好.</summary>
    private static List<VocabItemEntity> SeedVocab(AppDbContext db, int count)
    {
        var items = Enumerable.Range(1, count).Select(rank => new VocabItemEntity
        {
            Id = Guid.NewGuid(),
            Hanzi = rank == 1 ? "你好" : $"字{rank}",
            Pinyin = rank == 1 ? "nǐ hǎo" : "zì",
            PinyinNormalised = rank == 1 ? "nihao" : "zi",
            Glosses = rank == 1 ? ["hello", "hi"] : [$"word {rank}"],
            Level = 1,
            FrequencyRank = rank,
        }).ToList();
        db.VocabItems.AddRange(items);
        db.SaveChanges();
        return items;
    }

    /// <summary>Bootstraps the pool so 你好 can be answered.</summary>
    private static async Task<(DrillService Svc, VocabItemEntity NiHao)> Ready(AppDbContext db, int count = 20)
    {
        var vocab = SeedVocab(db, count);
        var svc = NewService(db);
        await svc.NextAsync(User, null);
        return (svc, vocab[0]);
    }

    [Fact]
    public async Task Next_bootstraps_the_pool_and_draws_from_it()
    {
        using var db = NewDb();
        SeedVocab(db, 20);
        var svc = NewService(db);

        var card = await svc.NextAsync(User, null);

        Assert.NotNull(card);
        Assert.Equal(10, await db.UserWordStates.CountAsync(s => s.UserId == User));
        Assert.True(await db.UserWordStates.AnyAsync(s => s.VocabItemId == card.VocabItemId));
    }

    [Fact]
    public async Task Next_returns_null_with_no_vocabulary()
    {
        using var db = NewDb();

        Assert.Null(await NewService(db).NextAsync(User, null));
    }

    [Fact]
    public async Task ZhToEn_card_shows_the_word_and_hides_the_meaning()
    {
        using var db = NewDb();
        SeedVocab(db, 1);

        var card = await NewService(db).NextAsync(User, DrillDirection.ZhToEn);

        Assert.NotNull(card);
        Assert.Equal(DrillDirection.ZhToEn, card.Direction);
        Assert.Equal("你好", card.Hanzi);
        Assert.Equal("nǐ hǎo", card.Pinyin);
        Assert.Null(card.Glosses);
    }

    [Fact]
    public async Task EnToZh_card_shows_the_meaning_and_hides_the_word()
    {
        using var db = NewDb();
        SeedVocab(db, 1);

        var card = await NewService(db).NextAsync(User, DrillDirection.EnToZh);

        Assert.NotNull(card);
        Assert.Equal(DrillDirection.EnToZh, card.Direction);
        Assert.Equal(["hello", "hi"], card.Glosses);
        Assert.Null(card.Hanzi);
        Assert.Null(card.Pinyin);
    }

    [Theory]
    [InlineData("hello", AnswerVerdict.Correct)]
    [InlineData("  HI ", AnswerVerdict.Correct)]
    [InlineData("hello there", AnswerVerdict.Wrong)]
    [InlineData("", AnswerVerdict.Wrong)]
    public async Task ZhToEn_answer_matches_any_gloss(string answer, AnswerVerdict expected)
    {
        using var db = NewDb();
        var (svc, niHao) = await Ready(db);

        var outcome = await svc.AnswerAsync(User, niHao.Id, DrillDirection.ZhToEn, answer);

        Assert.Equal(expected, outcome!.Verdict);
    }

    [Fact]
    public async Task English_gloss_inner_whitespace_is_collapsed()
    {
        using var db = NewDb();
        var vocab = SeedVocab(db, 1);
        vocab[0].Glosses = ["to be"];
        db.SaveChanges();
        var svc = NewService(db);
        await svc.NextAsync(User, null);

        var outcome = await svc.AnswerAsync(User, vocab[0].Id, DrillDirection.ZhToEn, "To   be");

        Assert.Equal(AnswerVerdict.Correct, outcome!.Verdict);
    }

    [Fact]
    public async Task A_blank_answer_never_matches_a_blank_gloss()
    {
        using var db = NewDb();
        var vocab = SeedVocab(db, 1);
        vocab[0].Glosses = ["hello", " "];
        db.SaveChanges();
        var svc = NewService(db);
        await svc.NextAsync(User, null);

        var outcome = await svc.AnswerAsync(User, vocab[0].Id, DrillDirection.ZhToEn, "");

        Assert.Equal(AnswerVerdict.Wrong, outcome!.Verdict);
    }

    [Theory]
    [InlineData("ni3 hao3", AnswerVerdict.Correct)]
    [InlineData("ni hao", AnswerVerdict.Almost)]
    [InlineData("xie4 xie4", AnswerVerdict.Wrong)]
    public async Task EnToZh_answer_is_graded_through_the_pinyin_grader(string answer, AnswerVerdict expected)
    {
        using var db = NewDb();
        var (svc, niHao) = await Ready(db);

        var outcome = await svc.AnswerAsync(User, niHao.Id, DrillDirection.EnToZh, answer);

        Assert.Equal(expected, outcome!.Verdict);
    }

    [Fact]
    public async Task Outcome_reveals_the_whole_word()
    {
        using var db = NewDb();
        var (svc, niHao) = await Ready(db);

        var outcome = await svc.AnswerAsync(User, niHao.Id, DrillDirection.EnToZh, "wrong");

        Assert.Equal("你好", outcome!.Hanzi);
        Assert.Equal("nǐ hǎo", outcome.Pinyin);
        Assert.Equal(["hello", "hi"], outcome.Glosses);
    }

    [Theory]
    [InlineData("ni3 hao3", 3, 1)]
    [InlineData("ni hao", 2, 0)]
    [InlineData("nope", 1, 0)]
    public async Task Answer_moves_strength_and_counts_the_attempt(string answer, int strength, int timesCorrect)
    {
        using var db = NewDb();
        var (svc, niHao) = await Ready(db);
        var state = await db.UserWordStates.SingleAsync(s => s.VocabItemId == niHao.Id);
        state.Strength = 2;
        await db.SaveChangesAsync();

        var outcome = await svc.AnswerAsync(User, niHao.Id, DrillDirection.EnToZh, answer);

        Assert.Equal(strength, outcome!.Strength);
        Assert.Equal(strength, state.Strength);
        Assert.Equal(1, state.TimesSeen);
        Assert.Equal(timesCorrect, state.TimesCorrect);
        Assert.NotNull(state.LastSeen);
    }

    [Fact]
    public async Task Answer_is_logged_exactly_as_typed()
    {
        using var db = NewDb();
        var (svc, niHao) = await Ready(db);

        await svc.AnswerAsync(User, niHao.Id, DrillDirection.EnToZh, "  NI3 hao3 ");

        var log = await db.AnswerLogs.SingleAsync();
        Assert.Equal(User, log.UserId);
        Assert.Equal(niHao.Id, log.VocabItemId);
        Assert.Equal(DrillDirection.EnToZh, log.Direction);
        Assert.Equal("  NI3 hao3 ", log.Given);
        Assert.Equal(AnswerVerdict.Correct, log.Verdict);
    }

    [Fact]
    public async Task Answer_for_a_word_outside_the_pool_writes_nothing()
    {
        using var db = NewDb();
        var (svc, _) = await Ready(db);
        var outsider = await db.VocabItems.SingleAsync(v => v.FrequencyRank == 20);

        var outcome = await svc.AnswerAsync(User, outsider.Id, DrillDirection.ZhToEn, "word 20");

        Assert.Null(outcome);
        Assert.Empty(db.AnswerLogs);
        Assert.False(await db.UserWordStates.AnyAsync(s => s.VocabItemId == outsider.Id));
    }

    [Fact]
    public async Task Answer_for_another_users_word_returns_null()
    {
        using var db = NewDb();
        var (svc, niHao) = await Ready(db);

        Assert.Null(await svc.AnswerAsync("someone-else", niHao.Id, DrillDirection.ZhToEn, "hello"));
    }

    [Fact]
    public async Task The_answer_that_masters_the_pool_unlocks_the_next_batch()
    {
        using var db = NewDb();
        var (svc, niHao) = await Ready(db);
        // Pool total 34 (mean 3.4, under the 3.5 threshold); 你好 going 1 -> 2 makes it 35.
        var others = await db.UserWordStates.Where(s => s.VocabItemId != niHao.Id).ToListAsync();
        for (var i = 0; i < others.Count; i++)
            others[i].Strength = i < 6 ? 4 : 3;
        (await db.UserWordStates.SingleAsync(s => s.VocabItemId == niHao.Id)).Strength = 1;
        await db.SaveChangesAsync();

        var outcome = await svc.AnswerAsync(User, niHao.Id, DrillDirection.ZhToEn, "hello");

        Assert.Equal(5, outcome!.Unlocked);
        Assert.Equal(15, await db.UserWordStates.CountAsync());
    }

    [Fact]
    public async Task An_answer_below_the_threshold_unlocks_nothing()
    {
        using var db = NewDb();
        var (svc, niHao) = await Ready(db);

        var outcome = await svc.AnswerAsync(User, niHao.Id, DrillDirection.ZhToEn, "hello");

        Assert.Equal(0, outcome!.Unlocked);
        Assert.Equal(10, await db.UserWordStates.CountAsync());
    }

    // ── Learn mode (BEH-LEARN-1, kit#288) ───────────────────────────────────

    [Fact]
    public async Task Learn_bootstraps_and_shows_five_words_whole_by_rank()
    {
        using var db = NewDb();
        SeedVocab(db, 20);

        var round = await NewService(db).LearnAsync(User, 5);

        Assert.Equal(5, round.Count);
        // Nothing hidden: Learn mode teaches before it asks.
        Assert.Equal("你好", round[0].Hanzi);
        Assert.Equal("nǐ hǎo", round[0].Pinyin);
        Assert.Equal(["hello", "hi"], round[0].Glosses);
        Assert.Equal(["你好", "字2", "字3", "字4", "字5"], round.Select(c => c.Hanzi));
    }

    [Fact]
    public async Task Learn_puts_words_already_answered_after_unseen_ones()
    {
        using var db = NewDb();
        var (svc, niHao) = await Ready(db);
        await svc.AnswerAsync(User, niHao.Id, DrillDirection.ZhToEn, "hello");

        var round = await svc.LearnAsync(User, 5);

        Assert.DoesNotContain(round, c => c.VocabItemId == niHao.Id);
        Assert.Equal(["字2", "字3", "字4", "字5", "字6"], round.Select(c => c.Hanzi));
    }

    [Fact]
    public async Task Learn_never_reaches_outside_the_pool_and_writes_nothing()
    {
        using var db = NewDb();
        SeedVocab(db, 20);
        var svc = NewService(db);

        var round = await svc.LearnAsync(User, 20);

        // The pool bootstraps to ten; the other ten words stay locked.
        Assert.Equal(10, round.Count);
        Assert.Equal(0, await db.AnswerLogs.CountAsync());
        Assert.All(await db.UserWordStates.ToListAsync(), s => Assert.Equal(0, s.TimesSeen));
    }
}
