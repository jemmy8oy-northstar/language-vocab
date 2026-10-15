import { useState } from 'react';
import { useAnswerCardMutation, useGetNextCardQuery } from '../api/generatedApi';
import { speak } from '../speech';
import { verdictStyle } from '../verdict';


const Practice = () => {
  const { data: card, isFetching, isError, refetch } = useGetNextCardQuery({}, { refetchOnMountOrArgChange: true });
  const [answerCard, { data: outcome, isLoading: isSubmitting, reset }] = useAnswerCardMutation();
  const [answer, setAnswer] = useState('');

  // Enter submits, and Enter again moves on (MVP §5).
  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (outcome) {
      reset();
      setAnswer('');
      refetch();
      return;
    }
    if (!card?.vocabItemId || !card.direction || isSubmitting) return;
    await answerCard({ answerRequest: { vocabItemId: card.vocabItemId, direction: card.direction, answer } });
  };

  const zhToEn = card?.direction === 'ZhToEn';
  // The word itself is never shown before an En→Zh answer: the hanzi would give the pinyin away.
  const hanzi = outcome?.hanzi ?? (zhToEn ? card?.hanzi : null);
  const pinyin = outcome?.pinyin ?? (zhToEn ? card?.pinyin : null);
  const feedback = outcome?.verdict ? verdictStyle[outcome.verdict] : null;

  return (
    <section className="container" style={{ paddingTop: '140px' }}>
      <div className="glass" style={{ padding: '40px 32px', maxWidth: '520px', margin: '0 auto', textAlign: 'center' }}>
        {isError && <p style={{ color: '#f87171' }}>Could not reach the API.</p>}
        {!isError && !card && <p style={{ color: 'var(--text-secondary)' }}>Loading…</p>}

        {card && (
          <form onSubmit={onSubmit}>
            {!zhToEn && (
              <p data-testid="english-prompt" style={{ fontSize: '1.6rem', marginBottom: '16px' }}>
                {card.glosses?.join(' · ')}
              </p>
            )}
            {hanzi && (
              <p data-testid="hanzi" lang="zh-CN" style={{ fontSize: '4rem', lineHeight: 1.1 }}>
                {hanzi}
              </p>
            )}
            {pinyin && (
              <p data-testid="pinyin" style={{ fontSize: '1.4rem', color: 'var(--text-secondary)', marginBottom: '8px' }}>
                {pinyin}
              </p>
            )}
            {hanzi && (
              <button type="button" onClick={() => speak(hanzi)} aria-label="Play audio"
                style={{ fontSize: '1.4rem', marginBottom: '16px' }}>
                🔊
              </button>
            )}

            <input
              aria-label="Answer"
              autoFocus
              autoComplete="off"
              autoCapitalize="off"
              spellCheck={false}
              value={answer}
              readOnly={!!outcome}
              onChange={(e) => setAnswer(e.target.value)}
              placeholder={zhToEn ? 'Type the English meaning' : 'Type the pinyin — ni3 hao3, ni hao or nǐ hǎo'}
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
                {/* En→Zh needs no correction line: the word above now shows the right pinyin. */}
                {zhToEn && outcome.verdict !== 'Correct' && (
                  <p style={{ color: 'var(--text-secondary)', marginTop: '4px' }}>{outcome.glosses?.join(' · ')}</p>
                )}
                <p style={{ color: 'var(--text-secondary)', fontSize: '0.85rem', marginTop: '8px' }}>
                  Strength {outcome.strength}/5
                  {Number(outcome.unlocked) > 0 && ` · ${outcome.unlocked} new words unlocked`}
                </p>
              </div>
            )}

            <button type="submit" disabled={isFetching || isSubmitting}
              style={{ marginTop: '20px', padding: '10px 24px', borderRadius: '12px', background: 'var(--accent-primary)', color: '#fff' }}>
              {outcome ? 'Next ⏎' : 'Check ⏎'}
            </button>
          </form>
        )}
      </div>
    </section>
  );
};

export default Practice;
