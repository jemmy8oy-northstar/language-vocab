import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useGetPacksQuery, type IPack } from '../api/generatedApi';
import { credited, isPassed } from '../pathProgress';
import config from '../data/config.json';

/**
 * The learning path (BEH-PATH-1, his 2026-11-04 Kit note): themed sections in order, e.g.
 * greetings, then 1 to 10, then mahjong. Nothing is locked: any section opens, search finds one,
 * and "Next in order" goes to the first section not yet passed. Learn's quiz credits the words.
 */
const Path = () => {
  const { data: packs, isFetching, isError } = useGetPacksQuery();
  const [query, setQuery] = useState('');
  const done = new Set((packs ?? []).filter(isPassed).map((p) => p.id ?? ''));

  const sections = (packs ?? []).filter((p) => p.path != null).sort((a, b) => (a.path ?? 0) - (b.path ?? 0));
  const needle = query.trim().toLowerCase();
  const shown = sections.filter(
    (p) => !needle || (p.title ?? '').toLowerCase().includes(needle) || (p.hanzi ?? []).some((h) => h.includes(needle)),
  );
  const next = sections.find((p) => !done.has(p.id ?? ''));

  return (
    <section className="container" style={{ paddingTop: '140px' }}>
      <div className="glass" style={{ padding: '40px 32px', maxWidth: '520px', margin: '0 auto', textAlign: 'center' }}>
        <h1 style={{ fontSize: '1.8rem', marginBottom: '8px' }}>Path</h1>
        <p style={{ color: 'var(--text-secondary)', marginBottom: '16px' }}>Go through the sections in order, or skip ahead.</p>
        {isError && <p style={{ color: '#f87171' }}>Could not reach the API.</p>}
        {!isError && (isFetching || !packs) && <p style={{ color: 'var(--text-secondary)' }}>Loading…</p>}

        {next && (
          <Link to={`${config.navigation.learn}?pack=${encodeURIComponent(next.id ?? '')}`}
            style={{ display: 'inline-block', padding: '10px 24px', borderRadius: '12px', background: 'var(--accent-primary)', color: '#fff', marginBottom: '20px' }}>
            Next in order: {next.title}
          </Link>
        )}
        {!next && sections.length > 0 && <p style={{ color: 'var(--accent-primary)', fontWeight: 700, marginBottom: '20px' }}>Path complete</p>}

        <div style={{ marginBottom: '20px' }}>
          <label htmlFor="path-search" style={{ display: 'block', color: 'var(--text-secondary)', marginBottom: '6px' }}>Search the path</label>
          <input id="path-search" type="search" autoComplete="off" value={query} onChange={(e) => setQuery(e.target.value)}
            style={{ width: '100%', padding: '10px 14px', borderRadius: '12px', border: '1px solid var(--glass-border)', background: 'var(--bg-card)', color: 'var(--text-primary)' }} />
        </div>

        <div role="region" aria-label="Path sections">
          <ol style={{ listStyle: 'none', padding: 0, display: 'grid', gap: '12px' }}>
            {shown.map((p: IPack) => (
              <li key={p.id}>
                <Link to={`${config.navigation.learn}?pack=${encodeURIComponent(p.id ?? '')}`}
                  style={{ display: 'block', padding: '16px 24px', borderRadius: '12px', background: 'var(--accent-primary)', color: '#fff', fontSize: '1.2rem' }}>
                  <span style={{ display: 'block' }}>
                    {p.path}. {p.title}{done.has(p.id ?? '') && <span style={{ marginLeft: '10px', fontSize: '0.85rem' }}>Done</span>}
                  </span>
                  {!done.has(p.id ?? '') && (
                    <span style={{ display: 'block', fontSize: '0.85rem', opacity: 0.85 }}>
                      {credited(p.id ?? '').filter((h) => p.hanzi?.includes(h)).length} of {p.hanzi?.length} words passed
                    </span>
                  )}
                  <span lang="zh-CN" style={{ display: 'block', fontSize: '0.9rem', opacity: 0.85, marginTop: '4px' }}>
                    {p.hanzi?.slice(0, 4).join(' ')}…
                  </span>
                </Link>
              </li>
            ))}
          </ol>
        </div>
        {!isError && packs && sections.length > 0 && shown.length === 0 && (
          <p style={{ color: 'var(--text-secondary)', marginTop: '12px' }}>Nothing on the path matches.</p>
        )}
      </div>
    </section>
  );
};

export default Path;
