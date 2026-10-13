namespace Balenthiran.LanguageVocab.Abstractions.Enums;

/// <summary>Which way a drill card is presented.</summary>
public enum DrillDirection
{
    /// <summary>Show the Chinese word (hanzi + pinyin); user types the English meaning.</summary>
    ZhToEn = 0,

    /// <summary>Show the English; user types the pinyin.</summary>
    EnToZh = 1,
}
