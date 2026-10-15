import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useAnswerCardMutation, useGetLearnRoundQuery, type AnswerVerdict } from '../api/generatedApi';
import { speak } from '../speech';
import { verdictStyle } from '../verdict';
import config from '../data/config.json';

/**
 * Learn mode (BEH-LEARN-1/2, his 2026-10-15 Kit note): "flick through maybe 5 words where it
 * shows the pinyin and the english pairing and you have to click next, then after it questions
 * you on them". Practice draws from the whole pool; this teaches a round first and then asks
 * only about that round. Answers go through the same grader as Practice, so they count.
 */
const Learn = () => {
  const { data: round, isFetching, isError, refetch } = useGetLearnRoundQuery({}, { refetchOnMountOrArgChange: true });
  const [answerCard, { data: outcome, isLoading: isSubmitting, reset }] = useAnswerCardMutation();
  const [phase, setPhase] = useState<'show' | 'quiz' | 'done'>('show');
  const [index, setIndex] = useState(0);
  const [answer, setAnswer] = useState('');
  const [results, setResults] = useState<AnswerVerdict[]>([]);

  const words = round ?? [];
  const word = words[index];

  const restart = () => {
    reset();
    setAnswer('');
    setResults([]);
    setIndex(0);
    setPhase('show');
    refetch();
  };

  const next = () => {
    if (index + 1 < words.length) {
      setIndex(index + 1);
      return;
    }
    setIndex(0);
    setPhase('quiz');
  };

  // Enter checks, and Enter again moves on — the same rhythm as Practice (MVP §5).
  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (outcome?.verdict) {
      setResults([...results, outcome.verdict]);
      reset();
      setAnswer('');
      if (index + 1 < words.length) setIndex(index + 1);
      else setPhase('done');
      return;
    }
    if (!word?.vocabItemId || isSubmitting) return;
    await answerCard({ answerRequest: { vocabItemId: word.vocabItemId, direction: 'ZhToEn', answer } });
  };

  const feedback = outcome?.verdict ? verdictStyle[outcome.verdict] : null;
  const correct = results.filter((r) => r === 'Correct').length;

  return (
    <section className="container" style={{ paddingTop: '140px' }}>
      <div className="glass" style={{ padding: '40px 32px', maxWidth: '520px', margin: '0 auto', textAlign: 'center' }}>
        {isError && <p style={{ color: '#f87171' }}>Could not reach the API.</p>}
        {!isError && (isFetching || !round) && <p style={{ color: 'var(--text-secondary)' }}>Loading…</p>}
        {!isError && !isFetching && round && words.length === 0 && (
          <p style={{ color: 'var(--text-secondary)' }}>No words to learn yet.</p>
        )}

        {!isFetching && word && phase !== 'done' && (
          <>
            <p data-testid="learn-step" style={{ color: 'var(--text-secondary)', fontSize: '0.85rem', marginBottom: '12px' }}>
              {phase === 'show' ? 'Learn' : 'Quiz'} · {index + 1} of {words.length}
            </p>
            <p data-testid="hanzi" lang="zh-CN" style={{ fontSize: '4rem', lineHeight: 1.1 }}>
              {word.hanzi}
            </p>
            <p data-testid="pinyin" style={{ fontSize: '1.4rem', color: 'var(--text-secondary)', marginBottom: '8px' }}>
              {word.pinyin}
            </p>
            <button type="button" onClick={() => speak(word.hanzi ?? '')} aria-label="Play audio"
              style={{ fontSize: '1.4rem', marginBottom: '16px' }}>
              🔊
            </button>
          </>
        )}

        {!isFetching && word && phase === 'show' && (
          <>
            <p data-testid="english" style={{ fontSize: '1.6rem', marginBottom: '8px' }}>
              {word.glosses?.join(' · ')}
            </p>
            <button type="button" onClick={next} autoFocus
              style={{ marginTop: '20px', padding: '10px 24px', borderRadius: '12px', background: 'var(--accent-primary)', color: '#fff' }}>
              {index + 1 < words.length ? 'Next' : 'Start the quiz'}
            </button>
          </>
        )}

        {!isFetching && word && phase === 'quiz' && (
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
              placeholder="Type the English meaning"
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
                {outcome.verdict !== 'Correct' && (
                  <p style={{ color: 'var(--text-secondary)', marginTop: '4px' }}>{outcome.glosses?.join(' · ')}</p>
                )}
              </div>
            )}
            <button type="submit" disabled={isSubmitting}
              style={{ marginTop: '20px', padding: '10px 24px', borderRadius: '12px', background: 'var(--accent-primary)', color: '#fff' }}>
              {outcome ? 'Next ⏎' : 'Check ⏎'}
            </button>
          </form>
        )}

        {phase === 'done' && (
          <div data-testid="learn-done">
            <p style={{ fontSize: '1.6rem', marginBottom: '8px' }}>
              {correct} of {results.length} right
            </p>
            <p style={{ color: 'var(--text-secondary)', marginBottom: '20px' }}>
              {correct === results.length ? 'Every one — nice.' : 'The ones you missed come back in Practice.'}
            </p>
            <button type="button" onClick={restart} autoFocus
              style={{ padding: '10px 24px', borderRadius: '12px', background: 'var(--accent-primary)', color: '#fff', marginRight: '12px' }}>
              Learn five more
            </button>
            <Link to={config.navigation.home}>Practice</Link>
          </div>
        )}
      </div>
    </section>
  );
};

export default Learn;
