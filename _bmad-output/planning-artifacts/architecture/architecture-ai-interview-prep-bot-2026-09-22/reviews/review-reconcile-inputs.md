# Review: ARCHITECTURE-SPINE reconciled against PRD, PRD addendum, EXPERIENCE.md (DESIGN.md skimmed)

Reviewer scope: what did not land, what contradicts an input, what leaves two builders free to build incompatible things. The spine was not modified.

## Verdict

Revise before build. The structure (hexagonal, MEAI ports, state machine in Core, in-memory sessions, SQLite, SSE over POST) is sound and every FR has a home. The gaps cluster in one place: the interview turn and session lifecycle contract between backend and frontend. The spine fixes the streaming style and the error format, but not the payload, the endpoints or the failure semantics that the UX depends on. Two builders (or a frontend and a backend story) would diverge there. The rest are medium-severity holes and a few internal contradictions.

Severity scale: High = builders will likely build incompatible or non-conformant things; Medium = a stated requirement or testable consequence is unprotected or an invariant can be violated; Low = copy, ownership or clarity gaps.

---

## High

### F1. No turn/session contract: the UX needs per-message metadata the SSE events and endpoints do not carry
Inputs: EXPERIENCE.md (Session strip "Question N of about 10", N counts Questions not follow-ups; Question-type label on every bot Question "earlier ones included", follow-ups carry none; Session ending "the bot says so and the app opens /interview/report"; Empty Question Bank first message; End interview disabled until one answer), PRD FR-5, FR-7.
Spine: AD-6 defines events `token`, `done`, `error` with no payload beyond a text delta and a problem slug. AD-15 lists no endpoints. Only `/api/question-bank/import`, `/api/interview/sessions` and `/api/admin/re-embed` are named anywhere.
What is missing:
- Endpoints for: list, add, delete Questions; submit answer; retry reply; end early; get report; retry report; delete session. Their verbs, request bodies and response DTOs.
- Per bot message: `source` (Resume | QuestionBank; behavioral reads Resume), `kind` (question | follow-up), `questionNo`, and the planned count ("about 10" must come from `Interview:QuestionCount`, not be hardcoded in the client). Without these the label and the strip cannot be built, and the transcript for the label on "earlier ones" has nothing to render from.
- A session-complete signal (last planned Question answered) so the client knows to navigate to the report. The `done` event should carry it, or a separate transition is needed.
- Who emits the fixed bot messages that are not errors: the empty-bank notice ("so I'll ask only about your Resume"), the interview-over message, the unreadable-Resume bubble. AD-15 says the frontend maps error slugs to copy, which covers the last one only. The empty-bank flag must be returned when the session is created, and it must be defined whether these lines are canned or model-written.
- Whether `POST /api/interview/sessions` streams the first Question or returns JSON and a second call streams it. AD-6 says "chat replies stream from a POST endpoint", singular.
- How pre-stream failures (HTTP ProblemDetails before the 200) and mid-stream failures (`error` event) are both handled by the client.
Fix: add a short "Interview turn contract" AD (endpoint table plus the `done` event payload and the message record: `{questionNo, source, kind, text}`, session `{sessionId, plannedCount, bankEmpty}`), and put the message record in Core.

