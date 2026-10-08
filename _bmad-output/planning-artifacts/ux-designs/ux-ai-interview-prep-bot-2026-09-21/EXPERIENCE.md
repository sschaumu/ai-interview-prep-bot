---
name: ai-interview-prep-bot
title: ai-interview-prep-bot Experience Spine
status: final
created: 2026-09-21
updated: 2026-09-21
sources:
  - ../../prds/prd-ai-interview-prep-bot-2026-09-21/prd.md
  - ../../briefs/brief-ai-interview-prep-bot-2026-09-21/brief.md
---

# ai-interview-prep-bot: Experience Spine

> Paired with `DESIGN.md` (tokens and component visuals). No mockups exist yet; this file is the contract.

## Foundation

React single-page app with client-side routing, backed by a C# API. Responsive web: desktop browser and phone. UI system: none, hand-rolled plain CSS on the custom properties defined in `DESIGN.md` (the user is learning CSS). Calm feel, text-only chat, no accounts, local in v1. Light and dark themes both supported. Working title `ai-interview-prep-bot` is used in the header, unconfirmed. `[ASSUMPTION]`

## Information Architecture

A persistent header (Header / nav) on every route: product name (from `{spacing.bp-md}` up), links Interview and Question Bank, Theme toggle. Two top-level destinations. `[ASSUMPTION]`

| Route (paths `[ASSUMPTION]`) | Surface | Purpose |
|---|---|---|
| `/` | Redirects to `/interview` | |
| `/interview` | Resume upload, then Interview Session | Upload a Resume, chat, end |
| `/interview/report` | Coaching Report, a separate scrollable view (not a chat message) | Read feedback; Start new Interview Session |
| `/question-bank` | Import from file, Add a Question, list | Grow and prune the Question Bank |

The Interview Session lives in app memory: leaving for Question Bank and coming back resumes it. Reloading ends it (the Resume and session history are not kept). The Coaching Report is memory-only too; opening `/interview/report` with none redirects to `/interview`. `[ASSUMPTION]`

**Surface closure**

| Need | Surface |
|---|---|
| Import Questions from a file (FR-1) | Question Bank: Import from file, Import result panel |
| Add a Question (FR-2); view and delete (FR-3) | Question Bank: Add a Question form; list with Confirm dialog |
| Upload Resume (FR-4), told if unreadable | Interview: Upload zone; bot message |
| Mixed Questions, empty-bank notice, follow-ups (FR-5, FR-6) | Chat |
| End early (FR-7) | Session strip: End interview |
| Coaching Report (FR-8) | `/interview/report` |
| Question type, theme | Session strip switch; Theme toggle |
| Provider choice, re-embed (FR-9, FR-10) | No UI in v1 (configuration). Provider failures appear as Toast / inline error. |

## Voice and Tone

Calm, plain, honest, short sentences. The bot uses first person inside chat; interface chrome stays neutral. Glossary terms exactly as written: Question Bank, Question, Reference Answer, Interviewee, Resume, Interview Session, Coaching Report, Provider. No exclamation marks, emoji, praise inflation, or numeric score. Feedback is specific to what the person said. Brand posture lives in `DESIGN.md`.

| Situation | Copy |
|---|---|
| Upload | "Upload your Resume (Word or PDF) to start an Interview Session." / "Your Resume is used for this Interview Session only and isn't kept." |
| Unreadable Resume | "I couldn't find any text in that Resume. It may be a scanned PDF. Please choose a different file." |
| Empty Question Bank | "Your Question Bank is empty, so I'll ask only about your Resume. You can add Questions on the Question Bank page." |
| Waiting | "Thinking..." / "This is taking longer than usual." |
| Provider failure | "I couldn't reach the Provider. Check that it is running, then try again." |
| End early | "End interview now? Your Coaching Report will cover the answers you've given so far." |
| Report | "Preparing your Coaching Report..." / "I couldn't create your Coaching Report. Your answers are still here. Try again." |
| Import | "42 Questions added." / "2 parts could not be read:" |
| Delete | "Delete this Question? It won't be asked again." |
| Duplicate Question | "That Question is already in the Question Bank." |
| Add failed (not Provider) | "I couldn't add that Question. Try again." |

Not this: "Oops! Something went wrong!", "Invalid file.", "Great job!", "Score: 7/10".

## Component Patterns

Behavioral. Visual specs live in `DESIGN.md` Components.

