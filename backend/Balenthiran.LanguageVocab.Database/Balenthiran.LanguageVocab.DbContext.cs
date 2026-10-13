using Balenthiran.LanguageVocab.EntityModels;
using Microsoft.EntityFrameworkCore;

namespace Balenthiran.LanguageVocab.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<VocabItemEntity> VocabItems => Set<VocabItemEntity>();
    public DbSet<UserWordStateEntity> UserWordStates => Set<UserWordStateEntity>();
    public DbSet<AnswerLogEntity> AnswerLogs => Set<AnswerLogEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<VocabItemEntity>(e =>
        {
            e.HasKey(v => v.Id);
            e.Property(v => v.Language).IsRequired().HasMaxLength(8);
            e.Property(v => v.Hanzi).IsRequired();
            e.Property(v => v.Pinyin).IsRequired();
            e.Property(v => v.PinyinNormalised).IsRequired();
            // One vocab item per (language, hanzi) — the key the seed loader upserts on.
            e.HasIndex(v => new { v.Language, v.Hanzi }).IsUnique();
            // Pool unlock walks items in frequency order within a language.
            e.HasIndex(v => new { v.Language, v.FrequencyRank });
        });

        modelBuilder.Entity<UserWordStateEntity>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.UserId).IsRequired();
            e.HasOne(s => s.VocabItem)
                .WithMany()
                .HasForeignKey(s => s.VocabItemId)
                .OnDelete(DeleteBehavior.Cascade);
            // A user has at most one state row per vocab item (their active pool).
            e.HasIndex(s => new { s.UserId, s.VocabItemId }).IsUnique();
        });

        modelBuilder.Entity<AnswerLogEntity>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.UserId).IsRequired();
            e.HasOne(a => a.VocabItem)
                .WithMany()
                .HasForeignKey(a => a.VocabItemId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(a => new { a.UserId, a.VocabItemId });
        });
    }
}
