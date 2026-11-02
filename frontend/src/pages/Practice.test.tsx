import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { renderPage, stubApi } from '../test/harness';
import Practice from './Practice';

const zhToEn = (id: string, hanzi: string, pinyin: string, glosses: string[]) => ({
  vocabItemId: id,
  direction: 'ZhToEn',
  hanzi,
  pinyin,
  glosses,
});
const enToZh = { vocabItemId: 'z', direction: 'EnToZh', hanzi: null, pinyin: null, glosses: ['you'] };

const outcome = (verdict: string) => ({ verdict, hanzi: '你', pinyin: 'nǐ', glosses: ['you'], strength: 2, unlocked: 0 });

describe('Drill card feedback (BEH-CARD-1)', () => {
  it('shows a green "Correct" verdict after a right answer', async () => {
    stubApi({ 'GET /api/drill/next': zhToEn('a', '你', 'nǐ', ['you']), 'POST /api/drill/answer': outcome('Correct') });
    const user = userEvent.setup();
    renderPage(<Practice />);
    await screen.findByText('你');
    await user.type(screen.getByRole('textbox', { name: 'Answer' }), 'you{Enter}');

    const label = await within(await screen.findByTestId('verdict')).findByText('Correct');
    expect(label).toHaveStyle({ color: '#22c55e' });
  });

  it('shows an amber "Almost" verdict, pointing at the tones, after right syllables with wrong tones', async () => {
    stubApi({ 'GET /api/drill/next': enToZh, 'POST /api/drill/answer': outcome('Almost') });
    const user = userEvent.setup();
    renderPage(<Practice />);
    await screen.findByTestId('english-prompt');
    await user.type(screen.getByRole('textbox', { name: 'Answer' }), 'ni2{Enter}');

    const label = await within(await screen.findByTestId('verdict')).findByText(/Almost/);
    expect(label).toHaveTextContent('check the tones');
    expect(label).toHaveStyle({ color: '#f59e0b' });
    // The correct tones are shown: the pinyin is revealed after the verdict.
    expect(screen.getByTestId('pinyin')).toHaveTextContent('nǐ');
  });

  it('shows a red verdict with the right answer after a wrong answer', async () => {
    stubApi({ 'GET /api/drill/next': zhToEn('a', '你', 'nǐ', ['you']), 'POST /api/drill/answer': outcome('Wrong') });
    const user = userEvent.setup();
    renderPage(<Practice />);
    await screen.findByText('你');
    await user.type(screen.getByRole('textbox', { name: 'Answer' }), 'banana{Enter}');

    const verdict = await screen.findByTestId('verdict');
    expect(within(verdict).getByText('Not quite')).toHaveStyle({ color: '#f87171' });
    expect(verdict).toHaveTextContent('you'); // the accepted answer
  });

  it('moves on to the next card when Enter is pressed after the verdict', async () => {
    const cards = [zhToEn('a', '你', 'nǐ', ['you']), zhToEn('b', '好', 'hǎo', ['good'])];
    let served = 0;
    const calls = stubApi({
      'GET /api/drill/next': () => cards[Math.min(served++, cards.length - 1)],
      'POST /api/drill/answer': outcome('Correct'),
    });
    const user = userEvent.setup();
    renderPage(<Practice />);
    await screen.findByText('你');

    await user.type(screen.getByRole('textbox', { name: 'Answer' }), 'you{Enter}');
    await screen.findByTestId('verdict');
    expect(screen.getByText('你')).toBeInTheDocument(); // still on the same card: Enter #1 only graded it

    await user.keyboard('{Enter}');
    expect(await screen.findByText('好')).toBeInTheDocument();
    expect(screen.queryByTestId('verdict')).not.toBeInTheDocument();
    expect(screen.getByRole('textbox', { name: 'Answer' })).toHaveValue('');
    expect(calls.filter((c) => c.path.endsWith('/api/drill/answer'))).toHaveLength(1);
  });
});

describe('Audio on a card (BEH-AUDIO-1)', () => {
  it('speaks the word through the browser speechSynthesis in zh-CN, with no network call for audio', async () => {
    const speak = vi.fn();
    const cancel = vi.fn();
    vi.stubGlobal('speechSynthesis', { speak, cancel });
    vi.stubGlobal('SpeechSynthesisUtterance', class { text: string; lang = ''; constructor(text: string) { this.text = text; } });
    const calls = stubApi({ 'GET /api/drill/next': zhToEn('a', '你好', 'nǐ hǎo', ['hello']) });
    const user = userEvent.setup();
    renderPage(<Practice />);
    await screen.findByText('你好');
    const before = calls.length;

    await user.click(screen.getByRole('button', { name: 'Play audio' }));

    expect(speak).toHaveBeenCalledTimes(1);
    const utterance = speak.mock.calls[0][0] as { text: string; lang: string };
    expect(utterance.lang).toBe('zh-CN');
    expect(utterance.text).toBe('你好');
    expect(calls).toHaveLength(before); // no request was made to produce the audio
  });
});

describe('Chinese characters are displayed and never typed (BEH-CHAR-1)', () => {
  it('displays the hanzi large on a Chinese-to-English card, and the answer box asks for English, not hanzi', async () => {
    stubApi({ 'GET /api/drill/next': zhToEn('a', '你', 'nǐ', ['you']) });
    renderPage(<Practice />);

    const hanzi = await screen.findByTestId('hanzi');
    expect(hanzi).toHaveTextContent('你');
    expect(hanzi).toHaveAttribute('lang', 'zh-CN');
    expect(screen.getByRole('textbox', { name: 'Answer' })).toHaveAttribute('placeholder', 'Type the English meaning');
  });

  it('keeps the hanzi off an English-to-pinyin card until it is answered, then shows it; the answer box asks for pinyin, never hanzi', async () => {
    stubApi({ 'GET /api/drill/next': enToZh, 'POST /api/drill/answer': outcome('Correct') });
    const user = userEvent.setup();
    renderPage(<Practice />);

    await screen.findByTestId('english-prompt');
    // Tension with BEH-CHAR-1 "always displayed": the hanzi is hidden here on purpose, as it would give the pinyin away.
    expect(screen.queryByTestId('hanzi')).not.toBeInTheDocument();
    const input = screen.getByRole('textbox', { name: 'Answer' });
    expect(input).toHaveAttribute('placeholder', expect.stringMatching(/pinyin/i));
    expect(input.getAttribute('placeholder')).not.toMatch(/hanzi|character/i);

    await user.type(input, 'ni3{Enter}');
    expect(await screen.findByTestId('hanzi')).toHaveTextContent('你');
  });
});
