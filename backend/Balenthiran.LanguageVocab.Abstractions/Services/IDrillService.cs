using Balenthiran.LanguageVocab.Abstractions.Drilling;
using Balenthiran.LanguageVocab.Abstractions.Enums;
using Balenthiran.LanguageVocab.Abstractions.Packs;

namespace Balenthiran.LanguageVocab.Abstractions.Services;

/// <summary>
/// The drill loop (MVP §2–3): draw a card from the user's active pool, then grade an answer
/// to it and apply the consequences — strength (A4), the answer log, and the unlock rule.
/// </summary>
public interface IDrillService
{
    /// <summary>
    /// Bootstraps the pool if needed and draws the next card, weakness-weighted, with a fresh
    /// server-side seed per draw (kit#118). <paramref name="direction"/> <c>null</c> picks one
    /// at random. Returns <c>null</c> only if there is no vocabulary at all.
    /// </summary>
    Task<IDrillCard?> NextAsync(string userId, DrillDirection? direction, CancellationToken ct = default);

    /// <summary>
    /// Learn mode (BEH-LEARN-1): bootstraps the pool if needed and returns up to
    /// <paramref name="count"/> words from it, least-seen first and then by frequency rank, with
    /// nothing hidden. Showing a word writes nothing; the quiz that follows answers through
    /// <see cref="AnswerAsync"/>, so learning counts toward strength and unlocking like any drill.
    /// </summary>
    Task<IReadOnlyList<ILearnCard>> LearnAsync(string userId, int count, CancellationToken ct = default);

    /// <summary>
    /// Learn mode on one pack (BEH-PACK-1): adds the pack's words to the user's pool if they are
    /// not there yet, then returns up to <paramref name="count"/> of them, least-seen first and
    /// then in the pack's own order. Once in the pool, a pack's words drill in Practice too.
    /// </summary>
    Task<IReadOnlyList<ILearnCard>> LearnPackAsync(string userId, IPack pack, int count, CancellationToken ct = default);

    /// <summary>
    /// Grades <paramref name="answer"/> for a word in the user's pool: English against any
    /// accepted gloss (Zh→En), pinyin softly through the grader (En→Zh). Returns <c>null</c>
    /// if the word is not in the user's pool, in which case nothing is written.
    /// </summary>
    Task<IDrillOutcome?> AnswerAsync(
        string userId, Guid vocabItemId, DrillDirection direction, string answer, CancellationToken ct = default);
}
