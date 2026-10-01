---
stepsCompleted: [1, 2, 3]
inputDocuments:
  - _bmad-output/planning-artifacts/prds/prd-ai-interview-prep-bot-2026-09-21/prd.md
  - _bmad-output/planning-artifacts/prds/prd-ai-interview-prep-bot-2026-09-21/addendum.md
  - _bmad-output/planning-artifacts/architecture/architecture-ai-interview-prep-bot-2026-09-22/ARCHITECTURE-SPINE.md
  - _bmad-output/planning-artifacts/architecture/architecture-ai-interview-prep-bot-2026-09-22/architecture-explainer.html
  - _bmad-output/planning-artifacts/ux-designs/ux-ai-interview-prep-bot-2026-09-21/DESIGN.md
  - _bmad-output/planning-artifacts/ux-designs/ux-ai-interview-prep-bot-2026-09-21/EXPERIENCE.md
  - _bmad-output/planning-artifacts/briefs/brief-ai-interview-prep-bot-2026-09-21/brief.md
  - _bmad-output/planning-artifacts/briefs/brief-ai-interview-prep-bot-2026-09-21/addendum.md
---

# ai-interview-prep-bot - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for ai-interview-prep-bot, decomposing the requirements from the PRD, UX Design (DESIGN.md + EXPERIENCE.md), and Architecture (ARCHITECTURE-SPINE.md) into implementable stories.

## Requirements Inventory

### Functional Requirements

**4.1 Question Bank management**

FR-1: Import Questions from a file. A user can upload a Word, PDF or plain text file containing many Questions, with or without Reference Answers.
- After import, the app reports the number of Questions added and lists anything it couldn't read.
- Imported Questions are available to the next Interview Session.
- Reference Answers found in the file are stored with their Question.
- A file laid out like the sample bank imports every Question with its Reference Answer: each Question starts with "Q" plus a number and a period (Q1., Q2., …), and its answer is everything up to the next Question. The sample file yields 87 Questions.
- Text before the first Question (title, links list) is ignored.
- `[ASSUMPTION]` PDF and plain text files use the same "Q<number>." pattern; other layouts are best-effort.

FR-2: Add a single Question. A user can enter one Question, with an optional Reference Answer, in the app.
- The new Question is available to the next Interview Session.

FR-3: View and delete Questions. A user can list the Questions in the Question Bank and delete any of them. `[ASSUMPTION]` Edit is deferred; delete and re-add instead.
- The list shows every Question in the Question Bank.
- A deleted Question no longer appears in the list and is never asked in a later Interview Session.

**4.2 Resume upload**

FR-4: Upload a Resume. An Interviewee can upload a Resume as a Word or PDF file to start an Interview Session.
- The text is extracted and used in that Interview Session only.
- If no text can be extracted, the user is told and asked for another file.
- The Resume is not stored after the Interview Session ends.

**4.3 Interview Session**

FR-5: Ask a mix of Resume and Question Bank questions. The bot asks Questions drawn from the Resume and Questions retrieved from the Question Bank, chosen for relevance to the Resume and the conversation so far.
- A session contains at least one Resume-based question and at least one Question Bank Question. `[ASSUMPTION]` An Interview Session is about 10 questions.
- Behavioral questions ("Tell me about a time…") are generated from something specific on the Resume. `[ASSUMPTION]` Behavioral questions come from the Resume in v1, not the Question Bank.
- If the Question Bank is empty, the bot still runs using Resume-based questions and tells the user.

FR-6: Follow up on answers. When an answer is thin or interesting, the bot can ask a follow-up before moving on.
- A follow-up refers to something the user actually said in that answer.
- The bot asks at most one follow-up per Question. `[ASSUMPTION]`

FR-7: End the session. The session ends after the planned number of questions, or when the user ends it early.
- When the session ends, the user is taken to the Coaching Report.

**4.4 Coaching**

FR-8: Produce a Coaching Report. At the end of an Interview Session the bot produces a Coaching Report.
- The report names at least one strength and one weak spot, each referring to one of the user's actual answers.
- Where a Question has a Reference Answer, the report compares the user's answer to it.
- Where there is no Reference Answer, the report judges the answer against a simple rubric (situation, action, result for behavioral). `[ASSUMPTION]`
- If a session ends early, the report covers the answers given.

**4.5 Swappable providers**

FR-9: Choose Providers by configuration. The chat Provider and the embedding Provider can each be changed through configuration, with no code changes.
- The default configuration uses local models and needs no paid account or API key.
- Changing the configuration to a paid Provider runs the same Interview Session without code changes.

FR-10: Rebuild embeddings on change. When the embedding Provider changes, the Question Bank can be re-embedded so retrieval keeps working.
- After a re-embed, retrieval returns Questions from the whole Question Bank, including those imported before the change.

### NonFunctional Requirements

NFR-16GB: Runs on a 16 GB machine using small local models (roughly 3B–8B parameters, quantized).

NFR-secrets: API keys and other secrets never reach the browser.

NFR-resume-deleted: The Resume is deleted when the Interview Session ends.

NFR-streaming: Chat responses stream to the user as they are generated, so the user is not left on a blank screen. `[ASSUMPTION]`

### Additional Requirements

From ARCHITECTURE-SPINE.md (binding Architecture Decisions AD-1 to AD-18):

