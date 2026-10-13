using Balenthiran.LanguageVocab.Abstractions.Drilling;
using Balenthiran.LanguageVocab.Abstractions.Enums;

namespace Balenthiran.LanguageVocab.DataModels.Drilling;

/// <inheritdoc cref="IDrillOutcome"/>
public record DrillOutcome(
    AnswerVerdict Verdict,
    string Hanzi,
    string Pinyin,
    IReadOnlyList<string> Glosses,
    int Strength,
    int Unlocked) : IDrillOutcome;
