using Balenthiran.LanguageVocab.Abstractions.DomainModels;
using Balenthiran.LanguageVocab.DataModels.Models;

namespace Balenthiran.LanguageVocab.DomainModels.Models;

public class DomainStatus : Status, IDomainStatus
{
    public string GetFriendlyStatus()
    {
        return $"System is running version {Version} (Updated: {LastUpdated:g})";
    }
}
