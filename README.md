# language-vocab

Mandarin vocabulary trainer — typing-based recall with an adaptive vocabulary
pool. Built for learning to **speak and understand** Mandarin: cards show
hanzi + pinyin, answers are typed in English or tone-numbered pinyin
(`ni3 hao3`), and tones are graded softly. See [docs/DESIGN.md](docs/DESIGN.md)
for the full product and architecture design; the original idea is issue #1.

Scaffolded from [web-template](https://github.com/jemmy8oy-northstar/web-template)
(.NET 10 + React 19 monorepo, PostgreSQL, helm chart, ArgoCD deploy via
[oke-fleet](https://github.com/jemmy8oy-northstar/oke-fleet)).

## Layout

- `backend/` — Balenthiran.LanguageVocab.* (WebApi → Services → Abstractions /
  DataModels / DomainModels / EntityModels / Database)
- `frontend/` — Vite + React 19 + Redux Toolkit / RTK Query
- `helm/` — deployment chart (consumed by oke-fleet's ApplicationSet)
- `docs/DESIGN.md` — product/architecture design + bold-assumption log
- `.github/workflows/` — the template's stock CI files, untouched from the
  initial commit: the bot can't push `.github/workflows/**` changes (GitHub
  App has no `workflows` permission by design), so any edits there are
  James's. The docker-build-push workflow already derives image names from
  the repo name, so none are needed for this app.

## Local dev

```bash
node scripts/init.mjs        # generates dev config (JWT secret, conn string, frontend .env), restores deps
cd backend && dotnet build   # requires .NET 10 SDK
cd frontend && npm run dev
```

Postgres connection string lives in the generated (gitignored)
`backend/Balenthiran.LanguageVocab.WebApi/appsettings.Development.json`;
migrations apply automatically on startup when a connection string is present.

## Process

Feature branches → PR into `dev`; `dev` → `main` promotions by James.
This repo is built AI-first with bold assumptions logged in the design doc
(see claude-code-bot#11) rather than the full human-gated 7-phase SDD flow —
process learnings feed back into web-template.
