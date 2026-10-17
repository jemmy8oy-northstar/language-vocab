import { useGetStatsQuery } from '../api/generatedApi';

const MAX_STRENGTH = 5;
const muted = { color: 'var(--text-secondary)' } as const;
const track = { background: 'var(--glass-border)', borderRadius: '999px', overflow: 'hidden' } as const;

/**
 * Stats (BEH-STATS-1, MVP-4): how big the pool is, how strong each word in it is, and how
 * accuracy has moved day by day. Bars are plain divs sized by percentage — no chart library.
 */
const Stats = () => {
  const { data: stats, isFetching, isError } = useGetStatsQuery(undefined, { refetchOnMountOrArgChange: true });
  const words = stats?.words ?? [];
  const days = stats?.accuracy ?? [];

  return (
    <section className="container" style={{ paddingTop: '140px' }}>
      <div className="glass" style={{ padding: '40px 32px', maxWidth: '520px', margin: '0 auto' }}>
        <h1 style={{ fontSize: '1.8rem', marginBottom: '8px', textAlign: 'center' }}>Stats</h1>
        {isError && <p style={{ color: '#f87171', textAlign: 'center' }}>Could not reach the API.</p>}
        {!isError && (isFetching || !stats) && <p style={{ ...muted, textAlign: 'center' }}>Loading…</p>}

        {!isError && !isFetching && stats && (
          <>
            <p data-testid="pool-size" style={{ textAlign: 'center', marginBottom: '28px', ...muted }}>
              <span style={{ fontSize: '2.4rem', color: 'var(--text-primary)', fontWeight: 700 }}>{stats.poolSize}</span>
              {' '}of {stats.totalWords} words in your pool
            </p>

            <h2 style={{ fontSize: '1.1rem', marginBottom: '12px' }}>Word strength</h2>
            <div data-testid="strength-bars" style={{ display: 'grid', gap: '8px', marginBottom: '28px' }}>
              {words.length === 0 && <p style={muted}>No words yet — start with Practice or Learn.</p>}
              {words.map((w) => (
                <div key={w.hanzi} data-testid="strength-row" style={{ display: 'grid', gridTemplateColumns: '84px 1fr 28px', gap: '10px', alignItems: 'center' }}>
                  <span lang="zh-CN" title={`${w.pinyin} · ${w.glosses?.join(' · ')}`} style={{ fontSize: '1.1rem' }}>{w.hanzi}</span>
                  <div style={{ ...track, height: '10px' }}>
                    <div data-testid="strength-bar" role="meter" aria-label={`Strength of ${w.hanzi}`}
                      aria-valuemin={0} aria-valuemax={MAX_STRENGTH} aria-valuenow={Number(w.strength)}
                      style={{ width: `${(Number(w.strength) / MAX_STRENGTH) * 100}%`, height: '100%', background: 'var(--accent-primary)' }} />
                  </div>
                  <span style={{ ...muted, fontSize: '0.85rem', textAlign: 'right' }}>{w.strength}</span>
                </div>
              ))}
            </div>

            <h2 style={{ fontSize: '1.1rem', marginBottom: '12px' }}>Accuracy over time</h2>
            <div data-testid="accuracy-over-time" style={{ display: 'grid', gap: '8px' }}>
              {days.length === 0 && <p style={muted}>No answers yet — your accuracy shows up here.</p>}
              {days.map((d) => {
                const answered = Number(d.answered);
                const pct = answered > 0 ? Math.round((Number(d.correct) / answered) * 100) : 0;
                return (
                  <div key={d.date} data-testid="accuracy-row" style={{ display: 'grid', gridTemplateColumns: '84px 1fr 96px', gap: '10px', alignItems: 'center' }}>
                    <span style={{ ...muted, fontSize: '0.85rem' }}>{d.date}</span>
                    <div style={{ ...track, height: '10px' }}>
                      <div style={{ width: `${pct}%`, height: '100%', background: '#22c55e' }} />
                    </div>
                    <span style={{ ...muted, fontSize: '0.85rem', textAlign: 'right' }}>{pct}% · {d.correct}/{d.answered}</span>
                  </div>
                );
              })}
            </div>
          </>
        )}
      </div>
    </section>
  );
};

export default Stats;
