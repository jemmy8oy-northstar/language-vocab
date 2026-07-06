using Balenthiran.LanguageVocab.Abstractions.Grading;

namespace Balenthiran.LanguageVocab.Abstractions.Services;

/// <summary>
/// Grades a typed pinyin answer against a canonical (tone-marked) pinyin string,
/// accepting tone marks (nǐ hǎo), tone numbers (ni3 hao3) or toneless (ni hao)
/// input, and grading tones softly.
/// </summary>
public interface IPinyinGrader
{
    PinyinGradeResult Grade(string canonicalPinyin, string userInput);
}
