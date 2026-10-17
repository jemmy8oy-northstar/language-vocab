using Balenthiran.LanguageVocab.Abstractions.Stats;

namespace Balenthiran.LanguageVocab.DataModels.Stats;

/// <inheritdoc cref="IDailyAccuracy"/>
public record DailyAccuracy(
    string Date,
    int Answered,
    int Correct) : IDailyAccuracy;