| Component | Behavioral rules |
|---|---|
| Header / nav | Sticky. Current link has `aria-current="page"`. Route change moves focus to the new view's h1. |
| Theme toggle | One button cycling System, Light, Dark. Default System (follows `prefers-color-scheme`, live). Light or Dark overrides and persists in `localStorage`; System clears it. Applies instantly. Name states mode and next action ("Theme: Dark. Switch to System."). The 3-state cycle is `[ASSUMPTION]`. |
| Button (primary / secondary / destructive) | One primary per view. Destructive only inside Confirm dialog. Busy buttons show a busy label and ignore a second press. |
| Upload zone | Click, keyboard (Choose file button) or drag-and-drop; one file at a time. Resume: Word or PDF. Import from file: Word, PDF or plain text, with a visible hint: "Each Question starts with Q1., Q2., and so on. The answer is the text up to the next Question." Wrong type is rejected inline before upload. |
| Chat bubble (bot / Interviewee) | Bot text streams in place. Auto-scroll only while the reader is at the bottom, otherwise a "Jump to latest" text button. Plain text; sent answers are not editable in v1. |
| Composer | Desktop: Enter sends, Shift+Enter adds a line. Touch: Enter adds a line, Send sends. `[ASSUMPTION]` Empty input cannot send. Disabled while the bot replies; Send becomes Stop while streaming. A draft survives a failed send. |
| Thinking indicator | Appears when an answer is sent, until the first streamed token or an error. After the hang threshold (tuned in build) it adds "This is taking longer than usual." with Stop and Retry. `[ASSUMPTION]` |
| Session strip | Interview Session only. "Question N of about 10": N counts Questions, not follow-ups. Holds the Show question type switch and End interview. End interview is disabled until one answer is sent, so the Coaching Report always has an answer to cite. `[ASSUMPTION]` |
| Question-type label | Hidden by default. The switch reveals a label showing where the Question came from (Resume or Question Bank; behavioral Questions are generated from the Resume, so they read Resume) above every bot Question, earlier ones included; follow-ups carry none. `[ASSUMPTION]` The choice persists in `localStorage`. |
| Coaching Report card | Strength and weak spot cards show a point, a short quote of the answer it came from, and a "See the answer" link that scrolls to and focuses that Question's review card. Review cards show the Question, the answer given, then the Reference Answer if one exists, otherwise rubric feedback. |
| Question Bank row | Question text clamped to 2 lines; a "Reference Answer" disclosure expands in place when one exists. `[ASSUMPTION]` Delete opens Confirm dialog. No edit in v1 (delete and re-add). |
| Form field | Visible label. Validate on submit; error linked by `aria-describedby`; first invalid field gets focus. |
| Confirm dialog | `role="alertdialog"`. Initial focus on Cancel; focus trapped; Esc cancels. Names the Question. After delete, focus returns to the list (next row, else heading); no undo in v1. Reused, non-destructive, for End interview. `[ASSUMPTION]` |
| Import result panel | Stays until dismissed or the next import. Count added, plus a list of unreadable parts with reasons. |
| Toast / inline error | Errors are inline, `role="alert"`, never auto-dismiss, with Retry where it helps. Confirmations ("Question added.", "Question deleted.") are toasts, `role="status"`, gone after 5 seconds. |
| Empty state | One sentence, at most one action. |

## State Patterns

| State | Surface | Treatment |
|---|---|---|
| Fresh start | `/interview` | Bot greeting, Upload zone, privacy line. |
| Uploading Resume | `/interview` | Filename and "Reading..."; second upload blocked. |
| Unreadable Resume | `/interview` | Bot bubble (copy above); Upload zone stays ready. Wrong file type: "Choose a Word or PDF file." `[ASSUMPTION]` |
| Empty Question Bank | Interview Session | First bot message says so and that the session runs on the Resume only. |
| Waiting / streaming | Chat | Thinking indicator until first token; then text grows in place, Send is Stop, log is `aria-busy`. |
| Stopped, failed or hung reply | Chat | Partial text kept, marked "Stopped." or shown with Provider failure copy; Retry regenerates that reply. Composer stays off until Retry succeeds; End interview stays available once an answer exists. |
| Session ending | Chat, then report | After the last planned Question is answered the bot says so and the app opens `/interview/report`. |
| Coaching Report generating | Report | h1 "Coaching Report", Thinking indicator "Preparing your Coaching Report...". Report renders when complete, not streamed. `[ASSUMPTION]` |
| Coaching generation failure | Report | Inline error with Retry; answers stay in memory. `[ASSUMPTION]` |
| Ended early | Report | Top note: "You ended early. This report covers the N Questions you answered." |
| Report loaded | Report | Summary, Strengths, Weak spots, Question review, Start new Interview Session. At least one strength and one weak spot. No numeric score in v1. `[ASSUMPTION]` |
| Loading / load failure | Question Bank | "Loading Questions..." then list; failure: inline error with Retry. |
| Empty list | Question Bank | "Your Question Bank is empty. Import a file or add a Question above." |
| Import running | Question Bank | Filename and "Reading file..." (slow on a local Provider). `[ASSUMPTION]` |
| Import result | Question Bank | Import result panel. Zero found: "No Questions found." plus the Q1. hint. |
| Add invalid / failed | Question Bank | Empty text: "Enter a Question first." Failure: inline error with Retry, input kept. `[ASSUMPTION]` |
| Delete failed | Question Bank | Dialog stays open with inline error and Retry. |
| Long list | Question Bank | Single scrolling list, count in the heading (for example 87 Questions). No search or paging in v1. `[ASSUMPTION]` |

