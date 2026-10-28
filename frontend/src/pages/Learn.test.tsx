import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { renderPage, stubApi } from '../test/harness';
import Learn from './Learn';

const round = [
  { vocabItemId: 'a', hanzi: '你', pinyin: 'nǐ', glosses: ['you'] },
  { vocabItemId: 'b', hanzi: '好', pinyin: 'hǎo', glosses: ['good'] },
  { vocabItemId: 'c', hanzi: '我', pinyin: 'wǒ', glosses: ['I', 'me'] },
  { vocabItemId: 'd', hanzi: '他', pinyin: 'tā', glosses: ['he'] },
  { vocabItemId: 'e', hanzi: '们', pinyin: 'men', glosses: ['plural marker'] },
];

const outcome = { verdict: 'Correct', hanzi: '你', pinyin: 'nǐ', glosses: ['you'], strength: 1, unlocked: 0 };

const routes = () => ({ 'GET /api/drill/learn': round, 'POST /api/drill/answer': outcome });

/** Click through the show phase ("Next" x4, then "Start the quiz"). */
const showAllFive = async (user: ReturnType<typeof userEvent.setup>) => {
  await screen.findByText('你');
  for (let i = 0; i < 4; i++) await user.click(screen.getByRole('button', { name: 'Next' }));
  await user.click(screen.getByRole('button', { name: 'Start the quiz' }));
};

describe('Learn quiz after the five words (BEH-LEARN-2)', () => {
  it('quizzes only the five words just shown, prompting with the pinyin and taking the English', async () => {
    const calls = stubApi(routes());
    const user = userEvent.setup();
    renderPage(<Learn />);
    await showAllFive(user);

    // Prompt is the pinyin; the learner types English.
    expect(screen.getByTestId('learn-step')).toHaveTextContent('Quiz · 1 of 5');
    expect(screen.getByTestId('pinyin')).toHaveTextContent('nǐ');
    expect(screen.getByRole('textbox', { name: 'Answer' })).toHaveAttribute('placeholder', 'Type the English meaning');

    for (let i = 0; i < 5; i++) {
      await user.type(screen.getByRole('textbox', { name: 'Answer' }), 'x{Enter}');
      await within(await screen.findByTestId('verdict')).findByText('Correct');
      await user.keyboard('{Enter}');
    }
    expect(await screen.findByTestId('learn-done')).toHaveTextContent('5 of 5 right');

    const answers = calls.filter((c) => c.path.endsWith('/api/drill/answer'));
    expect(answers.map((c) => (c.body as { vocabItemId: string }).vocabItemId)).toEqual(['a', 'b', 'c', 'd', 'e']);
    expect(answers.every((c) => (c.body as { direction: string }).direction === 'ZhToEn')).toBe(true);
    // Nothing outside the round was fetched: one round, no drill/next.
    expect(calls.filter((c) => c.path.endsWith('/api/drill/learn'))).toHaveLength(1);
    expect(calls.some((c) => c.path.endsWith('/api/drill/next'))).toBe(false);
  });

  it('asks nothing until all five words have been shown', async () => {
    const calls = stubApi(routes());
    const user = userEvent.setup();
    renderPage(<Learn />);
    await screen.findByText('你');
    await user.click(screen.getByRole('button', { name: 'Next' }));

    expect(screen.queryByRole('textbox', { name: 'Answer' })).not.toBeInTheDocument();
    expect(calls.some((c) => c.path.endsWith('/api/drill/answer'))).toBe(false);
  });
});

describe('Learn reverse quiz, English to pinyin (BEH-LEARN-3)', () => {
  it('has the reverse direction switched off by default', async () => {
    stubApi(routes());
    renderPage(<Learn />);
    const toggle = await screen.findByTestId('reverse-toggle');

    expect(toggle).toHaveAttribute('aria-pressed', 'false');
    expect(toggle).toHaveTextContent('off');
  });

  it('prompts in English, hides the word, and sends EnToZh answers once the reverse toggle is on', async () => {
    const calls = stubApi(routes());
    const user = userEvent.setup();
    renderPage(<Learn />);
    await screen.findByText('你');
    await user.click(screen.getByTestId('reverse-toggle'));
    expect(screen.getByTestId('reverse-toggle')).toHaveAttribute('aria-pressed', 'true');
    for (let i = 0; i < 4; i++) await user.click(screen.getByRole('button', { name: 'Next' }));
    await user.click(screen.getByRole('button', { name: 'Start the quiz' }));

    // English prompt; the hanzi and pinyin would give the answer away, so they are not on screen.
    expect(screen.getByTestId('english-prompt')).toHaveTextContent('you');
    expect(screen.queryByTestId('hanzi')).not.toBeInTheDocument();
    expect(screen.queryByTestId('pinyin')).not.toBeInTheDocument();
    expect(screen.getByRole('textbox', { name: 'Answer' })).toHaveAttribute('placeholder', expect.stringMatching(/pinyin/i));

    await user.type(screen.getByRole('textbox', { name: 'Answer' }), 'ni3{Enter}');
    await screen.findByTestId('verdict');

    const answer = calls.find((c) => c.path.endsWith('/api/drill/answer'));
    expect(answer?.body).toEqual({ vocabItemId: 'a', direction: 'EnToZh', answer: 'ni3' });
    // The verdict reveals the word.
    expect(screen.getByTestId('hanzi')).toHaveTextContent('你');
  });
});
