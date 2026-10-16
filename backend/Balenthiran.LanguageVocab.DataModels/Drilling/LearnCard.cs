using Balenthiran.LanguageVocab.Abstractions.Drilling;

namespace Balenthiran.LanguageVocab.DataModels.Drilling;

/// <inheritdoc cref="ILearnCard"/>
public record LearnCard(
    Guid VocabItemId,
    string Hanzi,
    string Pinyin,
    IReadOnlyList<string> Glosses) : ILearnCard;
