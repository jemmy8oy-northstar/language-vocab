namespace Balenthiran.LanguageVocab.EntityModels;

/// <summary>
/// A user's mastery state for one vocabulary item. The presence of a row means
/// the word is in the user's <em>active pool</em>; <see cref="Strength"/> drives
/// weakness-weighted sampling (design assumption A4).
/// </summary>
public class UserWordStateEntity
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public Guid VocabItemId { get; set; }

    public VocabItemEntity? VocabItem { get; set; }

    /// <summary>Mastery strength, 0–5. +1 on full credit, −1 on miss, ±0 on "almost".</summary>
    public int Strength { get; set; }

    public DateTime? LastSeen { get; set; }

    public int TimesSeen { get; set; }

    public int TimesCorrect { get; set; }
}
