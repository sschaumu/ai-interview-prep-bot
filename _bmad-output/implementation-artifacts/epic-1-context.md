# Epic 1 Context: Question Bank Management

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Let the builder (Simon) import Questions in bulk from Word/PDF/text, add one by hand, and view and delete Questions in a persistent Question Bank. As the first vertical slice it also stands up the whole solution: Vite React-TS frontend with routing, a three-project .NET 10 backend, SQLite persistence via EF Core migrations, the default Ollama embedding adapter, the design token system and the persistent Header/nav shell.

## Stories

- Story 1.1: Add a Question (and stand up the solution)
- Story 1.2: View and Delete Questions
- Story 1.3: Import Questions from a File

## Requirements & Constraints

- A Question has text and an optional Reference Answer; new/imported Questions are available to the next Interview Session; deleted ones are never asked again.
- Import: files laid out "Q1.", "Q2.", ...; the answer is the text up to the next Question; text before Q1 is ignored; the committed sample bank must yield 87 Questions each with a Reference Answer.
- Secrets never reach the browser; no endpoint returns configuration; request logging never records bodies; Question/answer/imported text is never logged.
- Default config uses local models only, no paid account or API key.
- Every UI story must meet WCAG 2.2 AA in both themes, full keyboard operability with a 2px focus ring, reduced-motion and 200% zoom support, three-breakpoint responsive layout with 44px-class touch targets and 16px inputs, and microcopy taken verbatim from EXPERIENCE.md.

## Technical Decisions

- Layout: `backend/InterviewPrepBot.slnx`, `backend/src/InterviewPrepBot.{Core,Infrastructure,Api}`, `backend/tests/InterviewPrepBot.*.Tests` (fixtures hold the sample bank copy), `frontend/`, `data/` (git-ignored SQLite), `docs/`.
- Layering: Api → Infrastructure and Core; Infrastructure → Core. Core has no NuGet dependency beyond `Microsoft.Extensions.AI`. Endpoints only parse HTTP, call one Core use case, map to a DTO. Ports are Core interfaces implemented in Infrastructure.
- Models go through `IChatClient` / `IEmbeddingGenerator<string, Embedding<float>>`, chosen by `Providers:*:Type` config in the Api composition root. Default `ollama` (embedding `nomic-embed-text`, 768 dims). Model-specific quirks (nomic prefixes) live in an Infrastructure decorator; Core passes plain text plus intent (`document`/`query`) via `IQuestionEmbedder`.
- Persistence: single SQLite file via EF Core, `Storage:Path` default `data/interview-prep.db`, migrations applied at startup. Table `question`: `id`, `text`, `normalized_text` (unique), `reference_answer`, `embedding` BLOB, `embedding_profile`, `created_at` UTC. Only `IQuestionBank` writes Questions.
- `IQuestionBank` owns an immutable `QuestionIndex` snapshot replaced atomically after each write; `QuestionBankState` is `Empty`/`Ready`/`NotIndexed`. `IEmbeddingReconciler` is the single owner of re-embedding (startup and admin endpoint). Profile string `{providerType}:{model}:{dimensions}:{promptScheme}`. Only Question text is embedded.
- `QuestionText.Normalize` (trim, collapse whitespace, case-fold) is shared by parser, single add and the unique index. Add/import: embed first (outside a transaction), then insert in one transaction with `ON CONFLICT DO NOTHING`; conflicts count as `alreadyInBank`; embedding failure saves nothing. Single add of an existing Question returns `409 duplicate-question`.
- API: REST JSON under `/api`, camelCase, ISO-8601 UTC; errors are RFC 9457 ProblemDetails with stable `type` slugs (`provider-unavailable`, `unreadable-document`, `unsupported-file-type`, `file-too-large`, `duplicate-question`, `invalid-request`, ...). Question-bank endpoints: `GET /api/question-bank`, `POST /api/question-bank/questions`, `DELETE /api/question-bank/questions/{id}` (idempotent 204), `POST /api/question-bank/import`.
- Document extraction: `IDocumentTextExtractor` Core port; adapters for Word (DocumentFormat.OpenXml), PDF (PdfPig), text (UTF-8); type detected from content. Import parsing is a pure Core function. Uploads over `Uploads:MaxBytes` (10 MB) rejected before parsing.
- Security: Kestrel loopback only, `AllowedHosts` = `localhost`, non-GET requests need an `Origin` in `Security:AllowedOrigins` else `403 invalid-request`.
- Frontend: Vite react-ts, react-router, Vitest + React Testing Library; no UI kit, no server-state library. `src/api/` has one function per endpoint with relative `/api/...` URLs; `src/api/types.ts` hand-mirrors DTOs; Vite proxies `/api` to Kestrel. Tests use fake `IChatClient`/`IEmbeddingGenerator` on the backend and a mocked `src/api` on the frontend.
- Stack pins: .NET 10.0.401/net10.0, Microsoft.Extensions.AI 10.10.0, OllamaSharp 5.4.30, EF Core Sqlite 10.0.12, xunit.v3 4.0.1, React 19.3.0, Vite 8.3.0, react-router 8.4.0, Vitest 5.0.1, TypeScript ~6.0.2.

## UX & Interaction Patterns

- Single centered column on every view, mobile-first, breakpoints at 40rem and 64rem; never a sidebar.
- Routes: `/` redirects to `/interview`, `/interview`, `/interview/report`, `/question-bank`. Header is sticky with nav links (`aria-current="page"`), product name from 40rem up, and a Theme toggle cycling System → Light → Dark (persisted in `localStorage` in try/catch). Route change moves focus to the new `h1`.
- Design tokens are CSS custom properties on `:root`, light and dark sets with identical names, dark under `prefers-color-scheme` and `[data-theme="dark"]`; components use only `var(--color-...)`, system fonts, sentence case.
- Shared components: Button (primary/secondary/destructive), Form field (label above, error via `aria-describedby`, focus first invalid), Confirm dialog (`alertdialog`, focus on Cancel, trapped, Esc cancels), inline errors (`role="alert"`, with Retry) and toasts (`role="status"`, 5 s), Empty state.
- Question Bank rows: text clamped to 2 lines, "Reference Answer" disclosure, secondary Delete opening a Confirm dialog. Upload zone supports click, keyboard and drag-and-drop with the Q1. hint. Import result panel shows the added count, a "Could not read" list, and dismiss.

## Cross-Story Dependencies

- Story 1.1 creates the solution scaffold, shell, tokens and shared components that 1.2 and 1.3 reuse.
- Story 1.2 builds `IQuestionBank`, the `QuestionIndex` snapshot and `IEmbeddingReconciler`, which Epic 2 (retrieval) and Epic 3 (re-embed) rely on.
- Story 1.3 adds the document extractors and import parser that Epic 2 reuses for Resume upload.
- Epic 3 adds the OpenAI adapter and admin re-embed endpoint on top of this epic's ports.
