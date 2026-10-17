namespace Balenthiran.LanguageVocab.Abstractions.Stats;

/// <summary>
/// What the Stats page shows (BEH-STATS-1): how big the pool is, how strong each word in it is,
/// and how accuracy has moved day by day.
/// </summary>
public interface IStatsReport
{
    /// <summary>Words in the user's active pool.</summary>
    int PoolSize { get; }

    /// <summary>Words the app could ever put in the pool (every word in the drilled language).</summary>
    int TotalWords { get; }

    /// <summary>Every word in the pool, weakest first.</summary>
    IReadOnlyList<IWordStrength> Words { get; }

    /// <summary>One bucket per UTC day that has any answers, oldest first.</summary>
    IReadOnlyList<IDailyAccuracy> Accuracy { get; }
}

/// <summary>One pool word and how well it is known.</summary>
public interface IWordStrength
{
    string Hanzi { get; }

    string Pinyin { get; }

    IReadOnlyList<string> Glosses { get; }

    /// <summary>Mastery strength, 0–5.</summary>
    int Strength { get; }
}

/// <summary>One day's answers.</summary>
public interface IDailyAccuracy
{
    /// <summary>The UTC day, as <c>yyyy-MM-dd</c>.</summary>
    string Date { get; }

    int Answered { get; }

    /// <summary>Answers with verdict <c>Correct</c> only; "almost" is not counted as correct.</summary>
    int Correct { get; }
}
