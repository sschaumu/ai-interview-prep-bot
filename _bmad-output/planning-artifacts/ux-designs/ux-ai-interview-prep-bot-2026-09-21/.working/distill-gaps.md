# Distill gaps: ai-interview-prep-bot spines (2026-09-21)

## (a) [ASSUMPTION]s introduced beyond the memlog

Visual (DESIGN.md)
1. Concrete palette: muted teal accent over cool neutral greys, plus success/caution/danger sets, all hex values (memlog only said "neutral, low saturation, tag ASSUMPTION"). Contrast verified by script: text at least 6.4:1, on-accent 6.3:1 light / 9.1:1 dark, meaning ink on subtle fill at least 5.6:1, border-strong and focus-ring at least 3.4:1.
2. System font stack, one family; type ramp sizes (1.75 / 1.25 / 1 / 0.875 / 0.8125 rem); radii 4/8/12; 4px spacing scale; column max 44rem; breakpoints 40rem and 64rem.
3. Elevation: flat, only the dialog has a shadow; z-index steps; motion limited to opacity under 200ms.
4. Header hides the product name below 40rem so links plus Theme toggle fit one row.
5. Weak spots in amber (caution), not red; "Strength" / "Weak spot" text labels.

Behavior (EXPERIENCE.md)
6. Route paths `/`, `/interview`, `/interview/report`, `/question-bank`; `/` redirects.
7. Interview Session and Coaching Report held in memory only: nav away and back resumes, reload ends; report route with no report redirects.
8. Theme toggle is a 3-state cycle System / Light / Dark (memlog said a toggle that overrides and persists; no way back to System was specified).
9. Composer keys: desktop Enter sends, touch Enter adds a line.
10. End interview disabled until one answer is sent; End interview uses a Confirm dialog.
11. "Question N of about 10" counts Questions, not follow-ups.
12. Question-type label applies to earlier bubbles retroactively; follow-ups have no label.
13. Question Bank row: Question text clamped to 2 lines, Reference Answer in an expandable disclosure.
14. Coaching Report is rendered when complete (not streamed); generation failure shows inline error with Retry and keeps answers; "See the answer" link targets the Question's review card; early-end note text.
15. Stopped/failed reply keeps partial text, composer stays off until Retry succeeds; "Jump to latest" button when scrolled up.
16. Bot greeting bubble on `/interview`; unreadable Resume message rendered as a bot bubble; wrong-file-type inline error.
17. Import: "Reading file..." running state, "No Questions found." plus Q1. format hint on zero results; Add Question empty-text validation; delete failure keeps dialog open with Retry.
18. Question Bank long list: single scroll, count in heading, no search or paging.
19. Toast for confirmations auto-dismisses at 5 seconds; errors never auto-dismiss.
20. Word "Provider" used in user-facing failure copy.

## (b) Pass 1 coverage gaps not closed

- Flow coverage: strong. UJ-1 and UJ-2 have named protagonists, numbered steps, a climax and failure paths.
- Token completeness: all 53 `{...}` references resolve; every color has a hex and a `-dark` pair. Gaps: the DESIGN.md spec has no frontmatter key for shadows, motion, z-index or hover/disabled states, so those live in prose only (values stated, not tokens). Breakpoints are stored as `spacing.bp-*` reference-only tokens because media queries cannot read `var()`. Component tokens use `minHeight`, `minSize`, `border`, `typography`, which go beyond the spec's shown examples.
- Component coverage: 16 component names identical across DESIGN.md Components and EXPERIENCE.md Component Patterns. Nothing open.
- State coverage: offline is not a separate state (folded into Provider failure). Reference Answer very long text (small-model context) is not a UI state. No "session in progress, user reloads" warning (open decision, not listed).
- Visual references: no mockups, wireframes or imports exist; nothing to link.
- Shape fit: Inspiration & Anti-patterns dropped (no reference products or rejects in sources or memlog). Responsive & Platform included. DESIGN.md body is about 1,570 words (target 1,000-1,500); EXPERIENCE.md about 2,130 (target 1,500-2,200).
- Glossary check: passes. "Interviewee" appears only in the component name and bubble label, since UI copy addresses "you".

## (c) Conflicts between memlog and PRD/brief extract

1. Question-type taxonomy (memlog: Resume, Question Bank, Behavioral) vs PRD assumption that behavioral Questions are generated from the Resume in v1. A behavioral Question about her Resume could carry either label. Needs a rule.
2. Memlog progress strip "Question N of ~10" vs PRD: progress indicator not stated, and ~10 is itself an `[ASSUMPTION]`; how follow-ups and early end interact is unspecified (resolved here as item 11).
3. FR-8 requires at least one strength and one weak spot tied to an actual answer, but FR-7 allows ending early with no answers given. Resolved here by disabling End interview until one answer is sent (item 10); the PRD does not say this.
4. Memlog says the Coaching Report "links back to the answer"; the report has no chat transcript, so the link target is the per-Question review card (item 14).
5. Brief (Resume: PDF and plain text) vs PRD (Word or PDF). Spines follow the PRD; memlog is silent. Plain text is accepted only for Question import.
6. Brief uses lowercase "resume", "question bank", "coaching feedback"; spines use PRD glossary casing.
7. Memlog has no UI for FR-9 (Provider choice) or FR-10 (re-embed); the spines state "no UI in v1", matching PRD wording, but Simon may want a trigger for re-embed.
