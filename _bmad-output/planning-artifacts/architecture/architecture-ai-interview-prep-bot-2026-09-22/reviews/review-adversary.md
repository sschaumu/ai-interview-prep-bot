---
name: review-adversary
target: ../ARCHITECTURE-SPINE.md
reviewer: adversarial (agent-parallel build lens)
date: 2026-09-22
---

# Adversarial review: Architecture Spine, ai-interview-prep-bot

## Verdict

**Not ready to fan out to parallel build agents as written.** The layering ADs (AD-1, AD-2, AD-7, AD-10, AD-13) are sound and hard to violate. The weakness is everything that sits *between* two units: the spine names three endpoints and three SSE event names, and then leaves the whole API contract, the session/turn lifecycle and the cache-invalidation seam to be guessed independently by each agent. Most of the pairs below are not exotic races; they are two agents reading the same sentence and making different, equally legal choices.

Ranking is by likelihood of real rework (H = will almost certainly collide, M = likely, L = possible). Each finding constructs Unit A and Unit B, both obeying every AD and convention, shows the collision, and proposes the AD that closes it. Proposed ADs use the next free numbers (AD-19 onward) and tighten existing ones where noted.

| # | Hole | Likelihood | Closes with |
| - | ---- | ---------- | ----------- |
| 1 | No API contract: endpoints, verbs, DTOs, SSE payloads, first-question stream | H | AD-19 (contract table) + AD-6 tightened |
| 2 | Turn and session state: no server-side turn states, Retry is not idempotent, decide-vs-commit unspecified | H | AD-20 |
| 3 | Session lifecycle: who purges, who removes, expiry semantics, orphaned sessions, "delivered" cannot be known | H | AD-21 (amends AD-4) |
| 4 | Retriever cache and bank writes are two adapters with no invalidation seam; "empty bank" has two definitions | H | AD-22 (amends AD-8) |
| 5 | Import atomicity vs unique constraint vs concurrent imports; import report shape; parser boundaries | M-H | AD-23 (amends AD-13, AD-14) |
| 6 | Frontend: who owns session state and the in-flight fetch across routes; stale-tab delete | M-H | AD-24 (amends AD-16) |
| 7 | Session snapshot, `questionNo` and follow-ups: report and transcript disagree on what a "Question" is | M | AD-25 |
| 8 | Re-embed: startup and endpoint are two owners; concurrent with import, delete and live sessions; profile string derivation | M | AD-26 (amends AD-9, AD-10) |
| 9 | Interview-turn prompt assembly and Ollama context window are unowned; silent truncation | M | AD-27 |
| 10 | Loopback binding does not stop browser-originated requests; logging middleware leaks content | L-M | AD-28 (amends AD-17) |
| 11 | Small items (ids reuse, extractor allowed-types, follow-up on last Question, quote validation, SSE framing) | L-M | folded into the ADs above and the list at the end |

---

## 1. No API contract; the SPA and the API cannot be built independently (H)

The spine defines `/api/question-bank/import`, `/api/interview/sessions`, `/api/admin/re-embed`, the SSE event *names* `token` / `done` / `error`, and the error slug list. That is the entire contract. `src/api/types.ts` is declared "the single hand-written mirror of backend DTOs" but nothing says which side is written first or what the DTOs are.

**Pair 1a (backend story "answer turn" + frontend story "chat view").**
- Backend agent: `POST /api/interview/sessions/{id}/answers`, body `{ "text": "..." }`, SSE `token` events with `data:` being the raw text delta, `done` with an empty `data:`. Obeys AD-6, AD-15, camelCase, ISO dates.
- Frontend agent: needs, per bot message, `questionNo`, `source` (Resume or Question Bank, for the Show question type label), `isFollowUp` (strip must not advance, follow-ups carry no label), and whether the session is now complete (to route to `/interview/report`). AD-6 gives no channel for any of that except `done`, whose payload is undefined. The frontend agent invents `done` = `{ questionNo, source, isFollowUp, sessionComplete }` in `types.ts`. The backend agent never reads `types.ts`.
- Result: strip, labels and end-of-interview navigation all broken; discovered only at integration.

**Pair 1b (SSE framing).** Backend sends raw token text in `data:`. A token containing `\n` is split across multiple `data:` lines, and the SSE spec strips exactly one leading space after `data:`. A hand-rolled `fetch` + `ReadableStream` parser (AD-6 forbids `EventSource`) that trims, or that only reads one `data:` line, silently corrupts whitespace and newlines in model output.

