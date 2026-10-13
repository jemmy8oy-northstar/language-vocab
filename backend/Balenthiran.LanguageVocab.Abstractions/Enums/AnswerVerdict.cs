namespace Balenthiran.LanguageVocab.Abstractions.Enums;

/// <summary>
/// Outcome of grading a typed answer. <see cref="Almost"/> = right syllables but
/// wrong/missing tones (soft tone grading, design assumption A2).
/// </summary>
public enum AnswerVerdict
{
    Wrong = 0,
    Almost = 1,
    Correct = 2,
}
