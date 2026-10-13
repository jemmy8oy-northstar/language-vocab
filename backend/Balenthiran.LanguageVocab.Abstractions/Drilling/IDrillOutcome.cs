using Balenthiran.LanguageVocab.Abstractions.Enums;

namespace Balenthiran.LanguageVocab.Abstractions.Drilling;

/// <summary>
/// The result of answering a card: the verdict, the whole word (so amber and red feedback can
/// show the correct form, MVP §5), the word's new strength, and how many words the answer
/// unlocked into the pool.
/// </summary>
public interface IDrillOutcome
{
    AnswerVerdict Verdict { get; }
    string Hanzi { get; }
    string Pinyin { get; }
    IReadOnlyList<string> Glosses { get; }
    int Strength { get; }
    int Unlocked { get; }
}
