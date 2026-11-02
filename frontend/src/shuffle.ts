/**
 * A copy of `items` in a new order (Fisher–Yates). When the shuffle happens to land on the input
 * order, it is rotated by one instead: a quiz that came back unshuffled would be the very "same
 * order" his 2026-11-01 Kit note asked to get rid of.
 */
export const shuffle = <T,>(items: readonly T[], random: () => number = Math.random): T[] => {
  const out = [...items];
  for (let i = out.length - 1; i > 0; i--) {
    const j = Math.floor(random() * (i + 1));
    [out[i], out[j]] = [out[j], out[i]];
  }
  const unchanged = out.every((item, i) => item === items[i]);
  return unchanged && out.length > 1 ? [...out.slice(1), out[0]] : out;
};
