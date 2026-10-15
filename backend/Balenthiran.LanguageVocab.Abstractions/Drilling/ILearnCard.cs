namespace Balenthiran.LanguageVocab.Abstractions.Drilling;

/// <summary>
/// One word to learn (BEH-LEARN-1): everything shown at once — hanzi, pinyin and English —
/// because Learn mode teaches before it asks. Unlike <see cref="IDrillCard"/>, nothing is hidden.
/// </summary>
public interface ILearnCard
{
    Guid VocabItemId { get; }
    string Hanzi { get; }
    string Pinyin { get; }
    IReadOnlyList<string> Glosses { get; }
}
