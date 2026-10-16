using Balenthiran.LanguageVocab.Abstractions.Packs;

namespace Balenthiran.LanguageVocab.DataModels.Packs;

/// <inheritdoc cref="IPack"/>
public record Pack(
    string Id,
    string Title,
    IReadOnlyList<string> Hanzi) : IPack;