**Pair 1c (first Question streams, but session creation is "plain JSON").** AD-6: "Every other endpoint returns plain JSON." UX step 3: after Resume upload the first Question streams in behind a Thinking indicator. `POST /api/interview/sessions` is a multipart upload. Backend agent A returns `{ sessionId }` as JSON; frontend agent B expects the first Question to stream from that same call. Or A adds a second endpoint (`/start`) that nobody else knows about. Also **Retry has no target**: AD-5 says Retry "regenerates the reply to the last committed answer", and before the first answer there is none, so a failed first-Question stream has no legal recovery path.

**Pair 1d (fixed copy versus model text).** UX gives exact copy for the empty-bank notice, the "interview is over" line, "Stopped." and greeting. AD-6 makes every bot message a streamed model reply. Backend agent has the model write the notice; frontend agent hard-codes the copy and needs a `bankEmpty` flag that does not exist.

**Pair 1e (config the UI needs).** Strip reads "Question N of about 10". AD-17 says "No endpoint returns configuration". The frontend hard-codes 10; `Interview:QuestionCount` is changed and the two disagree.

**Pair 1f (error timing).** Errors before the first byte (session-not-found, invalid-request) can be HTTP 4xx ProblemDetails; errors after headers are sent can only be `error` events. AD-6 and AD-15 do not say which is which, or what the client does when the stream ends with neither `done` nor `error` (server restart, proxy drop). Backend performs the session lookup lazily inside the stream and emits everything as 200 + `error` event; frontend handles only HTTP status for the pre-stream case.

**Proposed AD-19 — API contract table is part of the spine.** The spine (or a sibling `api-contract.md` that the spine binds) lists every endpoint with verb, path, request body, success body, and status per error slug. Minimum content:
- Endpoints: `GET /api/question-bank/questions`, `POST` (add), `DELETE /{id}` (idempotent, 204 even if already gone), `POST /api/question-bank/import`, `POST /api/interview/sessions` (multipart) returning `{ sessionId, plannedQuestionCount, questionBankEmpty }`, `POST .../{id}/turns` (answer, SSE), `POST .../{id}/turns/retry` (SSE), `POST .../{id}/end`, `POST .../{id}/report`, `DELETE .../{id}`, `POST /api/admin/re-embed`.
- The first Question is produced by `POST .../{id}/turns/next` (SSE) or by retry with no committed answer; pick one and say so.
- SSE `data:` is always a JSON object. `token` = `{ "text": "..." }`. `done` = `{ "turn": { "questionNo", "source": "resume"|"bank"|null, "kind": "question"|"followUp"|"closing" }, "sessionComplete": bool }`. `error` = `{ "type": "<slug>" }`. A stream that ends without `done` or `error` is treated as `provider-unavailable` by the client.
- Errors that can be detected before streaming are HTTP ProblemDetails; only errors after the first byte are `error` events.
- Fixed copy (empty-bank notice, interview-over line, greeting, Stopped.) is client-owned from flags in the DTOs; model text is only Question wording, follow-ups and closing content.
- Backend DTO records and `types.ts` are produced in the same story that adds the endpoint, and a contract test asserts JSON shape against a fixture the SPA tests also read.

---

## 2. Turn and session state: no server-side turn states; Retry is not idempotent (H)

AD-3 makes the state machine "code-owned"; AD-5 defines commit points for the *transcript* but not for the *state machine's own state* (asked-set, question count, follow-up-used flag, chosen next Question). The sequence diagram does "commit answer, decide next step (state machine)" before streaming and "commit bot reply when stream completes" after.

**Pair 2a (decide is not idempotent; Retry double-advances).**
- Unit A (turn use case): mutates `askedIds`, `questionNo` and `followUpUsed` at decide time, before streaming, because the retriever needs the asked-set immediately (AD-8 "excluding already-asked").
- Unit B (Retry use case): "regenerate the reply to the last committed answer" = run the same `SubmitAnswer` decision again.
- Result: a failed stream then Retry chooses a *different* next Question (retrieval is fresh; the previously chosen one is already in the asked-set), and the count advances twice, so "Question 4 of about 10" becomes 5 and one bank Question is skipped forever. If the follow-up decision is a model call (deferred mechanism), Retry can flip follow-up to next-Question, or vice versa.
- Opposite legal choice (A mutates only at stream completion): two overlapping requests both decide from the same state and pick the same Question.

**Pair 2b (Stop at the last token).** Client Stop is only an abort. Server has already committed the bot reply (commit-on-completion) and is about to send `done`. Client sees an abort, shows "Stopped.", and Retry regenerates a second bot reply to the same answer. Transcript now has two consecutive bot messages; report and count are off.