- **Starter template (Epic 1 Story 1 impact):** Frontend scaffolded with `npm create vite@latest -- --template react-ts`, then `react-router`, Vitest and React Testing Library added by hand. No UI kit, no server-state library. Backend scaffolded as three .NET 10 projects under `backend/`: `InterviewPrepBot.Core`, `InterviewPrepBot.Infrastructure`, `InterviewPrepBot.Api`, plus `tests/InterviewPrepBot.*.Tests`.
- **Layering (AD-1):** Dependencies point inward only (Api → Infrastructure, Api → Core, Infrastructure → Core). Core takes no NuGet dependency beyond `Microsoft.Extensions.AI`. Endpoints only parse HTTP, call one Core use case, map to a DTO. Ports are interfaces in Core, implemented in Infrastructure.
- **Provider abstraction (AD-2):** All model calls go through `IChatClient` and `IEmbeddingGenerator<string, Embedding<float>>`, chosen independently by config (`Providers:Chat:Type`, `Providers:Embedding:Type`) and constructed in the Api composition root. v1 adapters: `ollama` (default) and `openai` (also covers Azure OpenAI / OpenAI-compatible endpoints).
- **Interview state machine in Core (AD-3):** Core owns question count, source mix, follow-up cap and session end. Slot plan fixed at session creation (`Interview:QuestionCount`, default 10); bank slot's Question selected lazily at generation time. `questionNo` is 1-based; a follow-up shares its parent's `questionNo`. Every asked Question is snapshotted into the transcript (text + Reference Answer).
- **Session lifecycle (AD-4):** `ISessionStore` (singleton, in-memory). Resume text/ResumeProfile purged when the interview ends; transcript/cached report purged when the session is deleted or expires. `DELETE /api/interview/sessions/{id}` (idempotent, 204), called on "Start new Interview Session" and best-effort on `pagehide`. Idle expiry `Sessions:IdleMinutes` (default 60), refreshed per request; in-flight ops hold a lease.
- **Turn protocol (AD-5):** Per-session lock (`409 session-busy` / `409 invalid-session-state`). Client-generated `turnId` (GUID) makes turns idempotent; retry replays the stored decision and only regenerates wording. Stop = client aborts, `CancellationToken` reaches the model call. Closing turn after the last planned Question uses fixed Core text, no model call.
- **API contract and streaming (AD-6):** REST JSON under `/api`, camelCase, ISO-8601 UTC. Chat turns stream as `text/event-stream` via `TypedResults.ServerSentEvents`, read with `fetch` + `ReadableStream`. SSE events: `token {text}`, `done {questionNo, plannedCount, kind, source, sessionComplete}`, `error {type}`. Fixed endpoint table: `GET /api/question-bank`, `POST /api/question-bank/questions`, `DELETE /api/question-bank/questions/{id}`, `POST /api/question-bank/import`, `POST /api/admin/re-embed`, `POST /api/interview/sessions`, `POST /api/interview/sessions/{id}/turns`, `POST /api/interview/sessions/{id}/end`, `POST /api/interview/sessions/{id}/report`, `DELETE /api/interview/sessions/{id}`.
- **Persistence (AD-7):** Single SQLite file via EF Core, `Storage:Path` default `data/interview-prep.db`, git-ignored. Schema managed by EF Core migrations applied at startup. Only `IQuestionBank` writes Questions. Table `question`: `id`, `text`, `normalized_text` (unique), `reference_answer`, `embedding` (BLOB), `embedding_profile`, `created_at` (UTC).
- **Retrieval (AD-8):** `IQuestionBank` owns an immutable `QuestionIndex` snapshot, replaced atomically after every write. `IQuestionRetriever` is a pure function: cosine similarity, top-k, excluding already-asked Questions. `QuestionBankState`: `Empty` / `Ready` / `NotIndexed` (session creation waits for one reconcile, else fails `provider-unavailable`). Only Question text is embedded, never Reference Answers.
- **Embedding profile and reconciler (AD-9):** Profile string `{providerType}:{model}:{dimensions}:{promptScheme}`. `IEmbeddingReconciler` is the single owner of re-embedding, runs at startup and from `POST /api/admin/re-embed`. No UI for it in v1.
- **Provider quirks isolation (AD-10):** Core embeds via `IQuestionEmbedder` with plain text + intent (`document`/`query`). Model-specific handling (e.g. `nomic-embed-text` prefixes) lives in an Infrastructure decorator.
- **Typed, validated model output (AD-11):** Any machine-read output uses `IChatClient.GetResponseAsync<T>` with a Core record. Coaching Report record: `endedEarly`, `answeredCount`, `summary`, strengths, weak spots, per-Question reviews (`basis`: `reference`/`rubric`). Deterministic Core validator requires ≥1 strength, ≥1 weak spot, every `questionNo` valid, every quote traceable to the answer (normalized comparison). One retry on validation failure, then `invalid-model-output`.
- **Prompt budgets (AD-12):** Chat adapter sets `Providers:Chat:ContextTokens` (default 8192) explicitly. Interview-turn prompt: system prompt, Resume text/ResumeProfile, current slot's Question text only (never its Reference Answer), last `Interview:HistoryTurns` (default 6). Resume over `Interview:MaxResumeChars` (default 12000) rejected with `file-too-large`. Coaching: one structured review call per answered Question (cached, so Retry redoes only failed calls), then one synthesis call. Untrusted text placed in delimited prompt blocks, never treated as instructions.
- **Document extraction (AD-13):** `IDocumentTextExtractor` Core port; Infrastructure adapters for Word (DocumentFormat.OpenXml), PDF (PdfPig `ContentOrderTextExtractor`), plain text (UTF-8). File type detected from content, not client content-type. Resume allows Word/PDF only; import allows Word/PDF/text. Import parsing is a pure Core function over extracted text (`Q<number>.` pattern; a line with no text after it goes to `unreadable` with its line number). A test pins the committed sample bank at 87 Questions, each with a Reference Answer.
- **Import/add semantics (AD-14):** `QuestionText.Normalize` (trim, collapse whitespace, case-fold) used by parser, single add and the unique index. Embed first (outside a transaction), then insert in one transaction with `ON CONFLICT DO NOTHING` on `normalized_text`; conflicts counted as `alreadyInBank`. Embedding failure saves nothing, returns a retryable error. Single add of an existing Question returns `409 duplicate-question`. `found` = Questions the parser recognised (UI shows "No Questions found." only when 0). Uploads over `Uploads:MaxBytes` (default 10 MB) rejected before parsing with `file-too-large`.
- **Error shape (AD-15):** RFC 9457 ProblemDetails with stable `type` slugs: `provider-unavailable`, `invalid-model-output`, `unreadable-document`, `unsupported-file-type`, `file-too-large`, `session-not-found`, `session-busy`, `invalid-session-state`, `duplicate-question`, `invalid-request`. Frontend maps each slug to its own copy. Exception text logged without content, never returned.
- **Frontend structure (AD-16):** One `InterviewProvider` above `<Routes>` owns `sessionId`, messages, in-flight `AbortController`, report (route changes never abort a stream). `src/api/` holds one function per AD-6 endpoint, relative `/api/...` URLs only; `src/api/types.ts` is the hand-written DTO mirror. POST turns never auto-retried; user's Retry re-sends the same `turnId`. Dev: Vite proxies `/api` to Kestrel (no CORS config needed).
- **Secrets and exposure (AD-17):** API keys from .NET user-secrets (dev) / environment variables (elsewhere), bound to validated options (`ValidateOnStart`). No endpoint returns configuration. Kestrel binds loopback only, `AllowedHosts` = `localhost`; every non-GET request must carry an `Origin` in `Security:AllowedOrigins` or gets `403 invalid-request`. Resume text, ResumeProfile, answers, imported text, Reference Answers never logged; request logging never records bodies. Hosted-Provider data egress documented in README, not blocked.
- **Test seam (AD-18):** Backend tests use fake `IChatClient`/`IEmbeddingGenerator`, never call a real model; live-model checks are separate and opt-in. Frontend tests run against a mocked `src/api` module.
- **Config sections:** `appsettings.json` + user-secrets; `Providers:Chat` / `Providers:Embedding` (`Type`, `Model`, `Endpoint`, `ApiKey`, chat also `ContextTokens`), `Interview`, `Sessions`, `Storage`, `Uploads`, `Security`.
- **Stack pins:** .NET SDK/target 10.0.401/net10.0; ASP.NET Core minimal APIs 10.0; Microsoft.Extensions.AI(.Abstractions) 10.10.0; OllamaSharp 5.4.30; Microsoft.Extensions.AI.OpenAI 10.10.0; Microsoft.EntityFrameworkCore.Sqlite 10.0.12; DocumentFormat.OpenXml 3.5.1; PdfPig 0.1.16; xunit.v3 4.0.1; Node.js/npm 24 LTS/11.6.2; React/react-dom 19.3.0; TypeScript ~6.0.2 (7.0.2 also verified); Vite/@vitejs/plugin-react 8.3.0/6.1.1; react-router 8.4.0; Vitest 5.0.1; Ollama 0.34.2 (not yet installed); default chat model `gemma3:4b`; default embedding model `nomic-embed-text` (768 dims).
- **Repo layout:** `backend/InterviewPrepBot.slnx`, `backend/src/InterviewPrepBot.{Core,Infrastructure,Api}`, `backend/tests/InterviewPrepBot.*.Tests` (with `tests/fixtures` holding the sample bank copy), `frontend/` (Vite react-ts), `data/` (git-ignored SQLite), `docs/`.
- **First-run setup:** Ollama model pull/warm-up steps belong in the repo README, including an `ollama ps` check that the chat model fits the GPU, with `qwen3:4b` (or smaller) as the fallback model.

