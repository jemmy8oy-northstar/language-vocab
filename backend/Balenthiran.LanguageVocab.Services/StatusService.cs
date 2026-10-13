using Balenthiran.LanguageVocab.Abstractions.DataModels;
using Balenthiran.LanguageVocab.Abstractions.DomainModels;
using Balenthiran.LanguageVocab.Abstractions.Services;
using Balenthiran.LanguageVocab.DomainModels.Models;

namespace Balenthiran.LanguageVocab.Services;

public class StatusService : IStatusService
{
    public Task<IDomainStatus> GetSystemStatusAsync()
    {
        IDomainStatus model = new DomainStatus
        {
            Version = "1.1.0-alpha",
            LastUpdated = DateTime.UtcNow
        };
        
        return Task.FromResult(model);
    }
}