**Pair 2c (double-submit and two-tab).** Client "Composer disabled while the bot replies" is a UI rule. A double Enter before React re-renders, or an aborted-then-resubmitted request whose first handler has not yet observed cancellation (Ollama can take seconds), reaches the server twice. Nothing says a session accepts an answer only in a specific state. Transcript gets two consecutive answers, or two concurrent streams write into one session. Same for `End` arriving while a reply is streaming: report generation snapshots the transcript while the reply commit lands afterwards.

**Pair 2d (slug list is closed and too small).** AD-15's six slugs have no code for "session busy", "wrong state" (answer while a reply is pending; retry with nothing to retry; answer after end), or "Question not found" on delete. Each agent picks an existing slug (`invalid-request` for all, which the frontend cannot distinguish) or invents one; the frontend maps only the six.

**Proposed AD-20 — Per-session turn state machine, single-flight, idempotent turns (amends AD-3, AD-5, AD-15).**
- A session has an explicit `TurnState`: `AwaitingAnswer`, `ReplyPending(turnId)`, `ReplyFailed(turnId)`, `Ended`. Only Core mutates it, under a per-session lock (one in-flight operation per session; a second gets HTTP 409).
- At decide time Core stores a `PendingTurn` (chosen Question or follow-up, wording intent, `turnId`). Nothing counted (asked-set, `questionNo`, follow-up-used) is finalised until the reply commits. Retry replays the stored `PendingTurn`; it never re-decides or re-retrieves.
- Every turn carries a client-generated `turnId` (or the server returns it in the `done`/`error`-adjacent event). A request whose `turnId` is already committed returns the committed reply (as a non-streamed `done` with the text) instead of creating a second one. Retry of a turn whose reply is already committed is a no-op returning that reply.
- Valid transitions: answer only from `AwaitingAnswer`; retry only from `ReplyFailed`/`ReplyPending`; end from any non-`Ended` state (cancels an in-flight stream first); report only from `Ended`.
- Add slugs `session-busy` (409) and `invalid-session-state` (409), and `question-not-found` if delete is not idempotent. AD-15 list is amended. Frontend maps them to UX copy (UX needs a line for each; see item 11).

---

## 3. Session lifecycle: who purges, who removes, and what "delivered" means (H)

AD-4: Resume text is "purged when the interview ends or the session expires". The transcript "stays until the Coaching Report is delivered successfully, then the whole session is removed." AD-1: endpoints only call one Core use case.

**Pair 3a (ResumeProfile: is it "Resume text"?).**
- Interview story: on End, purges Resume text and ResumeProfile (a profile is derived from the Resume, and NFR "Resume deleted" is a headline requirement; a test asserts nothing of it remains).
- Coaching story: builds the per-Question rubric verdicts and the synthesis call (AD-12) using ResumeProfile, and "Retry" after failure needs it again.
- Result: Coaching Retry fails after End with a null profile, or the profile survives and the NFR test fails. AD-4 lists ResumeProfile as held in the store but purges only "Resume text".

**Pair 3b (who removes the session).** "Delivered successfully then removed" cannot be in the Coaching use case (it does not know the response reached the client) nor in the endpoint (AD-1: endpoints call one use case). Coaching agent removes at return-from-use-case; endpoint agent adds a second call to `ISessionStore.Remove` after writing the response. If the response is lost (network drop, tab navigated away, SPA fetch aborted by unmount) the session is already gone, so Retry returns `session-not-found` and the user's answers are lost, the exact opposite of the UX copy "Your answers are still here."

**Pair 3c (expiry semantics unspecified).** "Idle expiry" has no value in the Config section (no `Interview:SessionIdleMinutes`), no definition of what refreshes it, and no rule for streaming or report generation. Interview agent touches on request start; Coaching agent does not touch at all. A 4b model on 16 GB can take minutes for N+1 report calls or a long stream; the timer fires mid-stream, and the reply commit dereferences a removed session. A user staring at the report error screen for longer than the window gets `session-not-found` on Retry. UX has no copy for `session-not-found`.

**Pair 3d (orphaned sessions defeat the NFR).** "The client keeps only `sessionId`... a reload ends the session." Reload only ends it for the *client*. The server still holds Resume text, ResumeProfile and transcript until the idle window expires. "Start new Interview Session" likewise leaves the previous session (already ended, transcript kept) in memory. There is no `DELETE` on a session, and no rule that ending purges Resume text immediately (versus at report delivery).