### UX Design Requirements

From DESIGN.md (visual system) and EXPERIENCE.md (behavior), bmad-ux spine pair:

**Design tokens**

UX-DR1: Implement the full color token system (light + dark sets, identical property names) as CSS custom properties declared once on `:root`; redefine the dark values under `@media (prefers-color-scheme: dark) { :root:not([data-theme="light"]) {...} }` and again under `:root[data-theme="dark"] {...}` so a saved manual choice beats the system. Components reference only `var(--color-...)`, never raw hex. Meet the documented contrast targets in both themes.

UX-DR2: Implement typography tokens (display, title, body, body-sm, label, meta) as `--type-<role>-<property>` custom properties, system font stack only (no web fonts), 16px minimum for body text and form inputs (stops iOS zoom-on-focus), sentence case everywhere, no all-caps/italics for emphasis.

UX-DR3: Implement the 4px-based spacing scale (`--space-1` … `--space-8`) plus named spacing tokens (`gutter-mobile`, `gutter-wide`, `column-max`, `header-height`, `strip-height`, `touch-target`, `bp-md`, `bp-lg` as literals for media queries) and the rounded-corner scale (`sm`, `md`, `lg`, `full`).

UX-DR4: Single centered column layout, max `{spacing.column-max}` wide, on every view at every width; mobile-first base rules with `min-width` media queries at `bp-md` (40rem, wider gutters) and `bp-lg` (64rem, extra top padding); never a sidebar or multi-column layout.

**Information architecture & routing**

UX-DR5: Implement client-side routes: `/` (redirect to `/interview`), `/interview` (Resume upload → Interview Session), `/interview/report` (Coaching Report, separate scrollable view — redirects to `/interview` if no report is in memory), `/question-bank` (import, add, list).

UX-DR6: Interview Session and Coaching Report state live in app memory (React state/Context, AD-16 `InterviewProvider`): navigating between Question Bank and Interview preserves the in-progress session; a page reload ends the session and discards the Resume, transcript and report.

