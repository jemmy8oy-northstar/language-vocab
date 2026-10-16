import { Link } from 'react-router-dom';
import { useGetPacksQuery } from '../api/generatedApi';
import config from '../data/config.json';

/**
 * Vocab packs (BEH-PACK-1, his 2026-10-15 Kit note): "I would love to add a mahjong vocab pack…
 * a fun and intuitive way to get interested in learning a language". Each pack opens Learn on
 * just its words; once learned they drill in Practice like any other.
 */
const Packs = () => {
  const { data: packs, isFetching, isError } = useGetPacksQuery();

  return (
    <section className="container" style={{ paddingTop: '140px' }}>
      <div className="glass" style={{ padding: '40px 32px', maxWidth: '520px', margin: '0 auto', textAlign: 'center' }}>
        <h1 style={{ fontSize: '1.8rem', marginBottom: '8px' }}>Packs</h1>
        <p style={{ color: 'var(--text-secondary)', marginBottom: '24px' }}>Learn a themed set of words together.</p>
        {isError && <p style={{ color: '#f87171' }}>Could not reach the API.</p>}
        {!isError && (isFetching || !packs) && <p style={{ color: 'var(--text-secondary)' }}>Loading…</p>}
        {packs?.length === 0 && <p style={{ color: 'var(--text-secondary)' }}>No packs yet.</p>}
        <ul style={{ listStyle: 'none', padding: 0, display: 'grid', gap: '12px' }}>
          {packs?.map((p) => (
            <li key={p.id}>
              <Link
                to={`${config.navigation.learn}?pack=${encodeURIComponent(p.id ?? '')}`}
                role="button"
                style={{
                  display: 'block',
                  padding: '16px 24px',
                  borderRadius: '12px',
                  background: 'var(--accent-primary)',
                  color: '#fff',
                  fontSize: '1.2rem',
                }}
              >
                {p.title}
                <span lang="zh-CN" style={{ display: 'block', fontSize: '0.9rem', opacity: 0.85, marginTop: '4px' }}>
                  {p.hanzi?.length} words · {p.hanzi?.slice(0, 4).join(' ')}…
                </span>
              </Link>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
};

export default Packs;