**Proposed AD-21 — Session data classes and lifecycle table (amends AD-4).**
- Define the data classes and their purge point: `ResumeText` (purged the moment the session enters `Ended`, before any report call), `ResumeProfile` (decided explicitly; recommended: purged with Resume text, the report is built from the transcript and questions only, or, if the coaching needs it, kept until the report is delivered and named an accepted exception in AD-4), `Transcript` and `Questions snapshot` (kept until delivery or expiry), `Report` (cached in the session once built).
- The report use case caches its result in the session, so a lost response is recoverable: the report endpoint is idempotent and returns the cached report until the session is removed.
- Removal happens on (a) an explicit `DELETE /api/interview/sessions/{id}` that the SPA sends when the user leaves the report (Start new, and best-effort `keepalive` on `pagehide`), and (b) expiry. "Delivered" is no longer inferred by the server.
- Expiry: `Interview:SessionIdleMinutes` (default e.g. 30). Any request touching the session, and every 30 s while a stream or report call is running, refreshes it. A session in `Ended` awaiting its report keeps a separate, longer TTL. In-flight operations hold a lease so expiry cannot remove a session under them.
- The SPA maps `session-not-found` to a defined UX line (new copy in EXPERIENCE.md) and a defined route (`/interview` fresh).

---

## 4. Retriever cache and bank writes: two adapters, no seam; "empty bank" has two meanings (H)

AD-7: only `IQuestionBank` writes. AD-8: `IQuestionRetriever` loads all matching embeddings, scores them, and "the cache is invalidated on every `IQuestionBank` write". The Capability map puts "sqlite bank, retriever" in Infrastructure as separate things.

**Pair 4a (invalidation never wired).**
- Story A: `SqliteQuestionBank` (add, import, delete). Obeys AD-7 to the letter; nothing in AD-7 or AD-1 says the bank calls anything after a write.
- Story B: `InMemoryQuestionRetriever` with a cache, and an `Invalidate()` method. Nothing calls it.
- Result: FR-3 breached: a deleted Question keeps being asked (or an imported one never appears, FR-1/FR-2 "available to the next Interview Session"). Both stories pass their own unit tests.

**Pair 4b (invalidate versus load race).** Retriever starts loading from the DB; an import commits and calls `Invalidate()`; the loader then publishes its (stale) snapshot over the cleared cache and it stays stale until the next write. Import during retrieval is exactly the race the brief asked about; a lock around the cache does not fix it without a version stamp.

**Pair 4c (what is "empty"?).** AD-3: "When the Question Bank is non-empty a session holds at least one ... Question Bank Question." FR-5: empty bank means the bot says so. Session-start story checks `IQuestionBank.CountAsync() == 0` to show the notice. Retriever story returns an empty list whenever every row mismatches the embedding profile (AD-8/AD-9) or the embedding is null. Result: bank has 87 rows, retriever has 0 (during or after an embedding-provider change, or mid re-embed), no notice is shown, no bank Question is asked, and the AD-3 guarantee is silently violated.

**Pair 4d (delete and DbContext lifetime).** The retriever is a singleton cache; EF `DbContext` is scoped. One agent injects `DbContext`, another `IDbContextFactory`; the singleton captures a disposed context. Same for the singleton `ISessionStore`, which touches nothing DB-related, so this only affects the retriever.

**Proposed AD-22 — One owner of the vector index; monotonic version (amends AD-7, AD-8).**
- `IQuestionBank` owns the *in-memory index* as well as the rows: it exposes `GetEmbeddedAsync(profile)` returning an immutable snapshot with a `version`, and every write bumps `version` under the same lock that commits. `IQuestionRetriever` (Core service) scores over that snapshot and never caches on its own. No separate invalidation call exists to forget.
- A loader may publish a snapshot only if `version` is unchanged since it started.
- "Bank empty" is defined once, by a single Core method `QuestionBankState` returning `Empty`, `Ready`, or `NotIndexed` (rows exist but none match the profile). `NotIndexed` is surfaced as a distinct notice and does not silently run Resume-only.
- The DbContext strategy (`IDbContextFactory<T>`, pooled) is stated.

---

## 5. Import: atomic batch versus unique constraint, concurrent imports, report shape, parser boundaries (M-H)

AD-14: atomic import and add; duplicates skipped by normalized text; embedding failure saves nothing. AD-7: `normalized_text` unique. AD-13: import parsing is a pure Core function.

