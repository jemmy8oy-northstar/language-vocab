using System.Text;
using Balenthiran.LanguageVocab.Abstractions.Enums;
using Balenthiran.LanguageVocab.Abstractions.Grading;
using Balenthiran.LanguageVocab.Abstractions.Services;
using Balenthiran.LanguageVocab.DataModels.Grading;

namespace Balenthiran.LanguageVocab.Services.Grading;

/// <summary>
/// Grades typed pinyin. The comparison is deliberately segmentation-free: an
/// answer is scored on two axes —
/// <list type="bullet">
///   <item>syllables: the toneless letter sequence (spaces/apostrophes ignored,
///     'ü' and 'v' unified) must match the canonical exactly;</item>
///   <item>tones: the ordered sequence of non-neutral tones (1–4) must match.</item>
/// </list>
/// Right syllables + right tones ⇒ Correct; right syllables + wrong/missing tones
/// ⇒ Almost; wrong syllables ⇒ Wrong. Normalisation follows design assumption A7.
/// </summary>
public class PinyinGrader : IPinyinGrader
{
    public IPinyinGradeResult Grade(string canonicalPinyin, string userInput)
    {
        var (canonicalBase, canonicalTones) = Parse(canonicalPinyin);
        var (userBase, userTones) = Parse(userInput);

        var syllablesCorrect = canonicalBase.Length > 0 && canonicalBase == userBase;
        if (!syllablesCorrect)
        {
            return new PinyinGradeResult(
                AnswerVerdict.Wrong, canonicalPinyin, false, false, userTones.Count > 0);
        }

        var tonesProvided = userTones.Count > 0;

        // Canonical has no gradeable (non-neutral) tones — nothing to get wrong.
        if (canonicalTones.Count == 0)
        {
            return new PinyinGradeResult(
                AnswerVerdict.Correct, canonicalPinyin, true, true, tonesProvided);
        }

        if (!tonesProvided)
        {
            return new PinyinGradeResult(
                AnswerVerdict.Almost, canonicalPinyin, true, false, false);
        }

        var tonesCorrect = userTones.SequenceEqual(canonicalTones);
        return new PinyinGradeResult(
            tonesCorrect ? AnswerVerdict.Correct : AnswerVerdict.Almost,
            canonicalPinyin, true, tonesCorrect, true);
    }

    /// <summary>
    /// Reduces any accepted pinyin form to a toneless letter string plus the
    /// ordered list of its non-neutral tones (1–4). Neutral tone (mark-less, or
    /// written 0/5) contributes no tone. 'ü' and 'v' both normalise to 'v'.
    /// </summary>
    internal static (string Base, IReadOnlyList<int> Tones) Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return (string.Empty, []);

        var baseSb = new StringBuilder(input.Length);
        var tones = new List<int>();

        foreach (var raw in input.Trim().ToLowerInvariant())
        {
            switch (raw)
            {
                case ' ':
                case '\'':
                case '’':
                case '-':
                case '·':
                    continue; // separators are ignored
                case 'ü':
                    baseSb.Append('v');
                    continue;
            }

            if (raw is >= '0' and <= '5')
            {
                if (raw is >= '1' and <= '4')
                    tones.Add(raw - '0');
                continue; // 0 and 5 are neutral → no tone
            }

            if (ToneMarks.TryGetValue(raw, out var mark))
            {
                baseSb.Append(mark.Plain);
                tones.Add(mark.Tone);
                continue;
            }

            baseSb.Append(raw);
        }

        return (baseSb.ToString(), tones);
    }

    private readonly record struct ToneMark(char Plain, int Tone);

    private static readonly Dictionary<char, ToneMark> ToneMarks = new()
    {
        ['ā'] = new('a', 1), ['á'] = new('a', 2), ['ǎ'] = new('a', 3), ['à'] = new('a', 4),
        ['ē'] = new('e', 1), ['é'] = new('e', 2), ['ě'] = new('e', 3), ['è'] = new('e', 4),
        ['ī'] = new('i', 1), ['í'] = new('i', 2), ['ǐ'] = new('i', 3), ['ì'] = new('i', 4),
        ['ō'] = new('o', 1), ['ó'] = new('o', 2), ['ǒ'] = new('o', 3), ['ò'] = new('o', 4),
        ['ū'] = new('u', 1), ['ú'] = new('u', 2), ['ǔ'] = new('u', 3), ['ù'] = new('u', 4),
        // ü with tone marks → base letter 'v' to match the ü/v unification above
        ['ǖ'] = new('v', 1), ['ǘ'] = new('v', 2), ['ǚ'] = new('v', 3), ['ǜ'] = new('v', 4),
    };
}
