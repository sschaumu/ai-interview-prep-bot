---
name: ai-interview-prep-bot
title: ai-interview-prep-bot Design System
description: Calm, low-saturation, text-first look for a Resume-based AI mock interview web app. Hand-rolled plain CSS, light and dark themes, system fonts.
status: final
created: 2026-09-21
updated: 2026-09-21
sources:
  - ../../prds/prd-ai-interview-prep-bot-2026-09-21/prd.md
  - ../../briefs/brief-ai-interview-prep-bot-2026-09-21/brief.md
colors:
  # Light set. Each key maps to a CSS custom property: colors.surface-base -> --color-surface-base.
  # The *-dark keys are the values the same property takes in the dark theme.
  surface-base: '#F7F7F5'
  surface-raised: '#FFFFFF'
  surface-sunken: '#EEEEEA'
  ink-primary: '#1F2328'
  ink-secondary: '#4F5660'
  ink-disabled: '#8B9199'
  border-hairline: '#DAD9D3'
  border-strong: '#7F868F'
  accent: '#2B6A66'
  on-accent: '#FFFFFF'
  accent-subtle: '#E1EEEC'
  accent-ink: '#1F4D4A'
  success: '#2C6444'
  success-subtle: '#E4F0E8'
  caution: '#7A4F0C'
  caution-subtle: '#F6ECD9'
  danger: '#A03631'
  on-danger: '#FFFFFF'
  danger-subtle: '#F7E4E2'
  focus-ring: '#2B6A66'
  scrim: '#1F232866'
  # Dark set. Same names, redefined values.
  surface-base-dark: '#141719'
  surface-raised-dark: '#1C2024'
  surface-sunken-dark: '#101315'
  ink-primary-dark: '#E7E9EB'
  ink-secondary-dark: '#A2A9B1'
  ink-disabled-dark: '#5B626A'
  border-hairline-dark: '#2C3237'
  border-strong-dark: '#737B84'
  accent-dark: '#86C5BE'
  on-accent-dark: '#0E1A19'
  accent-subtle-dark: '#1F3331'
  accent-ink-dark: '#B5E0DB'
  success-dark: '#8FCFA8'
  success-subtle-dark: '#1B2C22'
  caution-dark: '#E2B872'
  caution-subtle-dark: '#2E2513'
  danger-dark: '#F0908A'
  on-danger-dark: '#2A0F0E'
  danger-subtle-dark: '#3A1F1E'
  focus-ring-dark: '#86C5BE'
  scrim-dark: '#000000A6'
typography:
  # typography.body.fontSize -> --type-body-font-size. Rem so browser zoom works.
  display:
    fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif'
    fontSize: 1.75rem
    fontWeight: '600'
    lineHeight: '1.25'
  title:
    fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif'
    fontSize: 1.25rem
    fontWeight: '600'
    lineHeight: '1.35'
  body:
    fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif'
    fontSize: 1rem
    fontWeight: '400'
    lineHeight: '1.6'
  body-sm:
    fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif'
    fontSize: 0.875rem
    fontWeight: '400'
    lineHeight: '1.5'
  label:
    fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif'
    fontSize: 0.875rem
    fontWeight: '500'
    lineHeight: '1.4'
  meta:
    fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif'
    fontSize: 0.8125rem
    fontWeight: '400'
    lineHeight: '1.4'
rounded:
  sm: 4px
  md: 8px
  lg: 12px
  full: 9999px
spacing:
  # spacing.4 -> --space-4. Named tokens map the same way: spacing.column-max -> --space-column-max.
  '1': 4px
  '2': 8px
  '3': 12px
  '4': 16px
  '5': 24px
  '6': 32px
  '7': 48px
  '8': 64px
  gutter-mobile: 16px
  gutter-wide: 24px
  column-max: 44rem
  header-height: 3.5rem
  strip-height: 3rem
  touch-target: 2.75rem
  # Reference only: media queries cannot read var(), so write these literally.
  bp-md: 40rem
  bp-lg: 64rem
