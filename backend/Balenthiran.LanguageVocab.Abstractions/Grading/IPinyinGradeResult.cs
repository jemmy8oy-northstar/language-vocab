using Balenthiran.LanguageVocab.Abstractions.Enums;

namespace Balenthiran.LanguageVocab.Abstractions.Grading;

/// <summary>
/// Outcome of grading a typed pinyin answer against a canonical form.
/// Tones are graded softly (design assumption A2): correct syllables with
/// wrong or missing tones yield <see cref="AnswerVerdict.Almost"/>.
/// </summary>
public interface IPinyinGradeResult
{
    AnswerVerdict Verdict { get; }
    string CanonicalPinyin { get; }
    bool SyllablesCorrect { get; }
    bool TonesCorrect { get; }
    bool TonesProvided { get; }
}
