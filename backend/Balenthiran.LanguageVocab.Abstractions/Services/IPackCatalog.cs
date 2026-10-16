using Balenthiran.LanguageVocab.Abstractions.Packs;

namespace Balenthiran.LanguageVocab.Abstractions.Services;

/// <summary>The vocab packs this build ships (BEH-PACK-1), read once from the embedded pack files.</summary>
public interface IPackCatalog
{
    IReadOnlyList<IPack> All { get; }

    /// <summary>The pack with this id, or <c>null</c> if there is none.</summary>
    IPack? Find(string id);
}
