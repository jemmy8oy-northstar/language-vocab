/** Browser speech, zh-CN voice: no API key, no network call (A5). */
export const speak = (text: string) => {
  if (!('speechSynthesis' in window)) return;
  const utterance = new SpeechSynthesisUtterance(text);
  utterance.lang = 'zh-CN';
  window.speechSynthesis.cancel();
  window.speechSynthesis.speak(utterance);
};
