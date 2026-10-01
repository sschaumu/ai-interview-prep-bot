# UX Source Extract: ai-interview-prep-bot

Sources: PRD (`prd.md`), PRD addendum, brief, brief addendum (all 2026-09-21). "Not stated" = sources are silent.

## 1. Product, form factor, constraints
- Website where an Interviewee uploads a Resume and takes a mock job interview with an AI chat bot; bot asks Resume-based and Question Bank questions, follows up, then coaches (PRD §1).
- Also a learning vehicle for Simon (React from zero, modern C#, RAG). Learning commentary applies to the build process, not the product (PRD §1).
- Form factor: "React web app backed by a C# API" (brief, The solution); "chat-based mock interview in the browser", text only (brief Scope, `[ASSUMPTION]`). Mobile/desktop/responsive: not stated.
- Stack facts: React frontend; C# backend; local models by default via Ollama, 16 GB machine, small models ~3B-8B quantized, GPU unknown (brief Constraints; brief addendum). Paid Providers swappable by configuration (FR-9). Secrets never reach the browser (PRD NFR).
- Streaming: "Chat responses appear as they are generated, so the user is not left on a blank screen" `[ASSUMPTION]` (PRD §4.5 NFRs).
- No accounts; anonymous, per session (brief Who this serves). Not production-grade; "learning value comes before polish and scale" (PRD §5).
- Runs locally in v1; hosting is a late learning step (PRD §6.2).

## 2. Users and journeys
- Primary: Simon (builder/learner; also the Question Bank admin). Secondary: Interviewee, "anyone who uploads a resume", anonymous (brief).
- Non-users: recruiters/interviewers (PRD §2.2).
- Jobs: Interviewee wants "a realistic interview tailored to their background and get specific feedback" (PRD §2.1).
- **UJ-1. Priya practices for a backend role** (persona illustrative `[ASSUMPTION]`; mid-level dev, interview Friday). Entry: no account, bank loaded. Path: opens site, uploads Resume as Word file; bot starts Interview Session; first question about a project on her Resume; later questions from Question Bank; she types answers; bot asks follow-up when answer is thin; session ends after last question. Climax: Coaching Report naming what she did well, where weak, comparison with Reference Answer where one exists. Resolution: reads report, starts a new session or leaves; Resume not kept. Edge: unreadable Resume (e.g., scanned PDF) - bot says so and asks for a different file.
- **UJ-2. Simon grows the Question Bank.** Entry: site running locally. Path: uploads a file, app reports how many Questions found; then adds one Question by hand. Climax: next Interview Session asks a Question from the uploaded file. Resolution: bank keeps everything. Edge: unreadable part of a file - report lists what was skipped, "nothing disappears silently".

## 3. User-facing capabilities by FR
Surfaces implied (not named in sources): Resume upload/start, chat interview, Coaching Report, Question Bank list/import/add. Layout/navigation: not stated.
- **FR-1 Import from file** (Word, PDF, plain text; with/without Reference Answers). Input: file upload. Output: count of Questions added + list of anything unreadable. States: success report, partial-skip list. Loading/progress during import: not stated (addendum notes extraction may need an LLM-assisted step, which is slow/unreliable on small models).
- **FR-2 Add single Question**: input Question text + optional Reference Answer. Output: available to next session. Validation/error states: not stated.
- **FR-3 View and delete**: list showing every Question; delete any; deleted never asked again. Delete confirmation, pagination/search for ~87+ items: not stated.
- **FR-4 Upload Resume** (Word or PDF): text extracted, used that session only; if no text extractable "the user is told and asked for another file"; not stored after session.
- **FR-5 Mixed questions**: bot asks Resume-based + bank questions; if bank empty, bot runs Resume-only "and tells the user".
- **FR-6 Follow-ups**: refers to what the user said; max one per Question `[ASSUMPTION]`.
- **FR-7 End session**: ends after planned number of questions or when user ends early (an "end early" control is implied); user then "taken to the Coaching Report".
- **FR-8 Coaching Report**: at least one strength and one weak spot, each tied to an actual answer; compares to Reference Answer where present; else rubric; early end -> report covers answers given.
- **FR-9/FR-10**: Provider config and re-embed are configuration/admin; no UI stated ("Changing the configuration", "can be re-embedded"). Whether re-embed has a UI trigger: not stated.
- Streaming (NFR): chat replies stream token by token `[ASSUMPTION]`.
- Loading, empty, error states explicitly stated: unreadable Resume, unreadable import parts, empty Question Bank notice, streaming instead of blank screen. All others: not stated.

## 4. Interview Session specifics
- Length: about 10 questions `[ASSUMPTION]` (FR-5; brief open question "fixed length?"). Progress indicator: not stated.
- Sources: at least one Resume-based + at least one Question Bank Question (FR-5). Behavioral questions ("Tell me about a time...") are generated from something specific on the Resume, not from the bank `[ASSUMPTION]`. Bank has no tags/categories/difficulty (addendum findings); retrieval by Question text only.
- Selection "for relevance to the Resume and the conversation so far" (FR-5).
- Follow-ups: when an answer is "thin or interesting"; at most one per Question (FR-6).
- Input is typed text; no voice/video (§6.2).
- End: after planned count or user ends early (FR-7).
- Coaching content (FR-8): strengths and weak spots referencing actual answers; two modes per question: (a) Reference Answer comparison where one exists; (b) simple rubric otherwise (Resume-based Qs, or bank Q without answer); behavioral rubric = situation, action, result `[ASSUMPTION]`. Report format/structure, scores: not stated (addendum: "structured output and a scoring rubric are the likely approach"). Reference Answers are long (one to ~ten paragraphs) and pulled only when coaching (addendum).
- Session history not kept (§6.2 out of scope).

## 5. Question Bank management UX
- One shared bank for the whole site; persists across sessions; no accounts so anyone can view and change it `[ASSUMPTION]` (§3, §4.1).
- Add single: Question + optional Reference Answer (FR-2).
- Bulk import: Word, PDF, plain text (FR-1). Format rule: each Question starts with "Q" + number + period (Q1., Q2., ...); answer is everything up to next Question; text before first Question (title, links) ignored; PDF/text use same pattern `[ASSUMPTION]`, other layouts best-effort. Parse on text pattern, not Word heading styles (addendum). Sample: 87 Questions, Q1-Q87, all with Reference Answers, .NET/Azure only.
- Parse feedback: number of Questions added + list of anything it couldn't read (FR-1); "nothing disappears silently" (UJ-2, addendum).
- Delete: yes (FR-3). Edit: deferred; "delete and re-add instead" `[ASSUMPTION]` (FR-3, §6.2). Tagging out of v1.
- List: shows every Question (FR-3); whether Reference Answer is visible in the list: not stated.

## 6. Resume upload
- Accepted: Word or PDF (FR-4, glossary). Brief said PDF and plain text (brief `[ASSUMPTION]`); the PRD supersedes: Word or PDF. Plain text for Resume: not stated in PRD.
- Handling: text extracted, used in that session only; deleted when session ends (FR-4, NFR). Resume ~2 pages, treated as one block of text (addendum).
- Error: no extractable text (e.g., scanned PDF) -> user told and asked for another file (FR-4, UJ-1). Max file size, other errors: not stated.

## 7. Look/feel, tone, accessibility, performance
- Look/feel, branding, visual style: not stated. Tone: coaching should be "specific, honest feedback" (PRD §4.4), "specific strengths and weak spots ... not generic advice" (brief). Working title "ai-interview-prep-bot - confirm" (PRD title note).
- Accessibility: not stated. Responsive/mobile: not stated.
- Performance/latency: no numeric targets. Only "responses appear as they are generated".
- Risks affecting UI: small local models are slower/weaker; "weaker at natural follow-ups and at judging answers, so coaching quality is most exposed" (PRD §4.5 Known risk); LLM-assisted import extraction unreliable on small local models (addendum); scanned PDFs have no text layer; long Reference Answers stress small-model context (addendum). Coaching report generation time and failure handling: not stated.

## 8. Glossary (use verbatim in UI copy)
Question Bank; Question; Reference Answer; Interviewee; Resume; Interview Session; Coaching Report; Provider. (PRD §3: "Terms in the Glossary are used exactly as written.") Definitions: Resume = Word or PDF, "used for one Interview Session only"; Interview Session = "one chat from Resume upload to Coaching Report". Note: the brief uses lowercase "resume", "question bank", "coaching feedback"; the PRD capitalized terms govern.

## 9. Open questions and [ASSUMPTION]s touching UX
Open (PRD §8/brief): free hosted API fallback acceptable?; shared editable Question Bank acceptable once hosted with no accounts?; fixed interview length? (brief).
Assumptions: persona illustrative; one shared bank, anyone can edit; PDF/text use "Q<n>." pattern; edit deferred; ~10 questions per session; behavioral questions from Resume; rubric (situation/action/result) when no Reference Answer; at most one follow-up per Question; responses stream; no tagging; no voice/video. Working title unconfirmed.
