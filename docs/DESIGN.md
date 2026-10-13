# language-vocab — Design

_Mandarin vocabulary trainer. Goal: James learns to **speak and understand**
Mandarin. Web app first (iterate fast), mobile pivot later. Idea: issue #1;
process context: claude-code-bot#11._

## Product shape

A typing-based recall trainer with an adaptive vocabulary pool. Two drill
directions from day one:

1. **Zh → En**: show the word — hanzi large, pinyin under it (speaking focus:
   the pinyin is what he's really reading) — user types the English meaning.
2. **En → Zh**: show the English — user types the **pinyin** (the "English
   representation of how to speak it").

Chinese characters are always *displayed* (passive familiarity) but never
*required* as input.

## Bold assumptions (per claude-code-bot#11 — flagged, not blocking)

| # | Assumption | Why |
|---|---|---|
| A1 | The "english representation" = **pinyin**. Display with tone marks (nǐ hǎo), accept typed input as tone numbers (`ni3 hao3`), toneless (`ni hao`), or tone marks. | Pinyin is the standard, every reference uses it, it's typeable on any keyboard. |
| A2 | **Tones are graded, softly.** Correct syllables + correct tones = full credit; correct syllables, wrong/missing tones = "almost" (half credit, correct tones shown, card repeats sooner). | Tones are phonemic (mā mother vs mà scold) — ignoring them trains wrong speech, but hard-failing on them early is demoralising. Soft grading gives the signal without the wall. Strict mode = a settings toggle later. |
| A3 | Seed vocabulary = **HSK lists** (HSK1 ≈150 words → HSK2 ≈150 → HSK3 ≈300), stored as versioned JSON seed data in-repo (hanzi, pinyin, accepted English glosses, level, frequency rank). | Standard, well-ordered by usefulness, freely reproducible; no licensing issue with word lists. |
| A4 | **Adaptive pool = simple mastery model, not full SM-2 SRS.** Each word has strength 0–5 (+1 full credit, −1 miss, ±0 "almost"). Session picker samples weakness-weighted from the *active pool*. Pool grows by N new words when the pool's mean strength crosses a threshold; effectively shrinks when strengths drop (weak words dominate sampling and no new words unlock). | Delivers exactly the grow/shrink behaviour asked for in #1 with observable, debuggable mechanics. Schema keeps per-answer history so a real SRS scheduler can be swapped in later without data loss. |
| A5 | **Audio via browser SpeechSynthesis** (zh-CN voice) on each card — free, no API key, works offline-ish. | "Learning to speak" needs hearing; this is ~20 lines and zero infra. Real TTS/recorded audio can come later. |
| A6 | Single-user reality, but keep the template's auth as-is. | Zero extra work, and multi-user comes free if it ever goes public. |
| A7 | Grading normalisation: trim/case-fold; English answers match ANY accepted gloss ("he" or "him"); pinyin accepts `v` for `ü`, ignores spacing/apostrophes differences. | Don't punish formatting. |

## Explicitly deferred (roadmap, not v1)

- **Sentence mode** (the #1 issue names this as the eventual second phase) —
  schema reserves a `Sentence` item type.
- Other languages (data model is language-tagged from day one: `Language`
  column, seed loader per language).
- Real SRS scheduling, listening-first drills, speech input (Web Speech API
  recognition for zh is flaky), mobile app, streaks/gamification.

## MVP scope (v1, deployable)

1. Seed HSK1 into the DB via idempotent loader.
2. **Drill loop**: `GET /api/drill/next?direction=zh-en|en-zh` → card;
   `POST /api/drill/answer` → verdict (`correct | almost | wrong`), the
   canonical answer(s), tone feedback, updated strength.
3. Pool manager: active-pool bootstrap (first 10 by rank), weakness-weighted
   sampling, unlock rule (mean strength ≥ 3.5 → +5 words).
4. **Stats page**: pool size, per-word strength bars, accuracy over time.
5. Card UI: big hanzi, pinyin, audio button, answer box, instant feedback
   (green / amber with correct tones / red with answer), Enter-to-continue.

## Architecture

Follows web-template exactly (backend .NET + React frontend, helm chart,
oke-fleet ArgoCD deploy). App-specific pieces:

- `VocabItem` (id, language, hanzi, pinyin canonical, pinyin normalised,
  glosses[], level, rank, type=word|sentence)
- `UserWordState` (user, vocabItem, strength, lastSeen, timesSeen, timesCorrect)
- `AnswerLog` (user, vocabItem, direction, given, verdict, at) — the raw
  history that keeps future-SRS options open.
- `PinyinGrader` service: parse into syllables+tones from any accepted input
  form; compare canonical vs given; verdict + per-syllable tone diff.
  **This is the one genuinely fiddly component → gets the densest unit tests.**
- `PoolService`: sampling + unlock logic. Deterministic given (state, seed) →
  unit-testable.

## Answer to the open question in #1 (tones)

Yes — indicating tones is useful and this design treats them as first-class
but softly graded (A1/A2). Typing `ni3 hao3` *is* practising tones: recalling
tone-as-number forces the association without needing diacritic input.

## Delivery plan

- **PR 1** ✅: scaffold from web-template + this design doc + seed data + CI-less
  build scripts (bot can't push workflow files — James applies those).
- **PR 2** ✅: domain core, part A — entities, migration (DomainCore), PinyinGrader
  with dense tests. (Split from the original single "domain core" PR to keep each
  reviewable.)
- **PR 3**: domain core, part B — PoolService (bootstrap / weakness-weighted
  selection / unlock rule) + idempotent seed loader, with tests. Stacks on PR 2.
- **PR 4**: drill API routes + stats endpoint.
- **PR 5**: frontend drill screen + stats page + audio.
- Then: deploy PR in oke-fleet (needs image build by James — no workflows perm).

Each PR into `dev`, small and reviewable. Bold-assumption changes recorded in
this doc as they happen.