**Global chrome**

UX-DR7: Header / nav component — sticky; product name shown from `bp-md` up; nav links "Interview" and "Question Bank" with `aria-current="page"` on the active link (accent-ink text, 2px accent underline) vs. `ink-secondary` inactive; Theme toggle at the right; every target ≥ `{spacing.touch-target}`; route change moves focus to the new view's `h1`.

UX-DR8: Theme toggle component — one button cycling System → Light → Dark → System; default System (follows `prefers-color-scheme`, live); Light/Dark overrides persist to `localStorage` (wrapped in try/catch, default System theme on failure) and clear on System; applies instantly; accessible name states current mode and the next action (e.g. "Theme: Dark. Switch to System.").

**Shared controls**

UX-DR9: Button component (primary / secondary / destructive variants) — one primary action per view; destructive variant used only inside the Confirm dialog; height ≥ `{spacing.touch-target}`; busy state shows a busy label and ignores a second press; 2px `focus-ring` outline with 2px offset on focus.

UX-DR10: Form field component — visible label above the control; validated on submit; error linked via `aria-describedby` with the message below the field in `{colors.danger}`; first invalid field receives focus.

UX-DR11: Confirm dialog component — `role="alertdialog"`, max 24rem wide, over the scrim; initial focus on Cancel; focus trapped; Esc cancels; names the target item in a plain sentence; after a destructive action focus returns to the list (next row, else the heading); no undo in v1; buttons right-aligned on desktop, full-width stacked below `bp-md`; reused (non-destructive) for the End-interview confirmation.

UX-DR12: Toast / inline error components — inline errors are `role="alert"`, never auto-dismiss, show a Retry button where retry applies, placed where the failure happened; confirmation toasts (e.g. "Question added.", "Question deleted.") are `role="status"` and auto-dismiss after 5 seconds; never convey state by color alone.

UX-DR13: Empty state component — centered `ink-secondary` text, one sentence plus at most one action, no illustration.

**Interview surface**

UX-DR14: Upload zone component — click, keyboard (visible "Choose file" button) or drag-and-drop, one file at a time; Resume accepts Word/PDF only, import accepts Word/PDF/plain text with the visible hint "Each Question starts with Q1., Q2., and so on. The answer is the text up to the next Question."; wrong file type rejected inline before upload; drag-over state (`accent-subtle` fill, `accent` border); uploading state shows filename + "Reading...", second upload blocked.

UX-DR15: Chat bubble components (bot / interviewee) — max 85% column width; bot left-aligned (`surface-raised` + hairline), interviewee right-aligned (`accent-subtle`, no border); streaming text grows in place (no caret needed); auto-scroll only while the reader is at the bottom, otherwise a "Jump to latest" text button; sent answers are not editable in v1.

UX-DR16: Composer component — bottom-fixed bar, top hairline; auto-growing textarea (1–5 lines); on desktop Enter sends and Shift+Enter adds a line, on touch Enter adds a line and a Send button sends; empty input cannot send; disabled while the bot is replying; Send becomes a secondary Stop button while streaming; a draft survives a failed send; `enterkeyhint="send"`.

UX-DR17: Thinking indicator component — three pulsing dots + "Thinking..." on the bot side, appears when an answer is sent until the first streamed token or an error; after a (build-tuned) hang threshold, adds "This is taking longer than usual." with Stop and Retry actions; `role="status"`; reduced-motion variant shows static dots with text only.

UX-DR18: Session strip component — sticky bar under the header, Interview Session only; left: "Question N of about 10" (N counts Questions, not follow-ups); right: Show-question-type switch (`role="switch"`, 36×20px track, visible text label, persists in `localStorage`) and a secondary "End interview" button, disabled until one answer has been sent; wraps to two rows when it doesn't fit.

UX-DR19: Question-type label component — small tag above a bot Question bubble, hidden by default and revealed by the session-strip switch; shows the Question's source (Resume or Question Bank; behavioral Questions read "Resume"); applies retroactively to earlier Questions when toggled on; absent on follow-ups.

**Coaching Report surface**

UX-DR20: Coaching Report card components — three variants (Strength: `success-subtle`/`success` border/"Strength" label; Weak spot: `caution-subtle`/`caution`/"Weak spot" label; Question review: `surface-raised` with hairline, shows the Question, the answer given, then the Reference Answer or rubric feedback); each strength/weak-spot card ends with a quoted answer excerpt and a "See the answer" text link that scrolls to and focuses the matching Question-review card; no numeric score anywhere.

**Question Bank surface**

UX-DR21: Question Bank row component — full-width row with bottom hairline, ≥ `{spacing.touch-target}` tall; Question text clamped to 2 lines in `{typography.body}`; "Reference Answer" disclosure expands in place when one exists; secondary Delete button at the right (drops below the text on narrow widths), opens the Confirm dialog naming the Question.

UX-DR22: Import result panel component — appears under the upload zone after import, stays until dismissed or the next import; heading with the added count in `{colors.success}` (e.g. "42 Questions added."); if parts were skipped, a second "Could not read" heading with a plain list of skipped items (line/Question number + reason); dismiss link at the bottom.

**Voice & copy**

UX-DR23: Implement the fixed microcopy table verbatim for: upload prompt/privacy line, unreadable Resume, empty Question Bank, waiting/hang states, Provider failure, end-early confirmation, report generating/failure, import result/zero-found, delete confirmation — calm, plain, short sentences, first person for the bot inside chat, no exclamation marks/emoji/praise inflation/numeric scores, and explicitly avoid generic copy like "Oops! Something went wrong!", "Invalid file.", "Great job!", "Score: 7/10".