components:
  header:
    background: '{colors.surface-raised}'
    foreground: '{colors.ink-primary}'
    border: '{colors.border-hairline}'
    height: '{spacing.header-height}'
    padding: '{spacing.4}'
  nav-link:
    foreground: '{colors.ink-secondary}'
    minHeight: '{spacing.touch-target}'
    typography: '{typography.label}'
  nav-link-active:
    foreground: '{colors.accent-ink}'
    border: '{colors.accent}'
  theme-toggle:
    foreground: '{colors.ink-secondary}'
    minSize: '{spacing.touch-target}'
    radius: '{rounded.md}'
  button-primary:
    background: '{colors.accent}'
    foreground: '{colors.on-accent}'
    radius: '{rounded.md}'
    minHeight: '{spacing.touch-target}'
    padding: '{spacing.2} {spacing.5}'
    typography: '{typography.label}'
  button-secondary:
    background: '{colors.surface-raised}'
    foreground: '{colors.ink-primary}'
    border: '{colors.border-strong}'
    radius: '{rounded.md}'
    minHeight: '{spacing.touch-target}'
  button-destructive:
    background: '{colors.danger}'
    foreground: '{colors.on-danger}'
    radius: '{rounded.md}'
    minHeight: '{spacing.touch-target}'
  upload-zone:
    background: '{colors.surface-sunken}'
    border: '{colors.border-strong}'
    radius: '{rounded.lg}'
    padding: '{spacing.6}'
  upload-zone-active:
    background: '{colors.accent-subtle}'
    border: '{colors.accent}'
  chat-bubble-bot:
    background: '{colors.surface-raised}'
    foreground: '{colors.ink-primary}'
    border: '{colors.border-hairline}'
    radius: '{rounded.lg}'
    padding: '{spacing.3} {spacing.4}'
  chat-bubble-interviewee:
    background: '{colors.accent-subtle}'
    foreground: '{colors.ink-primary}'
    radius: '{rounded.lg}'
    padding: '{spacing.3} {spacing.4}'
  composer:
    background: '{colors.surface-raised}'
    border: '{colors.border-hairline}'
    padding: '{spacing.3}'
  composer-input:
    background: '{colors.surface-base}'
    foreground: '{colors.ink-primary}'
    border: '{colors.border-strong}'
    radius: '{rounded.md}'
    typography: '{typography.body}'
  thinking-indicator:
    foreground: '{colors.ink-secondary}'
    typography: '{typography.body-sm}'
  session-strip:
    background: '{colors.surface-base}'
    border: '{colors.border-hairline}'
    height: '{spacing.strip-height}'
    typography: '{typography.label}'
  question-type-label:
    background: '{colors.surface-sunken}'
    foreground: '{colors.ink-secondary}'
    radius: '{rounded.sm}'
    typography: '{typography.meta}'
  report-card-strength:
    background: '{colors.success-subtle}'
    foreground: '{colors.ink-primary}'
    border: '{colors.success}'
    radius: '{rounded.md}'
  report-card-weak-spot:
    background: '{colors.caution-subtle}'
    foreground: '{colors.ink-primary}'
    border: '{colors.caution}'
    radius: '{rounded.md}'
  report-card-review:
    background: '{colors.surface-raised}'
    foreground: '{colors.ink-primary}'
    border: '{colors.border-hairline}'
    radius: '{rounded.md}'
  question-row:
    background: '{colors.surface-raised}'
    foreground: '{colors.ink-primary}'
    border: '{colors.border-hairline}'
    minHeight: '{spacing.touch-target}'
  form-field:
    background: '{colors.surface-base}'
    border: '{colors.border-strong}'
    radius: '{rounded.md}'
    typography: '{typography.body}'
  confirm-dialog:
    background: '{colors.surface-raised}'
    foreground: '{colors.ink-primary}'
    radius: '{rounded.lg}'
    padding: '{spacing.5}'
  import-result-panel:
    background: '{colors.surface-raised}'
    border: '{colors.border-hairline}'
    radius: '{rounded.md}'
    padding: '{spacing.4}'
  toast-inline-error:
    background: '{colors.danger-subtle}'
    foreground: '{colors.danger}'
    radius: '{rounded.md}'
    padding: '{spacing.3} {spacing.4}'
  empty-state:
    foreground: '{colors.ink-secondary}'
    padding: '{spacing.7} {spacing.4}'
