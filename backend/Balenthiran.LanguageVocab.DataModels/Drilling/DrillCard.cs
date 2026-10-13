using Balenthiran.LanguageVocab.Abstractions.Drilling;
using Balenthiran.LanguageVocab.Abstractions.Enums;

namespace Balenthiran.LanguageVocab.DataModels.Drilling;

/// <inheritdoc cref="IDrillCard"/>
public record DrillCard(
    Guid VocabItemId,
    DrillDirection Direction,
    string? Hanzi,
    string? Pinyin,
    IReadOnlyList<string>? Glosses) : IDrillCard;
