using Balenthiran.LanguageVocab.Abstractions.Stats;

namespace Balenthiran.LanguageVocab.Abstractions.Services;

/// <summary>Reads a user's progress back out of the pool and the answer log (BEH-STATS-1). Read-only.</summary>
public interface IStatsService
{
    Task<IStatsReport> GetStatsAsync(string userId, CancellationToken ct = default);
}