---

# ai-interview-prep-bot Design System

## Brand & Style

Calm is the whole brief. Someone about to be interviewed is already nervous, so the product should feel like a quiet desk, not a game: generous whitespace, one muted accent, text first, almost no motion. Feedback from the Coaching Report is honest but never alarming. Weak spots are shown in amber and plain words, never red. No streaks, scores, confetti, or gradients.

Everything is hand-rolled plain CSS (no component library). Every value below is a token that becomes one CSS custom property, so a component rule reads `background: var(--color-surface-raised)` and never a raw hex.

Palette, type and shape are neutral defaults chosen without user input. `[ASSUMPTION]` The user can swap any value later; contrast targets below must still hold.

## Colors

Two token sets with identical names. Light is the base set; dark redefines the same properties (keys with the `-dark` suffix). Which set is active is behavior, described in EXPERIENCE.md. `[ASSUMPTION]` Hues are a muted teal accent over cool neutral greys.

- **Surfaces.** `{colors.surface-base}` is the page. `{colors.surface-raised}` is anything that sits on it (header, bubbles, cards, dialog, composer). `{colors.surface-sunken}` is for recessed things (upload zone, question-type label). Layers differ by tone, not shadow.
- **Ink.** `{colors.ink-primary}` for text; `{colors.ink-secondary}` for meta and supporting text; `{colors.ink-disabled}` for disabled controls only (exempt from contrast, never for readable content).
- **Borders.** `{colors.border-hairline}` separates rows and cards and is decorative. Any border that identifies a control (inputs, secondary buttons, upload zone) uses `{colors.border-strong}` to meet the 3:1 non-text contrast rule.
- **Accent.** `{colors.accent}` with `{colors.on-accent}` is the single primary action per view. `{colors.accent-subtle}` is the Interviewee bubble and drag-over tint; `{colors.accent-ink}` is the active nav link and inline links. Never decorative.
- **Meaning colors.** `{colors.success}` / `{colors.success-subtle}` mark strengths and import success. `{colors.caution}` / `{colors.caution-subtle}` mark weak spots. `{colors.danger}`, `{colors.on-danger}`, `{colors.danger-subtle}` are for delete and errors only. Meaning is always paired with a text label, never color alone.
- **Focus and scrim.** `{colors.focus-ring}` is the 2px focus outline (3:1 or better against every surface). `{colors.scrim}` dims the page behind a dialog.

Contrast targets (WCAG 2.2 AA, verified for both themes): body text on any surface at least 4.5:1 (lowest is `ink-secondary` on `surface-sunken`, 6.4:1 light); `on-accent` on `accent` 6.3:1 light / 9.1:1 dark; meaning ink on its subtle fill at least 5.5:1; `border-strong` and `focus-ring` at least 3:1 against `surface-raised` and `surface-base`.

## Typography

System font stack, no web fonts: instant load, native feel on every device, zero licensing. `[ASSUMPTION]` One family, weight and size carry hierarchy.

| Role | Use |
|---|---|
| `{typography.display}` | View title (h1) on Coaching Report and Question Bank only |
| `{typography.title}` | Section headings (h2): Summary, Strengths, Weak spots, Question review |
| `{typography.body}` | Chat text, Question text, Reference Answer, form input (16px minimum stops iOS zoom-on-focus) |
| `{typography.body-sm}` | Thinking indicator, hints, timestamps |
| `{typography.label}` | Buttons, nav links, session strip, form labels |
| `{typography.meta}` | Question-type label, counts |

Sentence case everywhere; no all-caps, no italics for emphasis. Long Reference Answers and report text keep `{typography.body}` with paragraph spacing `{spacing.3}`.

## Layout & Spacing

Scale is 4px based: `{spacing.1}` 4, `{spacing.2}` 8, `{spacing.3}` 12, `{spacing.4}` 16, `{spacing.5}` 24, `{spacing.6}` 32, `{spacing.7}` 48, `{spacing.8}` 64. Tight pairs (label and field) use 1-2; related blocks use 3-4; sections use 5-7.

