/**
 * How often each word has been answered wrong in a path drill round (BEH-PATH-2, his 2026-11-05
 * Kit note: "keep track what the worse vocab is"), kept on this device next to the path progress.
 * Keyed by hanzi, like pathProgress. Missing or corrupt storage reads as no misses, so a broken
 * value never breaks the path.
 */
const KEY = 'lv.path.wrong';

export interface WrongWord {
  hanzi: string;
  pinyin: string;
  glosses: string[];
  count: number;
}

type Stored = Record<string, Omit<WrongWord, 'hanzi'>>;

const read = (): Stored => {
  try {
    const parsed: unknown = JSON.parse(localStorage.getItem(KEY) ?? '{}');
    if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) return {};
    const out: Stored = {};
    for (const [hanzi, v] of Object.entries(parsed)) {
      const w = v as Partial<Omit<WrongWord, 'hanzi'>> | null;
      if (w && typeof w.count === 'number' && w.count > 0) {
        out[hanzi] = {
          count: w.count,
          pinyin: typeof w.pinyin === 'string' ? w.pinyin : '',
          glosses: Array.isArray(w.glosses) ? w.glosses.filter((g): g is string => typeof g === 'string') : [],
        };
      }
    }
    return out;
  } catch {
    return {};
  }
};

/** Count one wrong answer against a word. Call once per wrong answer, never per screen render. */
export const recordWrong = (word: { hanzi: string; pinyin: string; glosses: string[] }): void => {
  const all = read();
  all[word.hanzi] = { count: (all[word.hanzi]?.count ?? 0) + 1, pinyin: word.pinyin, glosses: word.glosses };
  try {
    localStorage.setItem(KEY, JSON.stringify(all));
  } catch {
    // Storage full or blocked: the miss just goes uncounted.
  }
};

/** The words missed most, highest count first (ties keep the hanzi order, so the list is stable). */
export const worstWords = (limit = 10): WrongWord[] =>
  Object.entries(read())
    .map(([hanzi, w]) => ({ hanzi, ...w }))
    .sort((a, b) => b.count - a.count || a.hanzi.localeCompare(b.hanzi))
    .slice(0, limit);
