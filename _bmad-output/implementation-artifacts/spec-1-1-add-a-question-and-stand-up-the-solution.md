---
title: '1-1 Add a Question (and stand up the solution)'
type: 'feature'
created: '2026-10-02'
status: 'in-review'
baseline_commit: 'd9e4e01d73edd90376b92b2cbc05c8a6b76f8579'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The repo has only planning artifacts. There is no app, so Simon cannot add a Question or see any end-to-end slice.

**Approach:** Scaffold the .NET 10 backend (Core/Infrastructure/Api) and the Vite React-TS frontend, then deliver one vertical slice: the Add a Question form on `/question-bank` persisting through `POST /api/question-bank/questions` to SQLite, with the Header/nav shell, design tokens and shared components later stories reuse.

## Boundaries & Constraints

**Always:** Follow ARCHITECTURE-SPINE AD-1/2/7/14/15/16/17/18 as summarised in the epic context. Core references only `Microsoft.Extensions.AI`; the embedding adapter (default `ollama`, `nomic-embed-text`, with its prefix decorator) lives in Infrastructure and is built only in the Api composition root. Normalize, then embed, then insert; embedding failure saves nothing. Components use only `var(--color-...)` tokens. Copy is verbatim from EXPERIENCE.md. Stack versions are the epic context pins. Tests use fakes, never a real model.

**Never:** No list/delete/import UI or endpoints, no reconciler, no `QuestionBankState`/`QuestionIndex` (Story 1.2), no OpenAI adapter, no interview or chat features, no UI kit or server-state library, no logging of Question text or request bodies, no config-returning endpoint.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Add ok | text, optional reference answer | `201` with created Question; toast "Question added."; form cleared | N/A |
| Empty text | blank or whitespace | "Enter a Question first." under the field, focus on it, no API call | N/A |
| Duplicate | normalized text exists | `409 duplicate-question`; inline error, text kept | no retry button |
| Provider down | embedder throws | `503 provider-unavailable`; inline error with Retry, text kept; nothing saved | Retry re-submits |
| Bad Origin | non-GET, `Origin` not allowed or missing | `403 invalid-request` | N/A |

</frozen-after-approval>

## Code Map

- `backend/InterviewPrepBot.slnx`, `backend/src/InterviewPrepBot.{Core,Infrastructure,Api}`, `backend/tests/InterviewPrepBot.{Core,Infrastructure,Api}.Tests` -- new solution; Core has `QuestionText.Normalize`, `Question`, `IQuestionBank` (add only), `IQuestionEmbedder` (text + `document`/`query` intent), `AddQuestion` use case.
- `Infrastructure` -- EF Core SQLite `AppDbContext`, initial migration (`question` table, unique `normalized_text`), `QuestionBank` (embed, then transactional insert), Ollama embedder plus prefix decorator.
- `Api/Program.cs` -- composition root, options with `ValidateOnStart`, ProblemDetails mapping, Origin check, loopback binding, `AllowedHosts=localhost`, `appsettings.json` (`Providers`, `Storage`, `Security`).
- `frontend/` (Vite react-ts) -- `src/styles/tokens.css`, `src/components/{Header,ThemeToggle,Button,FormField,Toast,InlineError,EmptyState}`, `src/api/{questionBank.ts,types.ts}`, `src/pages/{Interview(placeholder),QuestionBank}.tsx`, `vite.config.ts` proxies `/api`.
- `.gitignore` -- add `data/`, `bin/`, `obj/`, `node_modules/`, `dist/`. `README.md` -- run steps, Ollama pull. `_bmad-output/planning-artifacts/ux-designs/**/EXPERIENCE.md` -- add `duplicate-question` copy.

## Tasks & Acceptance

**Execution:**
- [x] `backend/**` -- scaffold three projects plus tests, implement Core, Infrastructure and Api per Code Map, including the migration and security middleware -- AD-1/7/14/15/17
- [x] `backend/tests/**` -- test normalization, the add flow with fake embedder (ok, duplicate, embed failure saves nothing), endpoint statuses and Origin rule, and migration creating `question` -- the I/O matrix
- [x] `frontend/**` -- scaffold Vite react-ts, add react-router and Vitest/RTL, tokens, shell, shared components, `/` redirect, Add form on `/question-bank` -- UX-DR1-5, 7-10, 12, 13
- [x] `frontend/src/**/*.test.tsx` -- test the Add form against a mocked `src/api` (empty, success toast, duplicate, failure with Retry), theme toggle cycle -- AC coverage
- [x] `.gitignore`, `README.md`, `EXPERIENCE.md` -- as in Code Map

**Acceptance Criteria:**
- Given the dev servers run, when the SPA loads, then the header shows the product name from 40rem up, the Interview and Question Bank links and the Theme toggle, `/` redirects to `/interview`, and `/api` proxies to Kestrel.
- Given the backend starts, when migrations run, then `data/interview-prep.db` exists with the `question` table and is git-ignored.
- Given default config, when the app starts, then the embedder resolves to `ollama`/`nomic-embed-text`, and Core has no project reference to Infrastructure/Api and no package beyond `Microsoft.Extensions.AI`.
- Given the cross-cutting UI criteria, when the shell renders, then both themes meet contrast, focus ring is 2px, controls are at least 44px, and nothing scrolls horizontally at 200% zoom.

## Implementation Notes

- Implemented inline (no subagent). Backend: 22 tests pass (`dotnet test --solution`, MTP runner opted in via `backend/global.json`; xunit.v3 has no `dotnet new` template here, test csprojs written by hand). Frontend: 13 Vitest tests pass, `npm run build` and `oxlint` clean.
- Smoke-tested live: POST via Kestrel and via the Vite proxy; DB created at `data/interview-prep.db`; no Ollama installed, so a real add returns `503 provider-unavailable` as designed.
- Choices: duplicate pre-check before embedding (saves a Provider call; the `ON CONFLICT` insert still guards races); `Storage:Path` resolves against the content root, Development overrides it to the repo-root `data/`; `internal-error` slug for unhandled 500s; extra copy rows added to EXPERIENCE.md (duplicate, generic add failure).
- Added `Dimensions` to embedding options so the profile string `{type}:{model}:{dims}:{promptScheme}` is complete.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `dotnet test backend/InterviewPrepBot.slnx` -- expected: all pass
- `npm --prefix frontend test -- --run` and `npm --prefix frontend run build` -- expected: pass, no type errors

**Manual checks (if no CLI):**
- With Ollama running, add a Question in the browser and confirm the row in `data/interview-prep.db`; toggle themes and resize to phone width.
