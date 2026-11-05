/**
 * Progress along the path (BEH-PATH-1), kept on this device. A pack's Learn round is only its five
 * least-seen words, so a section's test is spread over rounds: a quiz round answered all right
 * credits its words to the section, and the section is passed once every word in it is credited.
 * Missing or corrupt storage reads as nothing credited, so a broken value never breaks the path.
 */
const KEY = 'lv.path.right';

type Credited = Record<string, string[]>;

const read = (): Credited => {
  try {
    const parsed: unknown = JSON.parse(localStorage.getItem(KEY) ?? '{}');
    if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) return {};
    return Object.fromEntries(
      Object.entries(parsed).map(([id, hanzi]) => [id, Array.isArray(hanzi) ? hanzi.filter((h): h is string => typeof h === 'string') : []]),
    );
  } catch {
    return {};
  }
};

/** The words of section `id` answered right so far. */
export const credited = (id: string): string[] => read()[id] ?? [];

/** Credit a round answered all right to its section. */
export const creditRound = (id: string, hanzi: string[]): void => {
  const all = read();
  all[id] = [...new Set([...(all[id] ?? []), ...hanzi])];
  try {
    localStorage.setItem(KEY, JSON.stringify(all));
  } catch {
    // Storage full or blocked: the round just goes uncredited.
  }
};

/** A section is passed once every word in it has been credited. */
export const isPassed = (pack: { id?: string | null; hanzi?: string[] | null }): boolean => {
  const right = new Set(credited(pack.id ?? ''));
  return (pack.hanzi ?? []).length > 0 && (pack.hanzi ?? []).every((h) => right.has(h));
};
