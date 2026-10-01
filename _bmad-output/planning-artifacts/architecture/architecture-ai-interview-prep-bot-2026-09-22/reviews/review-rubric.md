# Rubric review: ARCHITECTURE-SPINE.md (ai-interview-prep-bot)

Reviewed: 2026-09-22. Subject: `ARCHITECTURE-SPINE.md` (status draft, altitude initiative). Cross-checked against the PRD, PRD addendum, `EXPERIENCE.md` and the architecture `.memlog.md`. The spine was not modified.

## Verdict

**Conditional pass.** The provider, RAG, persistence and session seams are well chosen, and every FR-1 to FR-10 is bound. No finding blocks a build start. Two gaps (the interview-flow API contract and the import parser's question/answer boundary) are real divergence points that should be closed in the spine first. The operational envelope is mostly decided or deliberately deferred, but four dimensions have holes: data/schema lifecycle, run modes, security beyond secrets, and testing beyond backend fakes.

| Rubric item | Result |
| --- | --- |
| Fixes the real divergence points, misses none | Partial: F1, F2, F5, F8, F9, F12 |
| Every AD Rule enforceable and prevents its stated divergence | Partial: F3, F4, F5, F6, F7 |
| Nothing under Deferred lets two units diverge | Mostly pass: OpenAPI deferral compounds F1 |
| Named tech verified-current | Pass, with one compatibility caveat (F14) |
| Covers PRD capabilities | Pass (all FR-1..10 and the four NFRs bound); FR-8 and FR-1 testable consequences under-enforced (F6, F2) |
| Every owned dimension decided, deferred or open | Partial: see Dimension audit |
| Mermaid diagrams valid | Pass (all four parse); one semantic slip (F15) |
| Terse | Pass, minor repetition (F16) |

## Findings

Severity: **High** = two units will probably diverge or a stated requirement is unenforced; **Medium** = a rule has a real hole or a dimension is silent; **Low** = polish or cheap to fix.

### F1 (High) The interview-flow API contract is not fixed

Only three paths appear anywhere: `/api/question-bank/import`, `/api/interview/sessions`, `/api/admin/re-embed`. Missing, and all needed by both frontend and backend agents:

- Paths and verbs for submit answer, Retry, End interview, get Coaching Report, and Question Bank list/add/delete.
- The `done` event payload. The UX needs "Question N of about 10" (N counts Questions, not follow-ups), the per-Question source label (Resume or Question Bank; follow-ups carry none), an end-of-session signal that opens `/interview/report`, and the empty-Question-Bank notice. Nothing says how the server conveys these.
- Whether the session-start call streams the first Question or returns JSON.
- `types.ts` is "the single hand-written mirror", but of an undefined surface. Deferring OpenAPI generation makes this worse, because drift is discovered late.

Fix: add one AD (or a table under Consistency Conventions) listing every endpoint, verb, and response/SSE event shape at DTO-name level.

### F2 (High) The import parser leaves the Question/answer boundary and prefix handling open

AD-13 says a Question "starts at a line beginning `Q<number>.`" and the answer is "the text up to the next Question". It does not say:

- Whether `Q12.` is stripped from the stored `text`. This decides what the bot asks and, through `normalized_text`, whether re-import dedupe (AD-14) works when numbering changes.
- Where the question text ends and the answer begins: the first line only, or up to the first `?`. For a PDF where a long Question wraps, "first line" truncates it.
- PDF output as "one paragraph per line" is not something PdfPig gives natively (it yields words and lines). Hard-wrapped PDF lines make the boundary rule fail, and the rule as written cannot be met uniformly for PDF.
- Case-folding culture for `normalized_text` (invariant should be named).
- Whether "no answer" is null or empty (schema says nullable).

The FR-1 testable consequence (sample file yields 87 Questions with Reference Answers) is not attached to any test rule.

### F3 (Medium) AD-4 does not actually enforce "Resume deleted when the session ends"

- Idle expiry is named but has no config key or default (`Interview:SessionIdleMinutes` or similar). A browser reload orphans the server session with the Resume text in memory until that unspecified expiry.
- No explicit session-delete endpoint (for example a `DELETE` on the session, or a `sendBeacon` on unload).
- "Resume text ... purged when the interview ends" vs "the whole session is removed after the report": whether the ResumeProfile (derived personal content) is purged at interview end or kept until report delivery is ambiguous.
- No cap on concurrent sessions (single-user is assumed but never stated).

### F4 (Medium) No context budget for interview turns; AD-12 covers coaching only

NFR-16GB is bound to AD-12, but the interview-turn prompt (Resume text, ResumeProfile, growing transcript, top-k Questions) has no size rule and no transcript-truncation policy. The chat options are not in the Config section list (`Type`, `Model`, `Endpoint`, `ApiKey`), so nothing fixes `num_ctx`, temperature, `keep_alive` or timeouts. Ollama's default context window is much smaller than a model's advertised 128K, so a long session on `gemma3:4b` can silently truncate the Resume or early turns. Decide where the context window is configured and what is dropped first.

### F5 (Medium) AD-5 (turn commit protocol) has three holes

1. First-Question failure: after Resume upload the first Question has no committed answer, yet Retry is defined only as "regenerate the reply to the last committed answer". The UX requires Stop/Retry there too.
2. Retry semantics: "decide next step" happens at answer commit (sequence diagram). Whether Retry replays the already-decided step (same follow-up flag, same retrieved Question) or re-decides is unstated. Re-deciding can burn the one follow-up or pick a different Question, and "already-asked" exclusion depends on when a Question is marked asked (at plan time, at commit, or at stream completion).
3. Concurrency: nothing says one in-flight turn per session, or what a second submit or Retry during a stream returns. There is no slug for it in AD-15.

### F6 (Medium) AD-11 does not enforce FR-8's "each referring to one of the user's actual answers"

The record carries `questionNo` and a quote, but nothing requires Core to verify the quote occurs in that answer or that at least one strength and one weak spot exist (PRD FR-8, UX "at least one"). Small local models will fabricate quotes. Add a Core validation step (quote is a normalized substring of the answer; minimum one of each), using the existing one-retry path. Partial failure inside AD-12's N+1 calls (one per-Question review fails) is also undefined: retry all, or per Question.

### F7 (Medium) AD-15 vs AD-17 conflict on exception logging

AD-15: "Exception text is logged, never returned." AD-17: Resume text, answers and Reference Answers are never logged. Provider SDK and deserialization exceptions can echo request or response content. Also, Microsoft.Extensions.AI logging/OpenTelemetry middleware can log prompts. Add: log exception type and ids only (or scrub), and forbid content-logging middleware in the chat pipeline. The Core-side error taxonomy is also missing. Core cannot catch OllamaSharp or HTTP exceptions (AD-1), so a Core exception type (for example `ProviderUnavailableException`) and a single exception-to-ProblemDetails handler are needed; today the mapping is implied, not decided. HTTP status per slug (400/404/413/415/422/502) is not listed either.

### F8 (Medium) Frontend state ownership across routes is not decided

UX: leaving for Question Bank and returning resumes the Interview Session. AD-16 allows `useState`, `useReducer` and Context but does not say the interview state lives in a Context provider above the router outlet, nor what happens to an in-flight stream when the user navigates away (abort or continue). A build agent that keeps state in the route component breaks the UX contract. The single place that reads the SSE stream (`src/api`) is also implicit.

### F9 (Medium) Data lifecycle for the SQLite file is silent

- Schema creation and change: EF Core migrations vs `EnsureCreated` is not decided, and this matters for the BLOB and unique-index columns and for tests.
- `Storage:Path` default `data/interview-prep.db` is relative to what? The repo tree puts `data/` at the repo root while Kestrel runs from `backend/src/...`; a CWD-relative path creates the file in the wrong place.
- No backup or export for the Question Bank, the only durable, user-created data (87 imported Questions with long answers), and it is git-ignored.
- Deleting a Question mid-session: whether planned Questions in the session snapshot the text and Reference Answer or reference the row. FR-3 says a deleted Question is never asked later; a coaching call that needs a deleted row's Reference Answer would break.
- Related inconsistency: AD-4 says the session holds "planned Questions", but AD-8 builds retrieval queries from "the latest exchange", which implies per-turn selection. Pick one.

### F10 (Medium) Security: loopback-only is treated as sufficient, and it is not

- AD-17's "unauthenticated API not reachable from the network" is not enforced against `vite --host`, or an `ASPNETCORE_URLS` override; the Vite proxy would re-expose the API. Say where the bind is fixed and validate at startup.
- "CORS is never configured" blocks cross-origin reads but not side effects: any web page open in the user's browser can fire simple cross-site requests (multipart import, `POST /api/admin/re-embed`) at localhost, and DNS rebinding reaches the rest. Add an `AllowedHosts`/Origin check.
- Hosted Providers (`openai`, Azure) send Resume text and answers off the machine; the UX privacy line says the Resume "isn't kept". State the egress and that the UX copy or README must say so.
- The Resume and the user's answers are untrusted input to prompts (prompt injection into coaching output). AD-3 protects control flow, but no rule says to delimit untrusted text in prompts.
- Upload handling stops at the 10 MB compressed limit; OpenXml zip expansion and PDF parsing limits are unaddressed.

### F11 (Medium) Testing is decided for the backend only

AD-18 covers fake `IChatClient`/`IEmbeddingGenerator`. Silent: frontend tests (Vitest is in the Stack; the memlog's React Testing Library is not), whether repository tests use real SQLite (in-memory SQLite, not the EF InMemory provider, given BLOB and unique index), the FR-1 87-Question fixture (and whether the sample files, one of which is a real Resume, are committed), an architecture test for AD-1 layering, and what "live-model checks are opt-in" means mechanically (trait, environment flag).

### F12 (Low) Operations, environments and run modes

Hosting is properly deferred, but the spine is silent on how the SPA is served outside `vite dev`. AD-16 forces relative `/api` URLs, which work only with the dev proxy; the memlog's "ASP.NET serves wwwroot later" thought did not reach the spine. Also unspecified: the Kestrel dev port (the Vite proxy target must match it), `Development` vs `Production` config layering, startup behavior when Ollama is down or the model is not pulled (fail vs start degraded), and whether `POST /api/admin/re-embed` and the startup re-embed (AD-9) block startup, run in the background, report progress, or serialize against imports. A health/readiness endpoint is neither decided nor deferred. It would give the UX's "check that it is running" message something to key on.

### F13 (Low) Enforceability of the AD-1 and AD-2 Rules

AD-1's "endpoints only parse HTTP and call one use case" is a review convention with no check (an architecture test would make it enforceable). AD-2 says no wrapper interface duplicates `IChatClient`/`IEmbeddingGenerator`, and AD-10 introduces `IQuestionEmbedder` over `IEmbeddingGenerator`, so "all model calls go through" and "Core embeds through" read as contradictory; reword AD-2 as "Core's only path is the M.E.AI interfaces, plus `IQuestionEmbedder` for intent". AD-17 should also say `ApiKey` validation is conditional on `Type=openai` (the default needs no key).

### F14 (Low) Stack verification

Verified against the live registries on 2026-09-22: typescript 7.0.2, vite 8.3.0, react 19.3.0, react-router 8.4.0, vitest 5.0.1, Microsoft.Extensions.AI 10.10.0, OllamaSharp 5.4.30, xunit.v3 4.0.1 are all current stable. The memlog also records checks for the remaining NuGet packages, .NET SDK, Node/npm and Ollama 0.34.2 (not re-verified here). Caveats: (a) TypeScript 7.0.2 with the `react-ts` Vite template, Vitest 5 and any lint tooling is a compatibility risk (the template pins its own TypeScript); confirm on first scaffold. (b) `gemma3:4b` fitting a 4 GB VRAM GPU is a memlog assumption but appears in the Stack as a plain default; tag it `[ASSUMPTION]` or open question. (c) Ollama is not yet installed; this is flagged already. (d) No central package version file is named (`Directory.Packages.props` or equivalent), so the table's versions have no enforcement point.

### F15 (Low) Mermaid

All four blocks parse with mermaid 11 (`flowchart`, `sequenceDiagram`, `flowchart`, `erDiagram`; parse-checked with `mermaid.parse`). Semantic slip: in the sequence diagram `M-->>B: token events via SSE` shows the model streaming to the browser. Tokens flow M to C to A to B (SSE is produced by the Api endpoint). The topology diagram also omits the embedding Provider's own hosted path (only chat implied). The ER diagram shows `long` while the AD says integer; cosmetic.

### F16 (Low) Terseness

The spine is mostly decisions, and "Prevents" lines are the template. Small drift: rationale in parentheses (AD-6 "EventSource is GET-only", AD-16 "so CORS is never configured"), and rules restated three or four times: the dependency direction (Design Paradigm, AD-1, diagram), and cancellation, errors, state and logging (Consistency Conventions repeat AD-4/5/7/15/17). Trim the repeats to a pointer.

## Dimension audit

| Dimension | Status | Note |
| --- | --- | --- |
| Architecture paradigm / layering | Decided | AD-1 |
| Provider / infra strategy | Decided (AD-2, AD-9, AD-10); model runtime options missing | F4 |
| API shape and errors | Decided partially | F1, F7 |
| Frontend structure and state | Decided partially | F8 |
| Streaming | Decided | AD-6; payload gap in F1 |
| Persistence | Decided; lifecycle silent | F9 |
| Data lifecycle (sessions, Resume) | Decided; expiry and delete gaps | F3 |
| Data lifecycle (bank: schema, backup, delete-during-session) | Silent | F9 |
| Error handling | Decided partially | F5, F6, F7 |
| Security | Decided for secrets and exposure; broader threats silent | F10 |
| Deployment and environments | Local topology decided, hosting deferred; run modes and prod SPA serving silent | F12 |
| Operations (logging, health, startup, re-embed run) | Logging decided; rest silent | F7, F12 |
| Testing | Backend seam only | F11 |
| Performance / capacity (NFR-16GB) | Partially (coaching only) | F4 |
| Concurrency / multi-user assumption | Silent (single user implied) | F3, F5 |
| Accessibility / UX | Deferred to EXPERIENCE.md and DESIGN.md | Acceptable |
| Build tooling (formatting, analyzers, central packages) | Silent | Low; add `.editorconfig`, warnings-as-errors, `Directory.Packages.props` |

## Deferred section check

- Follow-up mechanism, slot ratio, hang threshold: safe. The first two are Core-internal, and AD-3 bounds the outcome. The hang threshold is client-only.
- Anthropic adapter, hosting/auth, paging, saveable reports, editing, tagging: safe, and each has a revisit trigger or a PRD/UX basis.
- OpenAPI-generated types: deferral is acceptable only if F1 is closed first.
- CI, packaging, Docker, telemetry: acceptable for a local build, but "how the SPA is served without Vite" is not covered by any of them (F12).
- Ollama pull and warm-up automation: fine, but cold-start latency and a first-call timeout policy belong in F4 and F12.

## PRD coverage

FR-1 to FR-10 and the four NFRs (16 GB, secrets, Resume deleted, streaming) are all bound and mapped. Weak links: FR-1 (87-Question sample and Reference Answers stored with the Question: F2, F11), FR-3 (deleted Question never asked: F9), FR-4 "Resume deleted when the session ends" (F3), FR-7 end-early requirement (server-side minimum-one-answer rule is not stated; UX disables the button, the server should reject with `invalid-request`), FR-8 (F6). PRD open questions 1 and 2 are correctly carried into Deferred. The UX open questions (file size, re-import dedupe) are resolved in AD-14.

## Summary of recommended additions (smallest set)

1. Endpoint and event catalogue (F1).
2. Import parsing details: strip prefix, boundary, PDF line handling, invariant fold (F2).
3. Session expiry default plus explicit delete, ResumeProfile purge (F3).
4. Interview-turn context budget and chat options in config (F4).
5. Retry/first-turn/concurrency rules in AD-5 (F5).
6. Quote and minimum-count validation, partial-failure rule (F6).
7. Logging scrub and Core error mapping (F7).
8. Interview state in an app-level Context (F8).
9. Migrations decision, DB path resolution, backup note (F9).
10. AllowedHosts/Origin check, Provider egress note, prompt-injection delimiting (F10).
11. Frontend and integration testing rule (F11).
