import { useState } from 'react';
import { Link } from 'react-router-dom';
import { enhancedApi, useGetPacksQuery, type IPack } from '../api/generatedApi';
import DrillRound, { type DrillItem } from '../components/DrillRound';
import { credited, isPassed } from '../pathProgress';
import { worstWords } from '../wrongCounts';
import config from '../data/config.json';

type Kind = 'words' | 'sentences' | 'both';

// The most the API returns for one section's round (MaxLearnRound).
const SECTION_ROUND = 20;

// The data has no word/sentence flag yet (VocabItemType.Sentence is reserved), so a sentence is
// told by its shape: four or more characters, or spaces and punctuation inside it.
const isSentence = (hanzi: string) => [...hanzi].length >= 4 || /[\s，。！？、,.!?]/.test(hanzi);

/**
 * The learning path (BEH-PATH-1, his 2026-11-04 Kit note): themed sections in order, e.g.
 * greetings, then 1 to 10, then mahjong. Nothing is locked: any section opens, search finds one,
 * and "Next in order" goes to the first section not yet passed. Learn's quiz credits the words.
 * BEH-PATH-2 (his 2026-11-05 note): tick sections, pick words / sentences / both, and drill them
 * as a round that re-drills the misses; "Worst words" lists what has been missed most.
 */
const Path = () => {
  const { data: packs, isFetching, isError } = useGetPacksQuery();
  const [loadRound] = enhancedApi.useLazyGetLearnRoundQuery();
  const [query, setQuery] = useState('');
  const [selected, setSelected] = useState<string[]>([]);
  const [kind, setKind] = useState<Kind>('both');
  const [drill, setDrill] = useState<DrillItem[] | null>(null);
  const [drillError, setDrillError] = useState('');
  const [starting, setStarting] = useState(false);
  const done = new Set((packs ?? []).filter(isPassed).map((p) => p.id ?? ''));

  const sections = (packs ?? []).filter((p) => p.path != null).sort((a, b) => (a.path ?? 0) - (b.path ?? 0));
  const needle = query.trim().toLowerCase();
  const shown = sections.filter(
    (p) => !needle || (p.title ?? '').toLowerCase().includes(needle) || (p.hanzi ?? []).some((h) => h.includes(needle)),
  );
  const next = sections.find((p) => !done.has(p.id ?? ''));

  const toggle = (id: string) => setSelected(selected.includes(id) ? selected.filter((s) => s !== id) : [...selected, id]);

  // Fetch every ticked section (in path order), keep the kind asked for, and drop a word two sections share.
  const startRound = async () => {
    setStarting(true);
    setDrillError('');
    try {
      const rounds = await Promise.all(
        sections.filter((p) => selected.includes(p.id ?? '')).map((p) => loadRound({ pack: p.id, count: SECTION_ROUND }).unwrap()),
      );
      const seen = new Set<string>();
      const items: DrillItem[] = [];
      for (const c of rounds.flat()) {
        const hanzi = c.hanzi ?? '';
        const wanted = kind === 'both' || (kind === 'sentences') === isSentence(hanzi);
        if (!c.vocabItemId || seen.has(c.vocabItemId) || !wanted) continue;
        seen.add(c.vocabItemId);
        items.push({ vocabItemId: c.vocabItemId, hanzi, pinyin: c.pinyin ?? '', glosses: c.glosses ?? [] });
      }
      if (items.length === 0) setDrillError('Nothing to drill with that selection.');
      else setDrill(items);
    } catch {
      setDrillError('Could not load those sections.');
    } finally {
      setStarting(false);
    }
  };

  if (drill) {
    return (
      <section className="container" style={{ paddingTop: '140px' }}>
        <div className="glass" style={{ padding: '40px 32px', maxWidth: '520px', margin: '0 auto', textAlign: 'center' }}>
          <DrillRound items={drill} reverse={localStorage.getItem('learn-reverse') === '1'}
            onExit={() => setDrill(null)} />
        </div>
      </section>
    );
  }

  const worst = worstWords();
  const pillStyle = (on: boolean) => ({
    padding: '6px 14px', borderRadius: '999px', fontSize: '0.85rem', border: '1px solid var(--glass-border)',
    background: on ? 'var(--accent-primary)' : 'transparent', color: on ? '#fff' : 'var(--text-secondary)',
  });

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
              <li key={p.id} style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                <input type="checkbox" aria-label={`Drill ${p.title}`} checked={selected.includes(p.id ?? '')} onChange={() => toggle(p.id ?? '')} />
                <Link to={`${config.navigation.learn}?pack=${encodeURIComponent(p.id ?? '')}`}
                  style={{ display: 'block', flex: 1, padding: '16px 24px', borderRadius: '12px', background: 'var(--accent-primary)', color: '#fff', fontSize: '1.2rem' }}>
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

        {sections.length > 0 && (
          <div role="group" aria-label="Drill round" style={{ marginTop: '20px' }}>
            <p style={{ color: 'var(--text-secondary)', marginBottom: '8px' }}>
              Tick sections to drill ({selected.length} selected){' '}
              <button type="button" onClick={() => setSelected([...new Set([...selected, ...shown.map((p) => p.id ?? '')])])}
                style={{ textDecoration: 'underline' }}>
                Select shown
              </button>
            </p>
            <div style={{ display: 'flex', gap: '8px', justifyContent: 'center', marginBottom: '12px' }}>
              {(['words', 'sentences', 'both'] as const).map((k) => (
                <button key={k} type="button" aria-pressed={kind === k} onClick={() => setKind(k)} style={pillStyle(kind === k)}>
                  {k === 'words' ? 'Words' : k === 'sentences' ? 'Sentences' : 'Both'}
                </button>
              ))}
            </div>
            <button type="button" disabled={selected.length === 0 || starting} onClick={startRound}
              style={{ padding: '10px 24px', borderRadius: '12px', background: 'var(--accent-primary)', color: '#fff' }}>
              Start round
            </button>
            {drillError && <p style={{ color: '#f87171', marginTop: '8px' }}>{drillError}</p>}
          </div>
        )}

        {worst.length > 0 && (
          <div role="region" aria-label="Worst words" style={{ marginTop: '24px' }}>
            <p style={{ color: 'var(--text-secondary)', marginBottom: '6px' }}>Worst words</p>
            <ol style={{ listStyle: 'none', padding: 0 }}>
              {worst.map((w) => (
                <li key={w.hanzi}>
                  <span lang="zh-CN">{w.hanzi}</span> {w.pinyin} · {w.glosses.join(' · ')} · wrong {w.count} {w.count === 1 ? 'time' : 'times'}
                </li>
              ))}
            </ol>
          </div>
        )}
      </div>
    </section>
  );
};

export default Path;
