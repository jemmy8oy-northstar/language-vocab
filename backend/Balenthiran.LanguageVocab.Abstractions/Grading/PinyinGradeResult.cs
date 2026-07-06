using Balenthiran.LanguageVocab.Abstractions.Enums;

namespace Balenthiran.LanguageVocab.Abstractions.Grading;

/// <summary>
/// Outcome of grading a typed pinyin answer against a canonical form.
/// Tones are graded softly (design assumption A2): correct syllables with
/// wrong or missing tones yield <see cref="AnswerVerdict.Almost"/>.
/// </summary>
public record PinyinGradeResult(
    AnswerVerdict Verdict,
    string CanonicalPinyin,
    bool SyllablesCorrect,
    bool TonesCorrect,
    bool TonesProvided);
