import type { ReactElement } from 'react';
import { configureStore } from '@reduxjs/toolkit';
import { render } from '@testing-library/react';
import { Provider } from 'react-redux';
import { MemoryRouter } from 'react-router-dom';
import { vi } from 'vitest';
import { emptySplitApi } from '../api/emptyApi';
import '../api/generatedApi'; // injects the endpoints into emptySplitApi

export interface Call {
  path: string;
  method: string;
  body: unknown;
}

/** Stub `fetch` with a router: `routes` maps "METHOD /path" to the JSON it answers. Returns the calls seen. */
export const stubApi = (routes: Record<string, unknown>) => {
  const calls: Call[] = [];
  vi.stubGlobal(
    'fetch',
    vi.fn(async (input: Request | string) => {
      const req = typeof input === 'string' ? new Request(input) : input;
      const path = new URL(req.url).pathname.replace(/\/{2,}/g, '/');
      const text = await req.clone().text();
      calls.push({ path, method: req.method, body: text ? JSON.parse(text) : undefined });
      const key = `${req.method} ${path.replace(/^\/language-vocab/, '')}`;
      if (!(key in routes)) return new Response('not stubbed: ' + key, { status: 404 });
      const answer = routes[key];
      return new Response(JSON.stringify(typeof answer === 'function' ? answer() : answer), { status: 200, headers: { 'Content-Type': 'application/json' } });
    }),
  );
  return calls;
};

/** Render inside the app's real RTK Query api (fresh store per test, so no cache bleeds) and a MemoryRouter. */
export const renderPage = (ui: ReactElement, route = '/') => {
  const store = configureStore({
    reducer: { [emptySplitApi.reducerPath]: emptySplitApi.reducer },
    middleware: (getDefault) => getDefault().concat(emptySplitApi.middleware),
  });
  return render(
    <Provider store={store}>
      <MemoryRouter initialEntries={[route]}>{ui}</MemoryRouter>
    </Provider>,
  );
};
