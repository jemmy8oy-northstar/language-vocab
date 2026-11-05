import { useState } from 'react';
import { useAnswerCardMutation } from '../api/generatedApi';
import { shuffle } from '../shuffle';
import { speak } from '../speech';
import { verdictStyle } from '../verdict';
import { recordWrong } from '../wrongCounts';

export interface DrillItem {
  vocabItemId: string;
  hanzi: string;
  pinyin: string;
  glosses: string[];
}

/**
 * A drill round over chosen items (BEH-PATH-2, his 2026-11-05 Kit note): "go through the failed
 * ones again until you get them right or review them". The quiz card is Learn's (BEH-LEARN-2):
 * same prompt, same grader, same Enter-checks-Enter-moves-on rhythm. A pass asks every item once;
 * the misses form the next pass, and a re-drill repeats until a pass has no misses.
 */
const DrillRound = ({ items, reverse, onExit }: { items: DrillItem[]; reverse: boolean; onExit: () => void }) => {
  const [answerCard, { data: outcome, isLoading: isSubmitting, reset }] = useAnswerCardMutation();
  const [queue, setQueue] = useState(() => shuffle(items));
  const [index, setIndex] = useState(0);
  const [answer, setAnswer] = useState('');
  // Misses of the pass in progress, and how often each item was missed across every pass of this round.
  const [missed, setMissed] = useState<DrillItem[]>([]);
  const [timesWrong, setTimesWrong] = useState<Record<string, number>>({});
  const [phase, setPhase] = useState<'quiz' | 'done'>('quiz');
  const [reviewing, setReviewing] = useState(false);

  const word = queue[index];
  const hideWord = reverse && !outcome;
  const feedback = outcome?.verdict ? verdictStyle[outcome.verdict] : null;

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (outcome?.verdict) {
      // Anything but Correct is a miss, as in Learn, where only an all-Correct round credits.
      const right = outcome.verdict === 'Correct';
      if (!right) {
        recordWrong(word);
        setTimesWrong({ ...timesWrong, [word.hanzi]: (timesWrong[word.hanzi] ?? 0) + 1 });
      }
      setMissed(right ? missed : [...missed, word]);
      reset();
      setAnswer('');
      if (index + 1 < queue.length) setIndex(index + 1);
      else setPhase('done');
      return;
    }
    if (isSubmitting) return;
    await answerCard({ answerRequest: { vocabItemId: word.vocabItemId, direction: reverse ? 'EnToZh' : 'ZhToEn', answer } });
  };

  const redrill = () => {
    setQueue(shuffle(missed));
    setMissed([]);
    setIndex(0);
    setReviewing(false);
    setPhase('quiz');
  };

  const wrongInRound = items.filter((i) => timesWrong[i.hanzi]);

  if (phase === 'done') {
    const right = queue.length - missed.length;
    return (
      <div data-testid="drill-done">
        <p data-testid="drill-tally" style={{ fontSize: '1.6rem', marginBottom: '8px' }}>
          {right} right, {missed.length} wrong
        </p>
        {missed.length === 0 && <p style={{ color: 'var(--accent-primary)', fontWeight: 700, marginBottom: '12px' }}>Every one right now.</p>}

        {wrongInRound.length > 0 && (
          <div role="region" aria-label="Times wrong" style={{ marginBottom: '16px' }}>
            <p style={{ color: 'var(--text-secondary)', marginBottom: '6px' }}>Times wrong this round</p>
            <ul style={{ listStyle: 'none', padding: 0 }}>
              {wrongInRound.map((i) => (
                <li key={i.hanzi}>
                  <span lang="zh-CN">{i.hanzi}</span> · wrong {timesWrong[i.hanzi]} {timesWrong[i.hanzi] === 1 ? 'time' : 'times'}
                </li>
              ))}
            </ul>
          </div>
        )}

        {reviewing && (
          <div role="region" aria-label="Missed words" style={{ marginBottom: '16px' }}>
            <ul style={{ listStyle: 'none', padding: 0 }}>
              {missed.map((i) => (
                <li key={i.hanzi}>
                  <span lang="zh-CN">{i.hanzi}</span> {i.pinyin} · {i.glosses.join(' · ')}
                </li>
              ))}
            </ul>
          </div>
        )}

        {missed.length > 0 && (
          <>
            <button type="button" onClick={redrill} autoFocus
              style={{ padding: '10px 24px', borderRadius: '12px', background: 'var(--accent-primary)', color: '#fff', marginRight: '12px' }}>
              Drill the missed again
            </button>
            <button type="button" onClick={() => setReviewing(!reviewing)} aria-pressed={reviewing}
              style={{ padding: '10px 24px', borderRadius: '12px', border: '1px solid var(--glass-border)', color: 'var(--text-primary)', marginRight: '12px' }}>
              Review them
            </button>
          </>
        )}
        <button type="button" onClick={onExit} autoFocus={missed.length === 0}
          style={{ padding: '10px 24px', borderRadius: '12px', border: '1px solid var(--glass-border)', color: 'var(--text-primary)' }}>
          Back to the path
        </button>
      </div>
    );
  }

  return (
    <>
      <p data-testid="drill-step" style={{ color: 'var(--text-secondary)', fontSize: '0.85rem', marginBottom: '12px' }}>
        Drill · {index + 1} of {queue.length}
      </p>
      {hideWord ? (
        <p data-testid="english-prompt" style={{ fontSize: '1.6rem', marginBottom: '16px' }}>{word.glosses.join(' · ')}</p>
      ) : (
        <>
          <p data-testid="hanzi" lang="zh-CN" style={{ fontSize: '4rem', lineHeight: 1.1 }}>{word.hanzi}</p>
          <p data-testid="pinyin" style={{ fontSize: '1.4rem', color: 'var(--text-secondary)', marginBottom: '8px' }}>{word.pinyin}</p>
          <button type="button" onClick={() => speak(word.hanzi)} aria-label="Play audio" style={{ fontSize: '1.4rem', marginBottom: '16px' }}>
            🔊
          </button>
        </>
      )}
      <form onSubmit={onSubmit}>
        <input
          aria-label="Answer"
          autoFocus
          autoComplete="off"
          autoCapitalize="off"
          spellCheck={false}
          value={answer}
          readOnly={!!outcome}
          onChange={(e) => setAnswer(e.target.value)}
          placeholder={reverse ? 'Type the pinyin, e.g. ni3 hao3' : 'Type the English meaning'}
          style={{
            width: '100%',
            padding: '12px 16px',
            fontSize: '1.1rem',
            borderRadius: '12px',
            border: `2px solid ${feedback?.colour ?? 'var(--glass-border)'}`,
            background: 'var(--bg-card)',
            color: 'var(--text-primary)',
            marginTop: '8px',
          }}
        />
        {outcome && feedback && (
          <div data-testid="verdict" style={{ marginTop: '20px' }}>
            <p style={{ color: feedback.colour, fontWeight: 700, fontSize: '1.2rem' }}>{feedback.label}</p>
            {!reverse && outcome.verdict !== 'Correct' && (
              <p style={{ color: 'var(--text-secondary)', marginTop: '4px' }}>{outcome.glosses?.join(' · ')}</p>
            )}
          </div>
        )}
        <button type="submit" disabled={isSubmitting}
          style={{ marginTop: '20px', padding: '10px 24px', borderRadius: '12px', background: 'var(--accent-primary)', color: '#fff' }}>
          {outcome ? 'Next ⏎' : 'Check ⏎'}
        </button>
      </form>
    </>
  );
};

export default DrillRound;
