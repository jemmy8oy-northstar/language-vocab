import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';
import { renderPage, stubApi } from '../test/harness';
import { creditRound } from '../pathProgress';
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
