import type { AnswerVerdict } from './api/generatedApi';

// Feedback colours (MVP §5): green, amber with the correct tones, red with the answer.
export const verdictStyle: Record<AnswerVerdict, { colour: string; label: string }> = {
  Correct: { colour: '#22c55e', label: 'Correct' },
  Almost: { colour: '#f59e0b', label: 'Almost — check the tones' },
  Wrong: { colour: '#f87171', label: 'Not quite' },
};
