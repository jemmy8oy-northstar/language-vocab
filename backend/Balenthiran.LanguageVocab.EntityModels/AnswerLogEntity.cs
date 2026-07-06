using Balenthiran.LanguageVocab.Abstractions.Enums;

namespace Balenthiran.LanguageVocab.EntityModels;

/// <summary>
/// Raw, append-only history of every answer. Kept so a real SRS scheduler can be
/// swapped in later without data loss (design assumption A4).
/// </summary>
public class AnswerLogEntity
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public Guid VocabItemId { get; set; }

    public VocabItemEntity? VocabItem { get; set; }

    public DrillDirection Direction { get; set; }

    /// <summary>Exactly what the user typed, before normalisation.</summary>
    public string Given { get; set; } = string.Empty;

    public AnswerVerdict Verdict { get; set; }

    public DateTime At { get; set; }
}
