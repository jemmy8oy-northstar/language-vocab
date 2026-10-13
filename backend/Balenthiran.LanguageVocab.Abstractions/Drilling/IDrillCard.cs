using Balenthiran.LanguageVocab.Abstractions.Enums;

namespace Balenthiran.LanguageVocab.Abstractions.Drilling;

/// <summary>
/// One card to answer. It carries only what the prompt shows, so the answer never reaches the
/// client before it is submitted: Zh→En shows the hanzi and pinyin, En→Zh shows the English
/// glosses. The fields the direction hides are <c>null</c>.
/// </summary>
public interface IDrillCard
{
    Guid VocabItemId { get; }
    DrillDirection Direction { get; }
    string? Hanzi { get; }
    string? Pinyin { get; }
    IReadOnlyList<string>? Glosses { get; }
}