One centered column, `{spacing.column-max}` wide (about 70 characters of body text), on every view. Side gutters are `{spacing.gutter-mobile}`, growing to `{spacing.gutter-wide}` from `{spacing.bp-md}`. No sidebar, no multi-column layout at any width. Sticky chrome: header (`{spacing.header-height}`), then on the Interview Session the session strip (`{spacing.strip-height}`); the composer sticks to the bottom.

**CSS notes** (for learning; keep brief)
- **Custom properties.** Declare every token on `:root` once; components only use `var(--...)`. Change a token, the whole app changes. Naming: `--color-*`, `--type-<role>-<property>`, `--radius-*`, `--space-*`.
- **Mobile-first.** Base rules are the phone layout. Add `@media (min-width: 40rem)` (`bp-md`) for wider gutters and `@media (min-width: 64rem)` (`bp-lg`) for extra top padding. Use `min-width` only.
- **Column.** `.page { max-width: var(--space-column-max); margin-inline: auto; padding-inline: var(--space-gutter-mobile); }`
- **Dark theme.** Light values live on `:root`. Redefine the color properties in `@media (prefers-color-scheme: dark) { :root:not([data-theme="light"]) { ... } }` and again in `:root[data-theme="dark"] { ... }`, so a saved manual choice (a `data-theme` attribute set by the toggle) beats the system. The duplicated block is the price of plain CSS.
- **Height.** Use `100dvh` (not `100vh`) for the chat view so the phone keyboard does not hide the composer.
- **Motion.** Wrap animations in `@media (prefers-reduced-motion: no-preference)`.

## Elevation & Depth

Flat. Hierarchy comes from tone (`surface-raised` on `surface-base`) and hairline borders, not shadows. Sticky header and session strip get a bottom hairline; the composer gets a top hairline. The confirm dialog is the only shadowed element: `0 8px 24px` at 12% black (light) or 50% black (dark), over `{colors.scrim}`. Sticky layers use z-index steps: header 10, strip 9, composer 8, dialog and scrim 100.

## Shapes

Soft but not playful. `{rounded.sm}` for tags and the question-type label; `{rounded.md}` for inputs, buttons, cards, panels; `{rounded.lg}` for chat bubbles, the upload zone, and the dialog. Chat bubbles round all corners except the one nearest the speaker, which uses `{rounded.sm}`. `{rounded.full}` only for the switch track and the thinking dots.

## Components

Visual specs; behavior lives in EXPERIENCE.md Component Patterns. Component tokens in the frontmatter resolve to the active theme automatically because they point at custom properties.

