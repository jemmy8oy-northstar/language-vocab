import { describe, expect, it } from 'vitest';
import { shuffle } from './shuffle';

describe('shuffle', () => {
  it('keeps every item exactly once', () => {
    expect(shuffle([1, 2, 3, 4, 5]).sort()).toEqual([1, 2, 3, 4, 5]);
  });

  it('never hands back the input order, even when the draw lands on it', () => {
    // random() just under 1 makes every Fisher–Yates swap a self-swap: the identity permutation.
    expect(shuffle(['a', 'b', 'c'], () => 0.9999)).toEqual(['b', 'c', 'a']);
    for (let n = 0; n < 200; n++) expect(shuffle(['a', 'b', 'c', 'd', 'e'])).not.toEqual(['a', 'b', 'c', 'd', 'e']);
  });

  it('leaves an empty or one-word round as it is, and does not touch the input', () => {
    const input = ['a', 'b'];
    expect(shuffle([])).toEqual([]);
    expect(shuffle(['a'])).toEqual(['a']);
    shuffle(input);
    expect(input).toEqual(['a', 'b']);
  });
});