**Accessibility**

UX-DR24: WCAG 2.2 AA contrast in both themes per the DESIGN.md targets (body text ≥4.5:1, `on-accent` on `accent` ≥6.3:1 light/9.1:1 dark, meaning-ink on subtle fill ≥5.5:1, `border-strong`/`focus-ring` ≥3:1); semantic HTML (`header`, `nav`, `main`, real headings, real buttons/controls).

UX-DR25: Full keyboard operability including the Upload zone and the type-switch (`role="switch"`); Tab follows reading order; Enter/Space activate buttons and the switch; Esc closes dialogs; visible 2px `focus-ring` outline everywhere a control can receive focus.

UX-DR26: Chat log is `role="log"` with `aria-live="polite"` and `aria-busy` while streaming, so screen readers hear finished messages rather than individual tokens.

UX-DR27: `prefers-reduced-motion: reduce` respected everywhere (motion limited to opacity fades under 200ms); the app is usable at 200% browser zoom with no horizontal scroll.

**Responsive behavior**

UX-DR28: Implement the three-breakpoint responsive table: below `bp-md` — `gutter-mobile` gutters, header shows only nav links + Theme toggle (no product name), Question Bank row's Delete drops below the text, Confirm dialog buttons stack full-width, session strip may wrap; `bp-md`–`bp-lg` — `gutter-wide` gutters, product name shown, dialog buttons side by side; `bp-lg`+ — same layout with extra top padding, no new columns.

UX-DR29: Touch targets ≥ `{spacing.touch-target}` and input font ≥16px throughout; on-screen-keyboard handling uses `100dvh` (not `100vh`) so the composer stays visible above the keyboard, the latest message scrolls into view on composer focus, header and session strip stay sticky.

**State coverage**

UX-DR30: Implement every state in the EXPERIENCE.md State Patterns table across the three surfaces — Interview (fresh start, uploading, unreadable Resume / wrong file type, empty Question Bank notice, waiting/streaming with `aria-busy`, stopped/failed/hung reply with Retry and Provider-failure copy, session ending), Report (generating, generation failure with Retry, ended-early note, loaded with summary/strengths/weak-spots/review/Start-new), and Question Bank (loading/load failure, empty list, import running, import result including zero-found, add invalid/failed, delete failed, long list with count in the heading, no search/paging in v1).

### FR Coverage Map

FR-1: Epic 1 - Import Questions from a file
FR-2: Epic 1 - Add a single Question
FR-3: Epic 1 - View and delete Questions
FR-4: Epic 2 - Upload a Resume
FR-5: Epic 2 - Ask a mix of Resume and Question Bank questions
FR-6: Epic 2 - Follow up on answers
FR-7: Epic 2 - End the session
FR-8: Epic 2 - Produce a Coaching Report
FR-9: Epic 3 - Choose Providers by configuration
FR-10: Epic 3 - Rebuild embeddings on change

NFR-16GB: Cross-cutting, primarily validated in Epic 2 (prompt budgets, default local models)
NFR-secrets: Epic 3 (Provider configuration/secrets), foundational wiring from Epic 1
NFR-resume-deleted: Epic 2 (session lifecycle)
NFR-streaming: Epic 2 (chat turn SSE streaming)

## Epic List

### Epic 1: Question Bank Management
Simon can import Questions in bulk from a Word, PDF or plain text file, add a single Question by hand, and view and delete Questions in the Question Bank, which persists across sessions. As the first vertical slice, this epic also bootstraps the solution: the Vite React-TS frontend scaffold with routing, the three-project .NET backend (`Core`/`Infrastructure`/`Api`), SQLite persistence via EF Core migrations, the default Ollama embedding adapter behind `IEmbeddingGenerator`, the DESIGN.md token system, and the persistent Header/nav shell.
**FRs covered:** FR-1, FR-2, FR-3

### Epic 2: Resume-Based Mock Interview with Coaching
An Interviewee uploads a Resume, takes a chat interview that mixes Resume-based questions with Question Bank questions (or runs Resume-only if the bank is empty), gets a follow-up on a thin answer, ends the session (on schedule or early), and receives a Coaching Report naming strengths and weak spots against Reference Answers or a rubric. This delivers UJ-1 end to end on the default local Provider.
**FRs covered:** FR-4, FR-5, FR-6, FR-7, FR-8

### Epic 3: Swappable AI Providers & Re-embedding
Simon can switch the chat Provider and/or the embedding Provider (Ollama to OpenAI/Azure OpenAI) purely by configuration, with no code changes, and re-embed the whole Question Bank after an embedding Provider change so retrieval keeps working.
**FRs covered:** FR-9, FR-10

## Cross-cutting acceptance for UI stories

Every story that adds or changes UI must also meet: WCAG 2.2 AA contrast in both themes (UX-DR24); full keyboard operability and a visible 2px focus ring, Esc closes dialogs (UX-DR25); `prefers-reduced-motion` respected and usable at 200% zoom without horizontal scroll (UX-DR27); the three-breakpoint responsive behavior with touch targets and inputs at the minimum sizes, `100dvh` for the chat layout (UX-DR28, UX-DR29); microcopy taken verbatim from EXPERIENCE.md (UX-DR23). Copy for the error slugs `session-busy`, `invalid-session-state`, `duplicate-question`, `invalid-model-output` and Resume `file-too-large` is added to EXPERIENCE.md by the first story that can raise each one.

## Epic 1: Question Bank Management

