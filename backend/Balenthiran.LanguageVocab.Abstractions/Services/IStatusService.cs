using Balenthiran.LanguageVocab.Abstractions.DataModels;
using Balenthiran.LanguageVocab.Abstractions.DomainModels;

namespace Balenthiran.LanguageVocab.Abstractions.Services;

public interface IStatusService
{
    Task<IDomainStatus> GetSystemStatusAsync();
}
