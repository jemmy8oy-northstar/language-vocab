namespace Balenthiran.LanguageVocab.Abstractions.DomainModels;

using Balenthiran.LanguageVocab.Abstractions.DataModels;

public interface IDomainStatus : IStatus
{
    string GetFriendlyStatus();
}