Simon can import Questions in bulk from a Word, PDF or plain text file, add a single Question by hand, and view and delete Questions in the Question Bank, which persists across sessions. As the first vertical slice, this epic also bootstraps the solution: the Vite React-TS frontend scaffold with routing, the three-project .NET backend (`Core`/`Infrastructure`/`Api`), SQLite persistence via EF Core migrations, the default Ollama embedding adapter behind `IEmbeddingGenerator`, the DESIGN.md token system, and the persistent Header/nav shell.

### Story 1.1: Add a Question (and stand up the solution)

As Simon (builder),
I want to enter a single Question with an optional Reference Answer and have it saved,
So that I have a working, end-to-end app and my first Question in the bank.

**Acceptance Criteria:**

**Given** a fresh clone of the repo
**When** the developer runs the frontend (Vite) and backend (Kestrel) dev servers
**Then** the SPA loads, the persistent Header/nav shows the product name (from `bp-md` up), the Interview and Question Bank nav links, and a Theme toggle, `/` redirects to `/interview`, and Vite proxies `/api` to Kestrel

**Given** the backend starts
**When** EF Core migrations run at startup
**Then** the SQLite file at `data/interview-prep.db` (git-ignored) is created/migrated with the `question` table (`id`, `text`, `normalized_text` unique, `reference_answer`, `embedding`, `embedding_profile`, `created_at`)

**Given** the composition root
**When** the app starts with default configuration
**Then** `Providers:Embedding:Type` resolves to `ollama` (model `nomic-embed-text`) behind `IEmbeddingGenerator`, and Core has no reference to Infrastructure/Api or any NuGet package beyond `Microsoft.Extensions.AI`

**Given** the Question Bank page
**When** Simon enters Question text (and optionally a Reference Answer) in the Add a Question form and submits
**Then** the text is normalized (trim, collapse whitespace, case-fold) and embedded before any database write, then inserted via `POST /api/question-bank/questions`, returning `201` with the created Question
**And** a "Question added." toast (`role="status"`, auto-dismisses after 5s) confirms success

**Given** Simon submits with empty Question text
**When** he clicks Add
**Then** the app shows "Enter a Question first." without calling the API

**Given** the add request fails (Provider unreachable, or the normalized text already exists)
**When** the error returns
**Then** an inline error appears near the form (Provider-unavailable copy, or `409 duplicate-question` handling) with Retry where it applies, and the entered text is kept

**Given** the API is running
**When** a non-GET request arrives
**Then** Kestrel binds to loopback only, `AllowedHosts` is `localhost`, and a request without an `Origin` in `Security:AllowedOrigins` gets `403 invalid-request` (AD-17)
**And** request logging never records bodies, and no endpoint returns configuration

**Given** the frontend shell
**When** it renders
**Then** the DESIGN.md color, typography, spacing and radius tokens (UX-DR1 to UX-DR4) exist as CSS custom properties, light/dark and System/Light/Dark theme switching works (UX-DR8), and the shared Button, Form field, Toast/inline error and Empty state components (UX-DR9, 10, 12, 13) are available for later stories

**Given** any story that adds UI
**When** it is accepted
**Then** it meets the cross-cutting UX criteria in "Cross-cutting acceptance for UI stories" below

### Story 1.2: View and Delete Questions

As Simon (builder),
I want to see every Question in the bank and delete ones I no longer want,
So that I can keep the bank accurate and confirm my imports/adds landed.

**Acceptance Criteria:**

**Given** Questions exist in the bank
**When** Simon opens `/question-bank`
**Then** "Loading Questions..." appears, then the full list renders via `GET /api/question-bank`, each row showing Question text clamped to 2 lines, with a count in the heading (e.g., "87 Questions")

**Given** a Question has a Reference Answer
**When** Simon views its row
**Then** a "Reference Answer" disclosure is present and expands in place; rows without one show no disclosure

**Given** the bank is empty
**When** Simon opens `/question-bank`
**Then** the empty state reads "Your Question Bank is empty. Import a file or add a Question above." with no illustration

**Given** the list fails to load
**When** the `GET` request errors
**Then** an inline error appears with Retry

**Given** a Question row
**When** Simon clicks Delete
**Then** a Confirm dialog (`role="alertdialog"`) opens naming that Question, initial focus on Cancel, focus trapped, Esc cancels

**Given** the Confirm dialog is open
**When** Simon confirms
**Then** `DELETE /api/question-bank/questions/{id}` is called; on success (204, idempotent) the row is removed, a "Question deleted." toast appears, and focus returns to the next row (or the heading if it was the last)
**And** if the delete request fails, the dialog stays open with an inline error and Retry

**Given** the Question Bank
**When** any write (add, delete, import) commits
**Then** `IQuestionBank` atomically replaces its immutable `QuestionIndex` snapshot and exposes `QuestionBankState` (`Empty` / `Ready` / `NotIndexed`) per AD-8

**Given** rows exist whose `embedding_profile` differs from the configured profile (or is null)
**When** the API starts
**Then** `IEmbeddingReconciler` (single owner, one run at a time, AD-9) re-embeds them and replaces the snapshot after the database commit; this reconciler is built here so Epic 2 can rely on it

**Given** a Question has been deleted
**When** the in-memory `QuestionIndex` snapshot is next replaced
**Then** that Question is excluded from retrieval and is never asked in a later Interview Session

### Story 1.3: Import Questions from a File

As Simon (builder),
I want to bulk-import Questions from a Word, PDF or plain text file,
So that I can grow the bank quickly from my existing question sets.

**Acceptance Criteria:**

**Given** the Question Bank page
**When** Simon opens the Import from file zone
**Then** he can choose a Word, PDF or plain text file by click, keyboard ("Choose file" button) or drag-and-drop, with the hint "Each Question starts with Q1., Q2., and so on. The answer is the text up to the next Question."; a wrong file type is rejected inline before upload

