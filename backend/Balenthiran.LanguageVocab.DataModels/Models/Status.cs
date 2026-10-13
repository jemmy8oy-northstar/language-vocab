using Balenthiran.LanguageVocab.Abstractions.DataModels;

namespace Balenthiran.LanguageVocab.DataModels.Models;

public class Status : IStatus
{
    public string Version { get; set; } = "1.0.0";
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
