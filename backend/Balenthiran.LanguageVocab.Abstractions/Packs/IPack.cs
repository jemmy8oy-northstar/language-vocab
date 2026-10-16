namespace Balenthiran.LanguageVocab.Abstractions.Packs;

/// <summary>
/// A themed set of words to learn together (BEH-PACK-1), e.g. the mahjong tile names. A pack
/// names its words by hanzi, so one word can sit in a list and in any number of packs.
/// </summary>
public interface IPack
{
    /// <summary>Stable id used in urls, e.g. "mahjong".</summary>
    string Id { get; }

    string Title { get; }

    /// <summary>Every word in the pack, in the order it teaches them.</summary>
    IReadOnlyList<string> Hanzi { get; }
}