**Given** Simon selects a valid file
**When** it uploads
**Then** the zone shows the filename and "Reading file...", and a second upload is blocked until it completes

**Given** the committed sample bank (`NET_Azure_Interview_Question_Bank.docx`)
**When** it is imported
**Then** the app reports 87 Questions added, each with its Reference Answer, and text before Q1 (title, subtitle, "Useful Links") is ignored — pinned by an automated test per AD-13

**Given** a file with an unreadable part (e.g., a `Q<n>.` line with no text after it)
**When** import completes
**Then** the Import result panel lists it under "Could not read" with its line/Question number and reason, and nothing else is silently dropped

**Given** a file yields zero recognizable Questions
**When** import completes
**Then** the panel reads "No Questions found." plus the Q1. hint

**Given** some imported Questions already exist in the bank (by normalized text) or repeat within the file
**When** import completes
**Then** those rows are counted under `alreadyInBank` and not duplicated (`ON CONFLICT DO NOTHING` on `normalized_text`)
**And** if embedding fails during import, nothing from that import is saved and a retryable error is returned

**Given** an uploaded file exceeds `Uploads:MaxBytes` (default 10 MB)
**When** Simon uploads it
**Then** it is rejected before parsing with `file-too-large` and matching inline copy

**Given** a successful import
**When** the result panel is showing
**Then** it stays until Simon dismisses it or starts a new import, and the newly imported Questions are available to the next Interview Session

## Epic 2: Resume-Based Mock Interview with Coaching

An Interviewee uploads a Resume, takes a chat interview that mixes Resume-based questions with Question Bank questions (or runs Resume-only if the bank is empty), gets a follow-up on a thin answer, ends the session (on schedule or early), and receives a Coaching Report naming strengths and weak spots against Reference Answers or a rubric. This delivers UJ-1 end to end on the default local Provider.

### Story 2.1: Upload a Resume and Start the Interview

As an Interviewee,
I want to upload my Resume and have the bot start asking me questions about it,
So that I can begin a mock interview tailored to my background.

**Acceptance Criteria:**

**Given** `/interview` is freshly visited
**When** the page loads
**Then** the bot greeting, the Upload zone, and the privacy line show ("Upload your Resume (Word or PDF) to start an Interview Session." / "Your Resume is used for this Interview Session only and isn't kept.")

**Given** an Interviewee uploads a Word or PDF Resume
**When** it uploads
**Then** the zone shows the filename and "Reading...", a second upload is blocked, and `POST /api/interview/sessions` (multipart `resume`) returns `201 { sessionId, plannedCount, bankState }`

**Given** no text can be extracted from the Resume
**When** upload completes
**Then** the bot says "I couldn't find any text in that Resume. It may be a scanned PDF. Please choose a different file." and the Upload zone stays ready; no session is created

**Given** a Resume longer than `Interview:MaxResumeChars` (default 12000)
**When** uploaded
**Then** it is rejected with `file-too-large`

**Given** a session is created
**When** the first turn is requested (no `answer`, first turn of the session)
**Then** a Resume-slot Question streams via SSE, the Thinking indicator shows until the first token, and the session strip reads "Question 1 of about 10"

**Given** the Question Bank is `Empty`
**When** the session starts
**Then** the bot's first message says the interview runs on the Resume only

**Given** Resume text and the ResumeProfile
**When** the interview ends (closing turn, End, delete or expiry, whichever first)
**Then** both are purged from session state and are never written to SQLite, files or logs

**Given** the ResumeProfile structured call fails at session creation
**When** that happens
**Then** no session exists and an error is surfaced to the user

### Story 2.2: Continue the Interview — Mixed Questions and Follow-ups

As an Interviewee,
I want the bot to keep asking me questions (mixing my Resume with the Question Bank) and follow up when my answer is thin,
So that the interview feels like a real, adaptive conversation.

**Acceptance Criteria:**

**Given** a session with `Ready` bank state
**When** Core selects a bank slot
**Then** `IQuestionRetriever` scores the `QuestionIndex` by cosine similarity, top-k, excluding already-asked Questions, selecting lazily at generation time (a Question deleted mid-session is never asked)

**Given** the Show-question-type switch is on
**When** a bank Question streams
**Then** its label reads "Question Bank"; behavioral Resume-based Questions read "Resume"

**Given** a thin or interesting answer
**When** Core decides a follow-up
**Then** at most one follow-up per Question is asked, referring to something the user actually said, `questionNo` does not advance, and the session strip counter stays the same

**Given** the composer
**When** Simon types an answer
**Then** desktop Enter sends / Shift+Enter adds a line, touch uses a Send button; empty input cannot send; the composer disables while the bot replies and Send becomes Stop while streaming

**Given** an in-flight turn
**When** Simon presses Stop
**Then** the `CancellationToken` reaches the model call, the partial reply is marked "Stopped." with Retry available, and the committed answer is preserved

**Given** a `turnId` already in flight or just completed
**When** Retry re-sends it
**Then** a matching `PendingTurn` replays the stored decision and only regenerates wording; a `turnId` equal to the last completed turn re-streams the stored reply with no model call

**Given** a turn, report or end request arrives while one is already running
**When** it is received
**Then** it gets `409 session-busy`; a request in the wrong state gets `409 invalid-session-state`

**Given** the interview-turn prompt
**When** built
**Then** it includes the system prompt, Resume text/ResumeProfile, the current slot's Question text only (never its Reference Answer), and the last `Interview:HistoryTurns` (default 6) transcript entries, within `Providers:Chat:ContextTokens` (default 8192)