| Component | Visual spec |
|---|---|
| Header / nav | `{colors.surface-raised}` bar, bottom hairline, height `{spacing.header-height}`. From `{spacing.bp-md}` up, left: product name in `{typography.title}`; below that width the name is hidden so the row holds only the two nav links (Interview, Question Bank) and the Theme toggle at the right. `[ASSUMPTION]` Active link `{colors.accent-ink}` with 2px `{colors.accent}` underline; inactive `{colors.ink-secondary}`. Every target at least `{spacing.touch-target}`. |
| Theme toggle | Icon-only button with a visible text label on hover/focus and an accessible name; icon shows the current mode (system, light, dark). `{colors.ink-secondary}`, `{rounded.md}`. |
| Button (primary / secondary / destructive) | Height at least `{spacing.touch-target}`, `{typography.label}`, `{rounded.md}`. Primary: `{colors.accent}` fill. Secondary: `{colors.surface-raised}` fill, 1px `{colors.border-strong}`. Destructive: `{colors.danger}` fill, used only inside the Confirm dialog. Hover: 8% darker (light) or lighter (dark); disabled: `{colors.ink-disabled}` text, no fill change. Focus: 2px `{colors.focus-ring}` outline, 2px offset. |
| Upload zone | `{colors.surface-sunken}` block, 2px dashed `{colors.border-strong}`, `{rounded.lg}`, `{spacing.6}` padding. Centered prompt, then a secondary "Choose file" button, then accepted-type hint in `{typography.meta}`. Drag-over: `{colors.accent-subtle}` fill, `{colors.accent}` border. Uploading: prompt replaced by filename and "Reading..." text. |
| Chat bubble (bot / Interviewee) | Max 85% of column width. Bot: left-aligned, `{colors.surface-raised}` with hairline. Interviewee: right-aligned, `{colors.accent-subtle}`, no border. `{typography.body}`, `{spacing.3}`/`{spacing.4}` padding, `{spacing.3}` gap between bubbles. Streaming text grows in place; a plain caret is not needed. |
| Composer | Bottom-fixed `{colors.surface-raised}` bar, top hairline. Auto-growing textarea (`composer-input`, 1 to 5 lines) with a Send primary button beside it. While streaming, Send becomes a secondary Stop button. |
| Thinking indicator | Bot-side row: three `{rounded.full}` dots in `{colors.ink-secondary}` pulsing opacity in sequence, followed by "Thinking..." in `{typography.body-sm}`. Reduced motion: static dots, text only. |
| Session strip | Sticky bar under the header. Left: "Question 3 of about 10" in `{typography.label}`. Right: the Show question type switch (36x20px track, `{rounded.full}`, on = `{colors.accent}`, with a visible text label) and a secondary End interview button. Wraps to two rows when it does not fit. |
| Question-type label | Small tag above a bot Question bubble: `{typography.meta}`, `{colors.ink-secondary}` on `{colors.surface-sunken}`, `{rounded.sm}`, text only (Resume or Question Bank). No icon, no color coding. |
| Coaching Report card | Three variants, `{rounded.md}`, `{spacing.4}` padding, 4px left border. Strength: `{colors.success-subtle}` fill, border `{colors.success}`, "Strength" label in `{colors.success}`. Weak spot: `{colors.caution-subtle}`, `{colors.caution}`, "Weak spot" label. Question review: `{colors.surface-raised}` with hairline; shows Question, the answer given, then Reference Answer or feedback. Each strength / weak spot ends with a quoted answer excerpt and a "See the answer" text link. |
| Question Bank row | Full-width `{colors.surface-raised}` row, bottom hairline, minimum `{spacing.touch-target}` tall. Question text in `{typography.body}` clamped to 2 lines, with a "Reference Answer" disclosure below when one exists, and a secondary Delete button at the right (below the text on narrow widths). |
| Form field | Label above (`{typography.label}`), textarea or input (`{colors.surface-base}`, 1px `{colors.border-strong}`, `{rounded.md}`), hint below in `{typography.body-sm}`. Error: border and message in `{colors.danger}`, message text below the field. |
| Confirm dialog | Centered modal, max 24rem wide, `{colors.surface-raised}`, `{rounded.lg}`, `{spacing.5}` padding, over `{colors.scrim}`. Title, one plain sentence (names the Question when deleting), then Cancel (secondary) and the destructive or primary action, right-aligned. Full-width stacked buttons below `{spacing.bp-md}`. |
| Import result panel | Card under the upload zone, hairline border. Heading with the count in `{colors.success}` ("42 Questions added"). If parts were skipped: a second heading "Could not read" and a plain list of the skipped items (line or Question number and reason). Dismiss link at the bottom. |
| Toast / inline error | Inline: `{colors.danger-subtle}` block with `{colors.danger}` text, placed where the failure happened, with a Retry secondary button when retry applies. Toast: same block fixed above the composer / bottom of viewport, auto-dismiss only for confirmations, never for errors. |
| Empty state | Centered `{colors.ink-secondary}` text in `{typography.body}`, one sentence plus at most one action. No illustration. |

## Do's and Don'ts

| Do | Don't |
|---|---|
| One primary (`{colors.accent}`) button per view | Two competing filled buttons; accent for decoration |
| Pair every meaning color with a text label ("Strength", "Weak spot", "Could not read") | Convey state by color alone |
| Reference tokens (`var(--color-...)`); redefine them for dark | Hard-code hex or a second dark stylesheet |
| Keep the single centered column at every width | Add a sidebar or split panes |
| Amber and plain words for weak spots | Red, warning icons, or numeric scores in the report |
| Keep motion to opacity fades under 200ms and honor reduced motion | Bouncing, sliding, confetti, animated gradients |
| Use `{colors.border-strong}` on anything interactive | Use `border-hairline` to outline a control |
| Keep inputs at 16px and targets at 44px on phones | Shrink controls to fit more on screen |