## Interaction Primitives

- Tap or click to act. No gesture-only or hover-only controls.
- Keyboard: Tab follows reading order; Enter and Space activate buttons and the switch; Esc closes a dialog.
- Drag-and-drop is an enhancement over the Choose file button.
- `localStorage` holds the theme (System, Light, Dark) and the Show question type switch. Access is wrapped in try/catch; on failure defaults apply (System theme, labels hidden).
- Motion: opacity fades under 200ms only.
- **Banned:** numeric scores, confetti, auto-dismissing errors, stacked modals, infinite scroll.

## Accessibility Floor

Contrast values live in `DESIGN.md` Colors. `[ASSUMPTION]` for the whole floor (hobby stakes, unstated).

- WCAG AA contrast in both themes; semantic HTML (`header`, `nav`, `main`, headings, real buttons and controls).
- Full keyboard operation, including the Upload zone and the switch (`role="switch"`). Visible focus: 2px `{colors.focus-ring}` outline.
- Chat log is `role="log"` with `aria-live="polite"`, `aria-busy` while streaming so screen readers hear finished messages, not tokens. Thinking indicator is `role="status"`.
- Dialogs trap and restore focus. Color never carries meaning alone ("Strength", "Weak spot", "Could not read" are text).
- `prefers-reduced-motion` respected. Usable at 200% zoom with no horizontal scroll.

## Responsive & Platform

Mobile-first; one column at every width, at most `{spacing.column-max}`, centered.

| Breakpoint | Behavior |
|---|---|
| Base (below `{spacing.bp-md}`) | Gutters `{spacing.gutter-mobile}`. Header shows links and Theme toggle only. Delete drops below the Question text. Dialog buttons stack. Session strip may wrap. |
| `{spacing.bp-md}` and up | Gutters `{spacing.gutter-wide}`. Product name in header. Dialog buttons side by side. |
| `{spacing.bp-lg}` and up | Same layout, more top padding. No new columns. |

Touch targets at least `{spacing.touch-target}`; input font 16px or larger. On-screen keyboard: the composer stays above it (dynamic viewport height), the latest message scrolls into view on focus, header and strip stay sticky, composer sets `enterkeyhint="send"`.

## Key Flows

### UJ-1. Priya practices for a backend role (Priya, mid-level developer, interview on Friday)

1. Priya opens the site with no account. Interview shows a bot greeting, the Upload zone and the privacy line.
2. She uploads her Resume as a Word file; the zone shows the filename and "Reading...".
3. The Interview Session starts. The session strip reads "Question 1 of about 10". After the Thinking indicator, the first Question, about a project on her Resume, streams in.
4. She types and sends answers. Later Questions come from the Question Bank.
5. One answer is thin, so the bot asks a single follow-up that refers to what she said. The strip does not advance.
6. After the last planned Question the bot says the interview is over and the app opens the Coaching Report ("Preparing your Coaching Report...").
7. **Climax:** the Coaching Report appears: a summary, strengths and weak spots each quoting her own answers, and a per-Question review comparing her answer with the Reference Answer where one exists.
8. She follows "See the answer" on one weak spot, reads the review, then chooses Start new Interview Session or leaves. Her Resume is not kept.

Failure and variants:
- Unreadable Resume (scanned PDF): at step 2 the bot says it found no text and asks for a different file.
- Empty Question Bank: at step 3 the bot says so and runs Resume-only.
- Slow or failed reply: Stop and Retry, partial text kept.
- End early: at step 4 she chooses End interview, confirms, and gets a Coaching Report on the answers given.
- Report failure: inline error with Retry.

### UJ-2. Simon grows the Question Bank (Simon, builder, site running locally)

1. Simon opens Question Bank; the heading shows the count, the list follows.
2. In Import from file he chooses his Word file (Q1. to Q87.); the zone shows "Reading file...".
3. The Import result panel appears: "87 Questions added." Any unreadable part is listed under "Could not read".
4. He adds one Question by hand in Add a Question, with an optional Reference Answer. A toast says "Question added." and the row appears.
5. He goes to Interview and starts an Interview Session with a Resume.
6. **Climax:** the bot asks a Question from his uploaded file; with Show question type on, its label reads Question Bank.
7. Back in Question Bank, everything is still there.

Failure and variants:
- Unreadable parts: listed, never skipped silently.
- Zero found: "No Questions found." with the Q1. hint.
- Delete: Delete opens the Confirm dialog naming the Question; confirming removes it for good.

## Open Questions

1. Search or paging once the Question Bank grows past about 100? (PRD silent.)
2. Maximum file size for Resume and import, and the message when exceeded.
3. Does re-importing the same file skip or duplicate Questions?
4. Should a Coaching Report be saveable? (Session history is out of scope; it is lost on reload.)
5. Is the sticky header plus session strip too tall on a phone with the keyboard open?
