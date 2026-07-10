using Balenthiran.LanguageVocab.Abstractions.Enums;
using Balenthiran.LanguageVocab.Abstractions.Grading;

namespace Balenthiran.LanguageVocab.DataModels.Grading;

/// <inheritdoc cref="IPinyinGradeResult"/>
public record PinyinGradeResult(
    AnswerVerdict Verdict,
    string CanonicalPinyin,
    bool SyllablesCorrect,
    bool TonesCorrect,
    bool TonesProvided) : IPinyinGradeResult;