**Pair 5a (atomic batch meets the unique index).**
- Story A (import): parses, filters against existing normalized texts (a read), embeds 87 texts (slow on Ollama), then inserts in one transaction.
- Story B (single add) or a second concurrent import of the same file: same steps.
- Both pass the read-time existence check, both embed, both insert; the second hits the unique constraint and, because the batch is atomic, the *entire* import fails with a 500 (no slug exists for a conflict), or the transaction is held open across the embedding calls and SQLite answers `database is locked` to every other request (list, delete, interview retrieval). Duplicates *inside one file* cause the same failure unless de-duplicated in the parser.

**Pair 5b (report shape).** AD-14 says duplicates are "reported as 'Already in the Question Bank'". UX has one list headed "N parts could not be read:". Backend returns one `skipped` array mixing unreadable parts and duplicates; frontend renders the array under the "could not be read" header, so duplicates are described as unreadable (or the frontend filters by a `reason` string the backend spelled differently). Zero found ("No Questions found." plus hint) versus all-duplicates ("0 added") is indistinguishable in the DTO. Same for single add: duplicate returns 200 `{added:false}` (frontend toast says "Question added.") or 409 with no slug.

**Pair 5c (parser boundary and the shared extractor).** AD-13 says a Question "starts at a line beginning `Q<number>.`" and its answer is the text up to the next; it does not say where the Question text *ends* (rest of the line? up to a blank line?). PdfPig yields visual lines, so an answer hard-wrapped in a PDF is many lines and a wrapped Question is two; Word adapter yields real paragraphs. Parser agent tests against Word paragraphs and PDF Questions mis-split. Word auto-numbered `Q1.` (list numbering) is not in the paragraph text at all; OpenXml text extraction drops it and the sample "87 Questions" silently becomes 0. AD-13 does not say the sample is a fixture the parser tests must pass.

**Pair 5d (allowed types per endpoint).** `IDocumentTextExtractor` has one interface. Import accepts Word, PDF, plain text; Resume accepts Word, PDF only (FR-4, UX "Choose a Word or PDF file."). Plain text has no content signature (AD-13 detects by signature), so "UTF-8 fallback" accepts anything that is not docx/pdf, including binary, and accepts a `.txt` Resume. The endpoint agents each decide the allowed set; the client-side pre-check is by extension and the server's by signature.

**Pair 5e (normalization owner).** "Trimmed, whitespace collapsed, case-folded" is stated in prose; the Core parser dedupes with `ToLowerInvariant`, the Infrastructure bank stores `normalized_text` with its own routine (or SQLite `COLLATE NOCASE`). They disagree on Unicode, so the unique index and the pre-check disagree.

**Proposed AD-23 — Import mechanics (amends AD-13, AD-14).**
- Embed outside any transaction; insert in one short transaction with `INSERT ... ON CONFLICT(normalized_text) DO NOTHING`; conflicts are counted as `alreadyInBank` skips, never a batch failure. In-file duplicates are removed in the parser.
- One Core function `QuestionText.Normalize` (NFC, trim, collapse whitespace, `ToLowerInvariant`) used by parser, bank and tests; the DB stores its output.
- Import response DTO fixed: `{ added: int, alreadyInBank: int, unreadable: [{ position, reason }] }` with a closed `reason` list (`empty-question`, `empty-text`, `no-question-marker`). The UI shows the three numbers separately.
- Single add: 201 with the row, or 409 slug `question-exists`.
- Extractor takes an `AllowedKinds` argument; Resume allows `docx`, `pdf`; import allows `docx`, `pdf`, `txt`. `txt` is accepted only if valid UTF-8 with no NUL bytes. `.doc` and other OLE files map to `unsupported-file-type`.
- The parser is defined as: Question text = the remainder of the `Q<n>.` line; answer = following lines until the next marker; the sample file is a committed test fixture that must yield 87. Word extraction renders list numbering into text or the fixture proves it is not needed.
- Optional: `Uploads:MaxBytes` enforced at Kestrel so oversize returns ProblemDetails, not a bare 413.

---

## 6. Frontend: who owns interview state and the in-flight request; stale tabs (M-H)

AD-16: "`fetch` in one `src/api/` module plus `useState`, `useReducer` and Context." UX: "leaving for Question Bank and coming back resumes [the session]"; reload ends it; the report is memory-only.

**Pair 6a (component state versus provider).**
- Frontend story "interview chat": `useReducer` inside `InterviewPage`, streaming `fetch` started in an effect, aborted on unmount (correct hygiene).
- Frontend story "app shell and routing": `react-router` routes; page components mount and unmount per route.
- Result: navigating to Question Bank mid-stream unmounts the page, aborts the stream (server: a Stop, answer committed, no reply), loses `sessionId` and all messages (UX says the session resumes), and orphans the server session with the Resume in memory. Or the chat agent puts state in a Context created by the *page*, with the same effect.

