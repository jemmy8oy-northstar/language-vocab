import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterEach, vi } from 'vitest';

// The app calls the API with a relative base ("/language-vocab/api/..."), which a browser resolves
// against the page but Node's Request cannot. Resolve against the jsdom origin, as a browser would.
const NativeRequest = globalThis.Request;
class RelativeRequest extends NativeRequest {
  constructor(input: RequestInfo | URL, init?: RequestInit) {
    super(typeof input === 'string' ? new URL(input, window.location.href).href : input, init);
  }
}
globalThis.Request = RelativeRequest;

afterEach(() => {
  cleanup();
  localStorage.clear();
  vi.unstubAllGlobals();
});