**Given** a Provider failure after the stream starts
**When** it happens
**Then** an `error` SSE event is emitted and the chat shows "I couldn't reach the Provider. Check that it is running, then try again."

### Story 2.3: End the Interview and Receive a Coaching Report

As an Interviewee,
I want the interview to end (on schedule or early) and give me a Coaching Report,
So that I know how I did and where to improve.

**Acceptance Criteria:**

**Given** the last planned Question has been answered
**When** the next turn is requested
**Then** it is a closing turn with fixed Core text (no model call), `sessionComplete` is set, and the app opens `/interview/report`

**Given** the session strip's "End interview" (enabled only once one answer has been sent)
**When** Simon clicks it and confirms ("End interview now? Your Coaching Report will cover the answers you've given so far.")
**Then** the session ends early

**Given** `/interview/report` loads
**When** the report is generating
**Then** h1 "Coaching Report" and "Preparing your Coaching Report..." show, and `POST /api/interview/sessions/{id}/report` is called

**Given** report generation runs
**When** it executes
**Then** one structured review call happens per answered Question (Reference Answer if one exists, else rubric — situation/action/result for behavioral), cached per Question, followed by one synthesis call

**Given** the assembled report
**When** validated
**Then** Core rejects it unless it has ≥1 strength, ≥1 weak spot, every `questionNo` names an answered Question, and every quote traces to that answer; on failure it retries once, then returns `invalid-model-output`

**Given** a successful report
**When** rendered
**Then** it shows Summary, Strengths and Weak spots (each with a quoted excerpt and a "See the answer" link scrolling to/focusing the matching review card), and per-Question review cards (Question, answer given, then Reference Answer or rubric feedback); no numeric score anywhere

**Given** the session ended early
**When** the report loads
**Then** a top note reads "You ended early. This report covers the N Questions you answered."

**Given** report generation fails
**When** the error returns
**Then** an inline error with Retry appears, answers stay in memory, and a repeated `POST` to the report endpoint returns the cached report once one succeeds

**Given** `/interview/report` is opened directly with no report in memory
**When** it loads
**Then** it redirects to `/interview`

**Given** "Start new Interview Session" is chosen
**When** clicked
**Then** `DELETE /api/interview/sessions/{id}` is called (idempotent, 204) and a fresh session begins; a page reload has the same effect since the client keeps only `sessionId` and rendered messages

## Epic 3: Swappable AI Providers & Re-embedding

Simon can switch the chat Provider and/or the embedding Provider (Ollama to OpenAI/Azure OpenAI) purely by configuration, with no code changes, and re-embed the whole Question Bank after an embedding Provider change so retrieval keeps working.

### Story 3.1: Configure a Paid Chat/Embedding Provider

As Simon (builder),
I want to configure a paid Provider (OpenAI or Azure OpenAI) for chat and/or embeddings,
So that I can run the same interview and Question Bank on a hosted model without code changes.

**Acceptance Criteria:**

**Given** `Providers:Chat:Type` is set to `openai` with `Model`/`Endpoint`/`ApiKey` configured
**When** the API starts
**Then** the composition root constructs an `IChatClient` via `Microsoft.Extensions.AI.OpenAI` in that one place, with no other code changes, and the same Interview Session (upload, ask, follow-up, end, report) works unchanged

**Given** `Providers:Embedding:Type` is set to `openai`
**When** configured
**Then** `IEmbeddingGenerator` is constructed via the OpenAI adapter the same way, and Question import/add continues to work

**Given** an Azure OpenAI resource
**When** `Endpoint` is set to the resource's `/openai/v1/` URL
**Then** the same OpenAI adapter connects to Azure OpenAI (config-only distinction, no separate adapter)

**Given** the default configuration (no `Providers` override)
**When** the app starts
**Then** it still uses `ollama` for both chat and embedding, needing no paid account or API key

**Given** API keys
**When** configured
**Then** they come from .NET user-secrets in development or environment variables elsewhere, bound to validated options (`ValidateOnStart`); no endpoint ever returns configuration, and keys never reach the browser

**Given** a model-specific quirk (e.g., `nomic-embed-text`'s `search_document:`/`search_query:` prefixes)
**When** embedding via Ollama
**Then** that handling lives in an Infrastructure decorator over `IEmbeddingGenerator`; Core only ever passes plain text plus an intent (`document`/`query`)

### Story 3.2: Re-embed the Question Bank After a Provider Change

As Simon (builder),
I want to re-embed all my Questions after switching the embedding Provider,
So that retrieval keeps working correctly with the new vectors.

**Acceptance Criteria:**

**Given** each Question row
**When** written
**Then** it records an `embedding_profile` string `{providerType}:{model}:{dimensions}:{promptScheme}`, produced only by Infrastructure

**Given** the configured embedding profile changes (e.g., `Providers:Embedding:Type` or `Model`)
**When** the API starts
**Then** the `IEmbeddingReconciler` built in Story 1.2 re-embeds every row whose profile differs from the configured one (verified here by a switch from the Ollama profile to an OpenAI profile using fake generators)

**Given** Simon wants to trigger this without restarting
**When** he calls `POST /api/admin/re-embed`
**Then** the same reconciler instance runs and the response reports `{ reembedded }`

**Given** rows exist but none match the current profile (`NotIndexed`)
**When** a session is created
**Then** session creation waits for one reconcile; if still `NotIndexed` afterward, it fails with `provider-unavailable`

**Given** a re-embed completes
**When** retrieval next runs
**Then** it returns Questions from the whole bank, including ones imported before the Provider change

**Given** two re-embed triggers occur close together (startup and an admin call, or two admin calls)
**When** both fire
**Then** `IEmbeddingReconciler` runs one at a time, so no two re-embed jobs race