**Pair 6b (which fetch owns the report).** Report request lives in `ReportPage` (fetch on mount, abort on unmount) versus in the interview provider. Navigating away and back re-issues it; two concurrent generations against one session unless AD-20/21 guard it.

**Pair 6c (stale Question Bank tabs).** Two tabs on `/question-bank`: tab A deletes Question 5; tab B still lists it and clicks delete. Backend returns 404 (`invalid-request`); UX "Delete failed: dialog stays open with inline error and Retry" makes Retry a permanent failure loop. Same with tab B adding a Question tab A already added (duplicate, see 5b), and after an import in tab A tab B's list and count heading are wrong until a manual reload; nobody owns a refetch policy.

**Pair 6d (one Retry, three meanings).** UX has chat Retry, report Retry, and inline Retry on Question Bank. `src/api/` agent implements one generic retry-with-backoff wrapper; chat agent must not retry POST turns automatically (non-idempotent, see 2). No rule on which calls may be auto-retried.

**Proposed AD-24 — Frontend state ownership (amends AD-16).**
- One `InterviewProvider` mounted above `<Routes>` owns `sessionId`, the rendered messages, the in-flight `AbortController`, the report and its status. Pages are views. Only an explicit Stop, End, or "Start new" aborts a request; route changes do not.
- `src/api/` exposes one function per endpoint from the contract table (AD-19); no page calls `fetch`. No automatic retries of non-idempotent calls; Retry is always user-initiated.
- Question Bank list is refetched after any successful write and on window focus; delete is idempotent (204 if already gone).
- The provider sends `DELETE /sessions/{id}` per AD-21 on "Start new" and on `pagehide`.

---

## 7. Snapshot, `questionNo` and follow-ups: what is "a Question" in the transcript and report? (M)

AD-4: session holds "planned Questions". AD-8: retrieval is per turn, from "the ResumeProfile and the latest exchange". Conventions: "report and transcript reference `questionNo` (1-based position in the session)". AD-3: a follow-up "does not advance the count". AD-11: strengths and weak spots carry `questionNo` and a short quote.

**Pair 7a (planned up front versus chosen per turn).**
- Session-start story reads "planned Questions" literally: retrieves ten in one pass at creation and stores them.
- Turn story reads AD-8 literally: retrieves top-k every turn from the latest exchange.
- Both fit the spine. One yields a fixed queue and the AD-3 mix guarantee trivially; the other needs reserved slots. Deferred says the ratio is "tuned in build" but not that the plan structure is undecided. The follow-up decision changes what "the latest exchange" is, so the two designs give different Question sequences and different tests.

**Pair 7b (live row versus snapshot).** Interview story stores `bankQuestionId` in the plan. Coaching story loads the Reference Answer by id at report time. A Question deleted mid-session (FR-3 allows this in another tab) makes the report throw or silently drop the comparison; or the row id is reused (EF SQLite `INTEGER PRIMARY KEY` without `AUTOINCREMENT` reuses the highest rowid after delete) and the report compares an answer to a different Question's Reference Answer. AD-7 says `id (integer, autoincrement)` but EF's default mapping does not add SQLite `AUTOINCREMENT`.