### F2. AD-5 turn commit protocol leaves failure and retry semantics ambiguous
Inputs: EXPERIENCE.md (Composer "A draft survives a failed send"; "Stopped, failed or hung reply: partial text kept... Retry regenerates that reply. Composer stays off until Retry succeeds"; Thinking indicator "Stop and Retry" after the hang threshold; "leaving for Question Bank and coming back resumes it").
Spine: AD-5 "An answer is committed on receipt... Retry is its own endpoint and regenerates the reply to the last committed answer."
Open cases:
- The first Question has no committed answer. A failed or stopped first Question after upload has nothing for Retry to regenerate. (The UX hang/Stop/Retry applies there too, and the local model's cold start makes it likely.)
- No turn id or idempotency key. If the POST fails at the network level the client cannot know whether the answer was committed. "Draft survives a failed send" (resend) and "answer already committed, use Retry" become a duplicate-answer bug. The client needs a rule, and the server needs a turn identity or a "an answer is already awaiting a reply" response.
- Retry vs a still-running stream (the hang state offers Retry while the first stream may not have been cancelled). Concurrency per session is undefined: one in-flight turn per session should be enforced server-side with a defined slug (`invalid-request`, or a new one), not only by disabling the composer.
- What Retry regenerates. Does the state machine keep its decision (same next Question or follow-up, count not advanced, Question not yet marked "asked" for retrieval exclusion) or re-decide? Left free, a Retry could pick a different Question, or a stopped reply could burn a slot.
- Route change while streaming. If the stream component unmounts and the fetch aborts, that is a Stop with no user action; the UX says coming back "resumes" the session. The session state, the in-flight stream and the report must live in a provider above the router (see F11), or the abort must be defined as Stop.
Fix: extend AD-5 with the first-turn case, a per-session single in-flight turn rule, decision persistence on Retry, and an idempotency key for answer submits.

---

## Medium

### F3. A live session can ask a Question the user has deleted, and coaching may lose its Reference Answer
Inputs: PRD FR-3 ("never asked in a later Interview Session"), EXPERIENCE.md Delete copy ("It won't be asked again."), FR-8 (compare to Reference Answer).
Spine: AD-4 says the session holds "planned Questions" (a snapshot); AD-12 says the review uses "that Question's Reference Answer". Not stated: whether the session snapshots Reference Answers, or reads them by id at report time.
Consequence: a Question deleted mid-session is still asked (contradicting the UX copy's plain reading), and if references are read by id at report time the Reference Answer vanishes and the report silently falls back to the rubric. Decide: snapshot Question text and Reference Answer into the session at the time the Question is asked, and state that a delete affects only Questions not yet asked (retrieval reads live).

### F4. AD-3's source-mix invariant conflicts with AD-8/AD-9 and with a small or stale bank
Spine internal, against FR-5 ("at least one Resume-based and one Question Bank Question"; empty bank runs Resume-only and tells the user).
- AD-3 says "when the Question Bank is non-empty a session holds at least one Bank Question". AD-8 says retrieval ignores rows whose `embedding_profile` mismatches, and AD-9 allows a period of mismatch (Provider changed, re-embed not yet run or failed). The bank is then non-empty but returns nothing: the invariant cannot be met and the empty-bank notice is wrong. Define "retrievable bank" as the emptiness test, and say what the user sees when rows exist but none match.
- AD-9 re-embeds "on startup". If Ollama is down at startup, does the app fail to start, or start and log? Unspecified, and it hits the local-first default (Ollama is "not yet installed").
- The bank can hold fewer Questions than `Interview:QuestionCount`, or fewer than the slots planned. The fill rule (Resume-based questions fill the rest) is not written down.
- AD-4 says "planned Questions" (plan up front) but AD-8 and the sequence diagram retrieve per turn from "ResumeProfile and the latest exchange". Which is it? It changes how the strip's "of about 10" and the mix are guaranteed.

### F5. No context budget for interview turns or the Resume; the 16 GB NFR is protected only for coaching
Inputs: PRD NFR "Runs on a 16 GB machine using small local models"; addendum ("Resume is short enough to sit whole in the prompt"; Reference Answers are long; small local model context).
Spine: AD-12 budgets the coaching prompt only. Nothing bounds (a) Resume text length after extraction (the only limit is 10 MB of file, AD-14), (b) how much transcript goes into each turn prompt over 10+ Questions plus follow-ups, (c) the Ollama context window setting (Ollama's default `num_ctx` is small and silently truncates, which drops the system prompt first). The default `gemma3:4b` will behave badly and silently.
Also unspecified: when and how the `ResumeProfile` is built (an extra structured call at session start, during the "Reading..." state), its record shape, and its failure path (AD-11 retry, then error; does the session start without it?). The addendum only lists it as an option, so the spine adopted it without defining it.
Fix: a `Interview:MaxResumeChars` (reject or truncate with a defined slug), a transcript window rule, and an explicit `num_ctx` option on the Ollama adapter.

### F6. The Coaching Report record and generation flow do not carry what the UX renders or the PRD tests
Inputs: PRD FR-8 (at least one strength and one weak spot, each referring to an actual answer; compare to Reference Answer where it exists, else rubric, SAR for behavioral; early-end covers answers given). EXPERIENCE.md (per-Question review shows Question, answer given, Reference Answer or rubric feedback; "You ended early. This report covers the N Questions you answered."; "See the answer" link).
Spine: AD-11 record "summary, strengths, weak spots, per-Question reviews"; each strength/weak spot has `questionNo` and a quote.
Gaps:
- Review item shape: no field for `basis` (referenceAnswer | rubric), the Reference Answer text to show, or the user's answer. `endedEarly` and `answeredCount` are absent.
- The minimum of one strength and one weak spot is not enforced. AD-11's retry covers deserialization only, so an empty list passes.
- The quote is not required to be a substring of the answer. Small models will invent quotes; this is the FR-8 "referring to an actual answer" testable consequence, and a substring check is cheap.
- Follow-up answers: which Question do they belong to when reviewing and quoting (`questionNo` counts Questions, not follow-ups)? Which Question kind (behavioral vs technical) drives the SAR rubric, and who sets it for a Resume-generated Question? The session's Question record needs a `kind`.
- AD-12 makes N+1 model calls. Failure and retry semantics are absent: does one failed call fail the whole report, is Retry idempotent and re-runs all, is there an HTTP timeout or per-call cap? With a 4B local model, 11 sequential calls is minutes, all under a non-streamed "Preparing..." message.

### F7. Resume-deletion NFR: what is purged, when, and what survives a reload is undefined
Inputs: PRD NFR "The Resume is deleted when the Interview Session ends"; FR-4 "not stored after the session ends"; EXPERIENCE.md "Reloading ends it".
Spine: AD-4 purges Resume text at interview end but keeps the `ResumeProfile` (structured facts derived from the Resume: skills, roles, projects) and transcript until the report is delivered. Idle expiry has no value. A reload (which "ends" the session per UX) or closing the tab leaves the Resume text in server memory until an unspecified idle expiry, and there is no `DELETE session` call or "Start new Interview Session" hook.
Consequence: the NFR is met for the happy path only; a reviewer can reasonably say ResumeProfile is the Resume. State that ResumeProfile is Resume-derived and purged with it (or must be dropped before the report, with coaching working off the transcript alone), define `Interview:SessionIdleMinutes`, and add an explicit end/delete endpoint the client calls on new-session start and best-effort on page hide.
Also: `session-not-found` (expiry, server restart, reload race) has no UX copy and no client behavior defined.

### F8. Import: "unreadable parts", duplicate-versus-zero, per-endpoint file types, and the 87-Question acceptance test
Inputs: PRD FR-1 (report added count and what it could not read; sample yields 87), UJ-2, addendum (nothing disappears silently); EXPERIENCE.md (Import result panel "42 Questions added." / "2 parts could not be read:"; "Zero found: No Questions found." plus hint; Resume takes Word or PDF only, import takes Word, PDF or text).
Spine: AD-13 says "Anything that cannot be read is returned in the import report with a reason". With a pure-regex parser and no model, "cannot be read" is undefined. Candidates: empty Question text, empty answer, gaps or duplicate numbers, a `Q<n>.` line inside an answer. Two builders will report different things, and the panel's "N parts" count has no source. Also:
- The import report DTO (`added`, `skipped duplicates`, `unreadable[{position, reason}]`) is not defined. AD-14's duplicates are "reported" but UX has no place for them, and re-importing the sample gives 0 added and 87 skipped, which UX would render as "No Questions found." (wrong).
- The extractor accepts plain text for Resume too. FR-4 and the UX allow only Word or PDF for a Resume; `unsupported-file-type` must be applied per endpoint. Say so.
- Single add (FR-2): a duplicate has no UX treatment and no status code or slug.
- PDF extraction ("one paragraph per line") through PdfPig yields wrapped lines, so a wrapped Question or answer splits at line ends. PRD only promises best-effort for PDF, but the test consequence "the sample file yields 87" should be pinned as a fixture test on the .docx, and PDF/text as best-effort.
- Deterministic-only parsing is a deliberate departure from the addendum's LLM-assisted fallback. That is acceptable under FR-1's best-effort wording, but the spine should say the "No Questions found" hint is the deliberate outcome for other layouts.

---

## Low

### F9. Error slugs do not line up with the UX copy table
- AD-11 folds "deserialization failure after one retry" into `provider-unavailable`-class. The UX copy for that slug is "I couldn't reach the Provider. Check that it is running", wrong for bad model output. Use a distinct slug (`provider-bad-output`) so the frontend can pick the right copy. The report copy is already different ("I couldn't create your Coaching Report...").
- `file-too-large`, `session-not-found`, `unsupported-file-type` (for import) and `invalid-request` (turn already in flight, empty answer, zero answers on End) have no UX copy. UX Open Questions 2 and 3 (size limit, re-import duplicates) are resolved by the spine (AD-14, 10 MB, skip) but that outcome is not fed back, so copy is missing.
- Max answer length is not bounded anywhere (only Resume/import bytes). A megabyte answer goes into the prompt.

### F10. Voice and tone constraints are not bound to prompts
EXPERIENCE.md: first person, plain, short sentences, no exclamation marks, emoji, praise inflation or numeric score, exact Glossary terms; "Feedback is specific to what the person said". The spine binds only "no numeric score" (AD-11) and puts prompts in `Core/*/Prompts` without a rule that they encode the voice. Add one line to the Consistency Conventions ("prompts carry the UX voice rules; a fake-model test asserts no numeric score field") so this survives as a build rule, not only a UX note.

### F11. Client-state lifetime is implied, not stated
AD-16 and the State row say "React state and Context". The UX needs the Interview Session (including an in-flight stream and unsent draft), and the Coaching Report to survive route changes between `/interview`, `/interview/report` and `/question-bank`, and to be lost on reload. Since AD-4 removes the server session once the report is delivered, the report exists only in client memory. Say where the provider lives (above the router) and that the report is held client-side after delivery.

### F12. Testable consequences without a test-strategy home
AD-18 covers backend fakes only. These PRD testable consequences have no named check: FR-1 sample file yields 87 Questions with Reference Answers; FR-5 mix and empty-bank behavior; FR-6 at most one follow-up and a follow-up that refers to the answer's content (a prompt-quality property a fake cannot prove, so an opt-in live check); FR-8 strengths/weak spots quote real answers (see F6); FR-10 whole-bank retrieval after re-embed. Frontend tests (Vitest is in the stack) have no rule. Add a one-line "acceptance fixtures" convention naming the sample bank and Resume files.

---

## Checked and landed correctly (no action)

FR-1 to FR-10 all have a home and binding ADs. In-memory sessions and reload-ends-session (AD-4 matches UX). Follow-up cap of one and "follow-up does not advance count" (AD-3 matches UX strip). No numeric score. Report not streamed (AD-6, AD-12 match UX assumption). Reference Answers never in interview-turn prompts (AD-8 matches addendum). Reference Answer pulled only for coaching, one at a time (AD-12). Parse on `Q<n>.` not heading styles, text before Q1 ignored (AD-13). Provider swap by config and embedding profile reconciliation (AD-2, AD-9, AD-10). Secrets server-side and loopback-only (AD-17). `localStorage` limited to theme and question-type switch inside try/catch (matches UX). Routes deferred to EXPERIENCE.md (AD-16). No Provider UI, no search or paging, no edit, no saveable report (Deferred, matches PRD/UX). DESIGN.md holds no architecture-bearing requirement beyond what EXPERIENCE.md states.

## Contradictions with inputs, summarized

1. AD-4 "planned Questions" snapshot vs FR-3 / UX Delete copy "won't be asked again" (F3).
2. AD-4 keeps ResumeProfile and transcript past "interview ends" vs NFR "Resume is deleted when the session ends", plus undefined idle expiry (F7).
3. AD-3 "non-empty bank has a Bank Question" vs AD-8/AD-9 "ignore mismatched rows" (F4, internal).
4. AD-11 "deserialization failure is `provider-unavailable`-class" vs UX copy for that slug (F9).
5. AD-13 extractor accepts plain text; FR-4 allows a Resume of Word or PDF only (F8).
