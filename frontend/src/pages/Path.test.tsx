import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';
import { renderPage, stubApi } from '../test/harness';
import { creditRound } from '../pathProgress';
import { recordWrong, worstWords } from '../wrongCounts';
import Path from './Path';

// Deliberately out of path order, with one pack off the path, so the page must sort and skip.
const packs = [
  { id: 'mahjong', title: 'Mahjong', hanzi: ['麻将', '万', '条'], path: 3 },
  { id: 'extra', title: 'Extra', hanzi: ['猫'], path: null },
  { id: 'numbers', title: 'Numbers 1 to 10', hanzi: ['一', '二', '三'], path: 2 },
  { id: 'greetings', title: 'Greetings', hanzi: ['你好', '谢谢'], path: 1 },
];

const sectionLinks = () => within(screen.getByRole('region', { name: 'Path sections' })).getAllByRole('link');

describe('Learning path (BEH-PATH-1)', () => {
  beforeEach(() => localStorage.clear());

  it("BEH-PATH-1: lists the path's sections in order, all open, skipping packs off the path", async () => {
    stubApi({ 'GET /api/packs': packs });
    renderPage(<Path />, '/path');

    await screen.findByRole('link', { name: /Numbers 1 to 10/ });
    expect(sectionLinks().map((l) => l.getAttribute('href'))).toEqual([
      '/learn?pack=greetings',
      '/learn?pack=numbers',
      '/learn?pack=mahjong',
    ]);
    expect(screen.queryByText('Extra')).not.toBeInTheDocument();
    // Nothing is locked: every section is an enabled link, none is marked done yet.
    sectionLinks().forEach((l) => expect(l).not.toHaveAttribute('aria-disabled'));
    expect(screen.queryByText('Done')).not.toBeInTheDocument();
  });

  it('BEH-PATH-1: search narrows the sections by title or word', async () => {
    stubApi({ 'GET /api/packs': packs });
    const user = userEvent.setup();
    renderPage(<Path />, '/path');
    await screen.findByRole('link', { name: /Numbers 1 to 10/ });
    const search = screen.getByLabelText('Search the path');

    await user.type(search, 'MAHJONG');
    expect(sectionLinks().map((l) => l.getAttribute('href'))).toEqual(['/learn?pack=mahjong']);

    await user.clear(search);
    await user.type(search, '谢');
    expect(sectionLinks().map((l) => l.getAttribute('href'))).toEqual(['/learn?pack=greetings']);
  });

  it('BEH-PATH-1: Next in order goes to the first section not yet done', async () => {
    stubApi({ 'GET /api/packs': packs });
    creditRound('greetings', ['你好', '谢谢']);
    // Two of numbers' three words: credited, but not passed.
    creditRound('numbers', ['一', '二']);
    renderPage(<Path />, '/path');

    expect(await screen.findByRole('link', { name: /Next in order/ })).toHaveAttribute('href', '/learn?pack=numbers');
    expect(screen.getAllByText('Done')).toHaveLength(1);
    expect(screen.getByText('2 of 3 words passed')).toBeInTheDocument();
    expect(screen.queryByText('Path complete')).not.toBeInTheDocument();
  });
});

const greetingCards = [
  { vocabItemId: 'g1', hanzi: '你好', pinyin: 'nǐ hǎo', glosses: ['hello'] },
  { vocabItemId: 'g2', hanzi: '谢谢', pinyin: 'xiè xie', glosses: ['thanks'] },
  { vocabItemId: 'g3', hanzi: '你好吗，朋友', pinyin: 'nǐ hǎo ma péng yǒu', glosses: ['how are you, friend'] },
];
const numberCards = [
  { vocabItemId: 'n1', hanzi: '一', pinyin: 'yī', glosses: ['one'] },
  { vocabItemId: 'g1', hanzi: '你好', pinyin: 'nǐ hǎo', glosses: ['hello'] }, // the same word, shared with greetings
];

/** Learn rounds come back one section at a time, in the order the sections were ticked (path order). */
const sectionRounds = (...rounds: unknown[][]) => {
  let n = 0;
  return () => rounds[n++ % rounds.length];
};

/** Verdicts are scripted in the order the answers arrive; the card order is shuffled, so tests read the card shown. */
const scriptedVerdicts = (...verdicts: string[]) => () => ({
  verdict: verdicts.shift() ?? 'Correct', hanzi: '', pinyin: '', glosses: ['x'], strength: 1, unlocked: 0,
});

type User = ReturnType<typeof userEvent.setup>;

const tick = async (user: User, ...titles: string[]) => {
  for (const t of titles) await user.click(await screen.findByRole('checkbox', { name: `Drill ${t}` }));
};

const startRound = async (user: User) => {
  await user.click(screen.getByRole('button', { name: 'Start round' }));
  await screen.findByTestId('drill-step');
};

