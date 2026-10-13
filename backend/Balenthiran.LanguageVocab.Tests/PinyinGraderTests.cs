using Balenthiran.LanguageVocab.Abstractions.Enums;
using Balenthiran.LanguageVocab.Services.Grading;

namespace Balenthiran.LanguageVocab.Tests;

/// <summary>
/// Behaviour of <see cref="PinyinGrader"/> — the correctness-critical bit. Tones
/// are graded softly (A2): right syllables + wrong/missing tones ⇒ Almost.
/// Input may be tone-marked (nǐ), tone-numbered (ni3) or toneless (ni); ü and v
/// are unified; separators (space/apostrophe/hyphen) are ignored (A7).
/// </summary>
public class PinyinGraderTests
{
    private readonly PinyinGrader _grader = new();

    [Theory]
    // exact tone-marked match
    [InlineData("nǐ hǎo", "nǐ hǎo")]
    // tone numbers, with and without the separating space
    [InlineData("nǐ hǎo", "ni3 hao3")]
    [InlineData("nǐ hǎo", "ni3hao3")]
    // case-insensitive
    [InlineData("nǐ hǎo", "NI3 HAO3")]
    // single syllable
    [InlineData("wǒ", "wo3")]
    // ü/v unification, both input spellings, with tone
    [InlineData("lǜ", "lv4")]
    [InlineData("lǜ", "lü4")]
    [InlineData("nǚ", "nv3")]
    // multi-syllable with mixed tones
    [InlineData("Běijīng", "bei3 jing1")]
    // neutral tone in canonical is ungradeable → any tone the user adds is fine
    [InlineData("ma", "ma")]
    [InlineData("ma", "ma1")]
    // trailing neutral syllable: only the graded tone must match
    [InlineData("xiè xie", "xie4 xie")]
    [InlineData("xiè xie", "xie4 xie5")]
    public void Grade_correct(string canonical, string input)
        => Assert.Equal(AnswerVerdict.Correct, _grader.Grade(canonical, input).Verdict);

    [Theory]
    // right syllables, no tones supplied
    [InlineData("nǐ hǎo", "ni hao")]
    [InlineData("wǒ", "wo")]
    [InlineData("Běijīng", "beijing")]
    // right syllables, a wrong tone
    [InlineData("nǐ hǎo", "ni2 hao3")]
    [InlineData("Běijīng", "bei3 jing2")]
    public void Grade_almost(string canonical, string input)
        => Assert.Equal(AnswerVerdict.Almost, _grader.Grade(canonical, input).Verdict);

    [Theory]
    // wrong letters ⇒ wrong regardless of tones
    [InlineData("nǐ hǎo", "ni3 hai3")]
    [InlineData("wǒ", "wa3")]
    // empty / whitespace input
    [InlineData("nǐ hǎo", "")]
    [InlineData("nǐ hǎo", "   ")]
    public void Grade_wrong(string canonical, string input)
        => Assert.Equal(AnswerVerdict.Wrong, _grader.Grade(canonical, input).Verdict);

    [Fact]
    public void Toneless_answer_reports_flags()
    {
        var r = _grader.Grade("nǐ hǎo", "ni hao");
        Assert.True(r.SyllablesCorrect);
        Assert.False(r.TonesProvided);
        Assert.False(r.TonesCorrect);
        Assert.Equal(AnswerVerdict.Almost, r.Verdict);
    }

    [Fact]
    public void Wrong_tone_reports_tones_provided_but_incorrect()
    {
        var r = _grader.Grade("nǐ hǎo", "ni2 hao3");
        Assert.True(r.SyllablesCorrect);
        Assert.True(r.TonesProvided);
        Assert.False(r.TonesCorrect);
    }

    [Fact]
    public void Result_carries_the_canonical_form()
        => Assert.Equal("nǐ hǎo", _grader.Grade("nǐ hǎo", "ni hao").CanonicalPinyin);
}