**Pair 7c (follow-up and `questionNo`).** Transcript story gives a follow-up and its answer the parent's `questionNo`; report story treats each answered *transcript entry* as one Question review ("one structured review call per answered Question", AD-12), producing two reviews for Question 3, and the UI's "See the answer" (scrolls to that Question's review card) has two targets. Or the reverse: the review call sees only the first answer, ignoring the follow-up the report must quote from. The "quote from that answer" in AD-11 does not say which answer of the pair.

**Pair 7d (wording versus stored Question).** "The model only writes wording": the transcript holds the model's reworded bank Question; the report displays "the Question" from the snapshot (original text) or from the transcript (reworded). The two differ; the user sees a different question in the report than they answered.

**Pair 7e (quote is unverified).** AD-11 requires a `quote` per strength and weak spot; nothing requires code to check it is a substring of the answer. A 4b model paraphrases; the UI shows a fabricated "quote of your answer". FR-8 requires each to refer to an actual answer.

**Proposed AD-25 — Session question snapshot and transcript keys.**
- The session holds `SessionQuestion { questionNo, source, bankQuestionId?, askedText, referenceAnswer? }`, copied at selection time; the report never reads the bank. Bank ids are `AUTOINCREMENT` and never reused.
- Question selection is per turn (or up front, but say which); the mix guarantee is enforced by a Core rule that reserves the last unfilled source when remaining slots equal unmet minimums.
- Transcript entries are `{ seq, role, kind: question|followUp|answer|closing, questionNo }`. A follow-up and its answers carry the parent's `questionNo`. There is exactly one review per `questionNo`, built from all answers to it. `quote` must be a case- and whitespace-insensitive substring of one of those answers; Core verifies and drops or retries a review that fails.
- The report DTO includes `endedEarly` and `answeredQuestionCount` so the UI "You ended early... N Questions" line is not derived twice. `askedText` is what the Interviewee saw and what the report displays.

---

## 8. Re-embed and profile: two owners, concurrent with everything (M)

AD-9: re-embed "on startup, and on `POST /api/admin/re-embed`". AD-10: the profile string is `provider:model:dimensions`, produced by Infrastructure.

**Pair 8a (two triggers, two owners).** A hosted `BackgroundService` that reconciles on startup, and an endpoint calling `IQuestionBank.ReEmbedAsync`. Written by different agents; one batches 32 rows and commits per batch, the other in one transaction. Hitting the endpoint while the startup job runs (or twice) has two writers updating the same rows; a delete between read and update throws `DbUpdateConcurrencyException`; an import inserting during re-embed sees a half-migrated bank. If Ollama is not running at startup, startup either fails (`ValidateOnStart` mindset) or logs and continues with no retry.

**Pair 8b (live sessions).** Re-embed clears profile rows mid-session; the retriever ignores mismatching rows (AD-8), so a running session's remaining turns lose the bank without notice (see 4c).

**Pair 8c (profile derivation).** The profile has a `dimensions` component. The config section has `Type, Model, Endpoint, ApiKey`, no dimensions. Infra agent A hard-codes 768 for nomic; agent B probes the provider with a test string at startup (a model call in the composition root, which must be reachable before `ValidateOnStart` passes); OpenAI `text-embedding-3` accepts a `dimensions` option. Also the prefix scheme (AD-10 `search_document:`) changes the vectors but is not part of the profile string, so changing the decorator prompt yields silently mixed vectors.

**Pair 8d (nullable embedding).** `embedding` is nullable; AD-14 says embedding failure means nothing is saved. Two agents disagree on whether import may persist rows with null embeddings when the provider is down (and then re-embed later), which is the natural user-friendly behaviour but contradicts AD-14.

**Proposed AD-26 — One re-embed job (amends AD-9, AD-10).**
- A single `IEmbeddingReconciler` in Core, called by both the startup hosted service and the endpoint; it takes a process-wide single-flight lock (second call returns 409 `reembed-in-progress` or joins), works in batches, tolerates rows deleted mid-run, and reports `{ processed, failed }`.
- Add `Providers:Embedding:Dimensions` (optional) and define the profile string as `type:model:dimensions:promptScheme`, where `promptScheme` is a version tag of the AD-10 decorator. Dimensions unset means probe once at startup and cache.
- `embedding` null is allowed only for rows awaiting reconciliation, and only the reconciler creates them (import and add stay atomic per AD-14).
- Startup failure to reach the embedding Provider does not block startup; it is logged and the state is `NotIndexed` (AD-22).

---

## 9. Interview-turn prompt assembly and the model context window are unowned (M)

AD-12 budgets the *coaching* prompt. Interview-turn prompts hold Resume information, ResumeProfile, transcript, retrieved Questions, and instructions. Ollama's default context is small (it silently truncates), and NFR-16GB pushes to small models.

**Pair 9a.** Turn story builds a prompt with the full transcript; Retrieval story builds queries from "the latest exchange". Neither owns the windowing. By turn 8 the prompt overflows `num_ctx`, Ollama drops the *oldest* tokens (the system prompt and the Resume/Profile), and the model stops following the state machine's wording instructions with no error anywhere. The Ollama adapter (Infrastructure) never sets `num_ctx`, since no Core rule tells it to.

**Pair 9b (Resume text versus profile in prompts).** One agent embeds raw Resume text in every turn (long, sensitive); another uses only ResumeProfile. Decides what the NFR purge can guarantee (3a).

**Proposed AD-27 — Prompt assembly is one Core component with a token budget.** A single `TurnPromptBuilder` owns interview prompts: system prompt, ResumeProfile (not raw Resume text after session start), the last N exchanges, the target Question. It enforces a configured `Providers:Chat:ContextTokens` budget with a documented truncation order (drop oldest exchanges first, never the system prompt). The Ollama adapter sets `num_ctx` from the same option. A test with a fake `IChatClient` asserts prompts never exceed the budget and never contain a Reference Answer (AD-8).

---

## 10. Loopback is not a boundary for a browser; logging middleware leaks content (L-M)

**Pair 10a.** AD-17 binds Kestrel to loopback and configures no CORS. Any web page open in the user's browser can still send a *simple* cross-origin POST to `http://localhost:5xxx/api/...` (multipart form, `text/plain`), which executes even though the response is unreadable: `POST /api/admin/re-embed`, or an import/add of junk into the shared Question Bank. DNS rebinding widens this. Endpoint agents follow AD-15 exactly and none add an Origin or Host check.

**Pair 10b (logging).** AD-17 says content is never logged. A dev adds `ChatClientBuilder.UseLogging()` or `UseOpenTelemetry(enableSensitiveData)` (Microsoft.Extensions.AI middleware, natural in AD-2's composition root) and EF `EnableSensitiveDataLogging()` while debugging; prompts (Resume-derived), answers and Reference Answers reach logs at Trace/Debug. Not a rule violation by intent, but no AD names the middleware.

**Proposed AD-28 — Local-hardening rules (amends AD-17).** All non-GET endpoints require `Content-Type: application/json` or multipart plus a fixed custom header (`X-Requested-With`), and reject requests whose `Origin` is present and not the SPA origin; `Host` must be `localhost` or `127.0.0.1`. `UseLogging` / `UseOpenTelemetry` with sensitive data and EF sensitive-data logging are banned in the composition root; a test asserts a sentinel Resume string never appears in captured log output.

---

## 11. Smaller collisions (L-M), fold into the ADs above

- **Follow-up on the last Question.** AD-3 does not say whether the final planned Question can get a follow-up. Core allows it; UX says "after the last planned Question is answered the bot says so and the app opens `/interview/report`". Decide; state it in AD-3.
- **`session-not-found` and other slugs have no UX copy.** EXPERIENCE.md has copy for provider failure and unreadable Resume only. `unsupported-file-type`, `file-too-large`, `session-not-found`, `session-busy` need lines; the frontend otherwise shows `detail`, which AD-15 forbids, or a generic "Oops" that UX bans.
- **`provider-unavailable` also covers schema-deserialization failure (AD-11)**, so the UX copy "Check that it is running" is wrong for a model that answered garbage. Add slug `provider-bad-response` or a distinct copy.
- **Report has no cancel and no progress.** N+1 sequential model calls on a local model is minutes; the report endpoint is a single held request with no Stop. Either add a Stop (aborts, returns to `Ended`, Retry available) or state a per-call timeout (`Providers:Chat:TimeoutSeconds`) and a hard total; the client "Preparing your Coaching Report..." has no hang path.
- **Empty report fields.** "At least one strength and one weak spot" cannot be enforced by a JSON schema; nothing says what happens if the model returns none (retry once per AD-11, then a report with a fallback strength?).
- **Config exposure versus UX needs** (`plannedQuestionCount`, `Uploads:MaxBytes`): return via the session-creation DTO and a `/api/limits`-style non-sensitive read model, and amend AD-17's "no endpoint returns configuration" to "no endpoint returns provider or secret configuration".
- **EF schema creation** (`EnsureCreated` versus migrations) is unstated; agents split. State: migrations, applied at startup before the reconciler.
- **Float32 BLOB layout** (little-endian, no header, length = dimensions x 4) is written by the bank and read by the index; put it in AD-7 to keep two Infra classes in agreement.
- **Vite proxy and SSE.** Response compression or a buffering middleware on the SSE routes stalls tokens until the end of the stream. State: no response compression on `/api/interview/**`, flush per event.
- **Dev hot reload.** `dotnet watch` restarts the process on every save and every in-memory session dies; the SPA must treat `session-not-found` mid-chat as a normal state (see 3).
- **Client-side and server-side type checks** disagree (extension versus signature); the client pre-check is advisory only and the server's slug wins (state in AD-13).

---

## Suggested amendment order

1. AD-19 (contract table) and AD-20 (turn states) before any story is cut; every backend and frontend story depends on them.
2. AD-21 and AD-25 next: session data classes, lifecycle, snapshot. These decide the Core records.
3. AD-22 and AD-23 before the Question Bank stories.
4. AD-24, AD-26, AD-27, AD-28 can land with the stories they gate.
