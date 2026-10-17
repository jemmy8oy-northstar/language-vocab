using Balenthiran.LanguageVocab.Abstractions.Stats;

namespace Balenthiran.LanguageVocab.DataModels.Stats;

/// <inheritdoc cref="IWordStrength"/>
public record WordStrength(
    string Hanzi,
    string Pinyin,
    IReadOnlyList<string> Glosses,
    int Strength) : IWordStrength;