/** Answer the card on screen and move on; returns the hanzi that was asked. */
const answerOne = async (user: User) => {
  const asked = screen.getByTestId('hanzi').textContent ?? '';
  await user.type(screen.getByRole('textbox', { name: 'Answer' }), 'x{Enter}');
  await screen.findByTestId('verdict');
  await user.keyboard('{Enter}');
  return asked;
};

const askedInPass = async (user: User, count: number) => {
  const asked: string[] = [];
  for (let i = 0; i < count; i++) asked.push(await answerOne(user));
  return asked;
};

describe('Drill chosen lessons (BEH-PATH-2)', () => {
  beforeEach(() => localStorage.clear());

  it("BEH-PATH-2: the ticked sections and the Words toggle build the item set, a shared word once", async () => {
    const calls = stubApi({
      'GET /api/packs': packs,
      'GET /api/drill/learn': sectionRounds(greetingCards, numberCards),
      'POST /api/drill/answer': scriptedVerdicts(),
    });
    const user = userEvent.setup();
    renderPage(<Path />, '/path');
    await tick(user, 'Greetings', 'Numbers 1 to 10');
    expect(screen.getByText(/2 selected/)).toBeInTheDocument();

    // Words only: the long greeting sentence is left out.
    await user.click(screen.getByRole('button', { name: 'Words' }));
    await startRound(user);
    expect(screen.getByTestId('drill-step')).toHaveTextContent('1 of 3');
    const words = await askedInPass(user, 3);
    expect([...words].sort()).toEqual(['一', '你好', '谢谢'].sort());
    expect(calls.filter((c) => c.path.endsWith('/api/drill/learn'))).toHaveLength(2);
  });

  it("BEH-PATH-2: Sentences keeps only the sentences, and search then Select shown ticks what is listed", async () => {
    stubApi({ 'GET /api/packs': packs, 'GET /api/drill/learn': greetingCards, 'POST /api/drill/answer': scriptedVerdicts() });
    const user = userEvent.setup();
    renderPage(<Path />, '/path');
    await user.type(await screen.findByLabelText('Search the path'), 'greet');
    await user.click(screen.getByRole('button', { name: 'Select shown' }));
    expect(screen.getByText(/1 selected/)).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Sentences' }));
    await startRound(user);
    expect(screen.getByTestId('drill-step')).toHaveTextContent('1 of 1');
    expect(screen.getByTestId('hanzi')).toHaveTextContent('你好吗，朋友');
  });

  it("BEH-PATH-2: Both drills every item, and Start round needs a ticked section", async () => {
    stubApi({ 'GET /api/packs': packs, 'GET /api/drill/learn': greetingCards, 'POST /api/drill/answer': scriptedVerdicts() });
    const user = userEvent.setup();
    renderPage(<Path />, '/path');
    await screen.findByRole('checkbox', { name: 'Drill Greetings' });
    expect(screen.getByRole('button', { name: 'Start round' })).toBeDisabled();
    await tick(user, 'Greetings');
    await startRound(user);
    expect(screen.getByTestId('drill-step')).toHaveTextContent('1 of 3');
  });

  it("BEH-PATH-2: the end of the round says how many were right and how many wrong", async () => {
    stubApi({ 'GET /api/packs': packs, 'GET /api/drill/learn': greetingCards, 'POST /api/drill/answer': scriptedVerdicts('Correct', 'Wrong', 'Almost') });
    const user = userEvent.setup();
    renderPage(<Path />, '/path');
    await tick(user, 'Greetings');
    await startRound(user);
    await askedInPass(user, 3);

    expect(await screen.findByTestId('drill-tally')).toHaveTextContent('1 right, 2 wrong');
    expect(screen.getByRole('button', { name: 'Drill the missed again' })).toBeInTheDocument();
  });

  it("BEH-PATH-2: Review them lists the missed items with their answers", async () => {
    stubApi({ 'GET /api/packs': packs, 'GET /api/drill/learn': greetingCards, 'POST /api/drill/answer': scriptedVerdicts('Wrong', 'Correct', 'Correct') });
    const user = userEvent.setup();
    renderPage(<Path />, '/path');
    await tick(user, 'Greetings');
    await startRound(user);
    const [first] = await askedInPass(user, 3);

    await user.click(await screen.findByRole('button', { name: 'Review them' }));
    const missedList = within(screen.getByRole('region', { name: 'Missed words' }));
    const card = greetingCards.find((c) => c.hanzi === first)!;
    expect(missedList.getAllByRole('listitem')).toHaveLength(1);
    expect(missedList.getByText(new RegExp(card.glosses[0]))).toBeInTheDocument();
  });

  it("BEH-PATH-2: Drill the missed again loops until every missed item is answered right", async () => {
    const calls = stubApi({
      'GET /api/packs': packs,
      'GET /api/drill/learn': greetingCards,
      // Pass 1: two misses. Pass 2: one of those two missed again. Pass 3: right.
      'POST /api/drill/answer': scriptedVerdicts('Wrong', 'Correct', 'Wrong', 'Correct', 'Wrong', 'Correct'),
    });
    const user = userEvent.setup();
    renderPage(<Path />, '/path');
    await tick(user, 'Greetings');
    await startRound(user);

    const pass1 = await askedInPass(user, 3);
    const missed1 = [pass1[0], pass1[2]];
    expect(await screen.findByTestId('drill-tally')).toHaveTextContent('1 right, 2 wrong');

    await user.click(screen.getByRole('button', { name: 'Drill the missed again' }));
    expect(screen.getByTestId('drill-step')).toHaveTextContent('1 of 2');
    const pass2 = await askedInPass(user, 2);
    expect([...pass2].sort()).toEqual([...missed1].sort());
    expect(await screen.findByTestId('drill-tally')).toHaveTextContent('1 right, 1 wrong');

    await user.click(screen.getByRole('button', { name: 'Drill the missed again' }));
    expect(screen.getByTestId('drill-step')).toHaveTextContent('1 of 1');
    expect(screen.getByTestId('hanzi')).toHaveTextContent(pass2[1]);
    await answerOne(user);

    expect(await screen.findByTestId('drill-tally')).toHaveTextContent('1 right, 0 wrong');
    expect(screen.queryByRole('button', { name: 'Drill the missed again' })).not.toBeInTheDocument();
    expect(screen.getByText('Every one right now.')).toBeInTheDocument();
    expect(calls.filter((c) => c.path.endsWith('/api/drill/answer'))).toHaveLength(6);
  });

  it("BEH-PATH-2: after re-drilling, the breakdown shows each item's times wrong in this round", async () => {
    stubApi({
      'GET /api/packs': packs,
      'GET /api/drill/learn': greetingCards,
      'POST /api/drill/answer': scriptedVerdicts('Wrong', 'Correct', 'Correct', 'Wrong', 'Correct'),
    });
    const user = userEvent.setup();
    renderPage(<Path />, '/path');
    await tick(user, 'Greetings');
    await startRound(user);
    const [missedWord] = await askedInPass(user, 3);
    await user.click(await screen.findByRole('button', { name: 'Drill the missed again' }));
    await answerOne(user); // missed again
    await user.click(await screen.findByRole('button', { name: 'Drill the missed again' }));
    await answerOne(user); // right at last

    const rows = within(await screen.findByRole('region', { name: 'Times wrong' })).getAllByRole('listitem');
    expect(rows).toHaveLength(1);
    expect(rows[0]).toHaveTextContent(missedWord);
    expect(rows[0]).toHaveTextContent('wrong 2 times');
  });

  it("BEH-PATH-2: every miss is counted once on this device, and Worst words lists the highest counts first", async () => {
    stubApi({
      'GET /api/packs': packs,
      'GET /api/drill/learn': greetingCards,
      // Pass 1 misses two; pass 2 misses the first of them again.
      'POST /api/drill/answer': scriptedVerdicts('Wrong', 'Wrong', 'Correct', 'Wrong', 'Correct'),
    });
    const user = userEvent.setup();
    const view = renderPage(<Path />, '/path');
    await tick(user, 'Greetings');
    await startRound(user);
    const pass1 = await askedInPass(user, 3);
    await user.click(await screen.findByRole('button', { name: 'Drill the missed again' }));
    const pass2 = await askedInPass(user, 2);
    await user.click(await screen.findByRole('button', { name: 'Back to the path' }));

    const worst = within(await screen.findByRole('region', { name: 'Worst words' })).getAllByRole('listitem');
    expect(worst).toHaveLength(2);
    expect(worst[0]).toHaveTextContent(pass2[0]);
    expect(worst[0]).toHaveTextContent('wrong 2 times');
    expect(worst[1]).toHaveTextContent(pass1.slice(0, 2).find((h) => h !== pass2[0])!);
    expect(worst[1]).toHaveTextContent('wrong 1 time');

    // Kept in the browser, not in component state: a fresh page still lists them.
    view.unmount();
    stubApi({ 'GET /api/packs': packs });
    renderPage(<Path />, '/path');
    const again = within(await screen.findByRole('region', { name: 'Worst words' })).getAllByRole('listitem');
    expect(again[0]).toHaveTextContent('wrong 2 times');
  });

  it("BEH-PATH-2: wrong counts are ordered by count and corrupt storage reads as none", () => {
    localStorage.setItem('lv.path.wrong', '{not json');
    expect(worstWords()).toEqual([]);
    recordWrong({ hanzi: '甲', pinyin: 'jiǎ', glosses: ['a'] });
    recordWrong({ hanzi: '乙', pinyin: 'yǐ', glosses: ['b'] });
    recordWrong({ hanzi: '乙', pinyin: 'yǐ', glosses: ['b'] });
    expect(worstWords().map((w) => [w.hanzi, w.count])).toEqual([['乙', 2], ['甲', 1]]);
    expect(worstWords(1)).toHaveLength(1);
  });
});
