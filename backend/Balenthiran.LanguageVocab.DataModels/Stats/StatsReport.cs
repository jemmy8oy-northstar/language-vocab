using Balenthiran.LanguageVocab.Abstractions.Stats;

namespace Balenthiran.LanguageVocab.DataModels.Stats;

/// <inheritdoc cref="IStatsReport"/>
public record StatsReport(
    int PoolSize,
    int TotalWords,
    IReadOnlyList<IWordStrength> Words,
    IReadOnlyList<IDailyAccuracy> Accuracy) : IStatsReport;
