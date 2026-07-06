using Balenthiran.LanguageVocab.Abstractions.Enums;

namespace Balenthiran.LanguageVocab.EntityModels;

/// <summary>
/// A single unit of vocabulary to drill. Language-tagged from day one so other
/// languages can be seeded later without a schema change.
/// </summary>
public class VocabItemEntity
{
    public Guid Id { get; set; }

    /// <summary>ISO-639 language code of the target language, e.g. "zh".</summary>
    public string Language { get; set; } = "zh";

    /// <summary>The characters as displayed (never required as input).</summary>
    public string Hanzi { get; set; } = string.Empty;

    /// <summary>Canonical pinyin with tone marks, e.g. "nǐ hǎo".</summary>
    public string Pinyin { get; set; } = string.Empty;

    /// <summary>
    /// Toneless, lowercased, space-stripped pinyin used for fast lookups and as
    /// a stable comparison key. Populated by the seed loader from <see cref="Pinyin"/>.
    /// </summary>
    public string PinyinNormalised { get; set; } = string.Empty;

    /// <summary>Accepted English glosses; an answer matching any one is correct.</summary>
    public List<string> Glosses { get; set; } = [];

    /// <summary>HSK level (1, 2, 3, …).</summary>
    public int Level { get; set; }

    /// <summary>Global ordering by usefulness/frequency; drives pool unlock order.</summary>
    public int FrequencyRank { get; set; }

    public VocabItemType Type { get; set; } = VocabItemType.Word;
}
