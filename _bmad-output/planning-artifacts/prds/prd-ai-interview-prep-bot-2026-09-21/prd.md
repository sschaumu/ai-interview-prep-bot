---
title: "PRD: ai-interview-prep-bot"
status: final
created: 2026-09-21
updated: 2026-09-21
---

# PRD: ai-interview-prep-bot
*Working title — confirm.*

## 0. Document purpose

This PRD defines v1 of ai-interview-prep-bot for Simon (builder and product owner) and for the downstream architecture, epic and build steps. Terms in the Glossary are used exactly as written. Features group the functional requirements (FRs), and every guess is tagged `[ASSUMPTION]` inline and indexed in §9. It builds on the product brief at `_bmad-output/planning-artifacts/briefs/brief-ai-interview-prep-bot-2026-09-21/brief.md`, and technical options live in that folder's `addendum.md`.

## 1. Vision

ai-interview-prep-bot is a website where someone uploads a Resume and takes a mock job interview with an AI chat bot. The bot asks questions about the Resume and questions drawn from a large Question Bank, follows up on the answers, and at the end coaches the person on how they did.

It is also a learning vehicle. Simon is refreshing modern C#, learning React from scratch and learning how AI applications work, with retrieval-augmented generation (RAG) at the center. v1 succeeds when the bot works end to end and Simon can explain the core concepts it used in his own words. The bot is free to run by default on local models, and its AI providers are swappable by configuration. Building it also carries a requirement: each new React, C# or AI concept is explained as it appears. This applies to the build process, not to the product.

## 2. Target user

### 2.1 Jobs to be done
- **Simon (builder):** learn React, modern C# and RAG by building something real that works.
- **Interviewee:** practice a realistic interview tailored to their background and get specific feedback on their answers.

### 2.2 Non-users (v1)
- Recruiters or interviewers who want to assess candidates.

### 2.3 Key user journeys

- **UJ-1. Priya practices for a backend role.**
  - **Persona + context:** Priya, a mid-level developer with an interview on Friday, wants a realistic rehearsal that asks about her actual experience. `[ASSUMPTION: persona name and role are illustrative]`
  - **Entry state:** No account; the Question Bank already has questions loaded.
  - **Path:** She opens the site and uploads her Resume as a Word file. The bot starts an Interview Session, and the first question is about a project on her Resume. Later questions come from the Question Bank. She types her answers, the bot asks a follow-up when an answer is thin, and the session ends after the last question.
  - **Climax:** The bot shows a Coaching Report naming what she did well, where her answers were weak, and how they compare with the Reference Answer where one exists.
  - **Resolution:** She reads the report and starts a new session or leaves. Her Resume is not kept.
  - **Edge case:** If the Resume can't be read (a scanned PDF with no text, for example), the bot says so and asks for a different file.

- **UJ-2. Simon grows the Question Bank.**
  - **Persona + context:** Simon has a large set of questions and answers in Word, PDF and text files, and wants them in the bot.
  - **Entry state:** The site is running locally.
  - **Path:** He uploads a file, and the app reports how many Questions it found. He then adds one Question by hand.
  - **Climax:** In the next Interview Session the bot asks a Question from the file he just uploaded.
  - **Resolution:** The Question Bank keeps everything he added.
  - **Edge case:** If the import can't read part of a file, the report lists what was skipped, so nothing disappears silently.

## 3. Glossary

- **Question Bank** — The saved collection of Questions the bot draws from. One shared bank for the whole site in v1. `[ASSUMPTION]`
- **Question** — One interview question, with an optional Reference Answer.
- **Reference Answer** — A known-good answer to a Question, used in coaching.
- **Interviewee** — A person who uploads a Resume and takes an Interview Session.
- **Resume** — A Word or PDF document uploaded by an Interviewee. Used for one Interview Session only.
- **Interview Session** — One chat from Resume upload to Coaching Report.
- **Coaching Report** — The feedback given at the end of an Interview Session.
- **Provider** — The service or local model behind the bot's chat or embeddings, chosen by configuration.

## 4. Features

### 4.1 Question Bank management
**Description:** Simon builds the Question Bank by bulk import or by adding one Question at a time. Realizes UJ-2. The Question Bank persists across sessions. `[ASSUMPTION: no accounts, so anyone using the site can view and change the Question Bank in v1]`

#### FR-1: Import Questions from a file
A user can upload a Word, PDF or plain text file containing many Questions, with or without Reference Answers. Realizes UJ-2.

**Consequences (testable):**
- After import, the app reports the number of Questions added and lists anything it couldn't read.
- Imported Questions are available to the next Interview Session.
- Reference Answers found in the file are stored with their Question.
- A file laid out like the sample bank imports every Question with its Reference Answer: each Question starts with "Q" plus a number and a period (Q1., Q2., …), and its answer is everything up to the next Question. The sample file yields 87 Questions.
- Text before the first Question, such as a title or a list of links, is ignored.
- `[ASSUMPTION: PDF and plain text files use the same "Q<number>." pattern; other layouts are best-effort]`

#### FR-2: Add a single Question
A user can enter one Question, with an optional Reference Answer, in the app. Realizes UJ-2.

**Consequences (testable):**
- The new Question is available to the next Interview Session.

#### FR-3: View and delete Questions
A user can list the Questions in the Question Bank and delete any of them. `[ASSUMPTION: edit is deferred; delete and re-add instead]`

**Consequences (testable):**
- The list shows every Question in the Question Bank.
- A deleted Question no longer appears in the list and is never asked in a later Interview Session.

### 4.2 Resume upload
**Description:** An Interviewee provides a Resume that shapes the Interview Session. Realizes UJ-1.

#### FR-4: Upload a Resume
An Interviewee can upload a Resume as a Word or PDF file to start an Interview Session. Realizes UJ-1.

**Consequences (testable):**
- The text is extracted and used in that Interview Session only.
- If no text can be extracted, the user is told and asked for another file.
- The Resume is not stored after the Interview Session ends.

### 4.3 Interview Session
**Description:** The bot conducts a chat interview, mixing Resume-based Questions with Questions from the Question Bank. Realizes UJ-1.

#### FR-5: Ask a mix of Resume and Question Bank questions
The bot asks Questions drawn from the Resume and Questions retrieved from the Question Bank, chosen for relevance to the Resume and the conversation so far. Realizes UJ-1.

**Consequences (testable):**
- A session contains at least one Resume-based question and at least one Question Bank Question. `[ASSUMPTION: an Interview Session is about 10 questions]`
- Behavioral questions (for example "Tell me about a time…") are generated from something specific on the Resume. `[ASSUMPTION: behavioral questions come from the Resume in v1, not from the Question Bank]`
- If the Question Bank is empty, the bot still runs using Resume-based questions and tells the user.

#### FR-6: Follow up on answers
When an answer is thin or interesting, the bot can ask a follow-up before moving on. Realizes UJ-1.

**Consequences (testable):**
- A follow-up refers to something the user actually said in that answer.
- The bot asks at most one follow-up per Question. `[ASSUMPTION]`

#### FR-7: End the session
The session ends after the planned number of questions, or when the user ends it early. Realizes UJ-1.

**Consequences (testable):**
- When the session ends, the user is taken to the Coaching Report.

### 4.4 Coaching
**Description:** After the session, the bot gives specific, honest feedback. Realizes UJ-1.

#### FR-8: Produce a Coaching Report
At the end of an Interview Session the bot produces a Coaching Report. Realizes UJ-1.

**Consequences (testable):**
- The report names at least one strength and one weak spot, each referring to one of the user's actual answers.
- Where a Question has a Reference Answer, the report compares the user's answer to it.
- Where there is no Reference Answer (Resume-based questions, or a bank Question added without one), the report judges the answer against a simple rubric. For behavioral questions the rubric is situation, action and result. `[ASSUMPTION]`
- If a session ends early, the report covers the answers given.

### 4.5 Swappable providers
**Description:** The bot works with local models by default, and switching to a paid Provider is a configuration change.

#### FR-9: Choose Providers by configuration
The chat Provider and the embedding Provider can each be changed through configuration, with no code changes.

**Consequences (testable):**
- The default configuration uses local models and needs no paid account or API key.
- Changing the configuration to a paid Provider runs the same Interview Session without code changes.

#### FR-10: Rebuild embeddings on change
When the embedding Provider changes, the Question Bank can be re-embedded so retrieval keeps working.

**Consequences (testable):**
- After a re-embed, retrieval returns Questions from the whole Question Bank, including those imported before the change.

**Known risk:** small local models are weaker at natural follow-ups and at judging answers, so coaching quality is most exposed. Swappable Providers are the mitigation.

**Cross-cutting NFRs**
- Runs on a 16 GB machine using small local models.
- API keys and other secrets never reach the browser.
- The Resume is deleted when the Interview Session ends.
- Chat responses appear as they are generated, so the user is not left on a blank screen. `[ASSUMPTION]`

## 5. Non-goals
- Not a recruiting or candidate-assessment tool.
- Not a general-purpose chat bot or a career-advice site.
- No paid services in the default setup.
- Not a production-grade product. Learning value comes before polish and scale.

## 6. MVP scope

### 6.1 In scope
- Question Bank import (Word, PDF, text) and single-Question entry, plus list and delete.
- Resume upload (Word, PDF).
- A chat interview mixing Resume and Question Bank questions, with follow-ups.
- A Coaching Report.
- Local-by-default, swappable Providers.

### 6.2 Out of scope for MVP
- User accounts, saved Resumes and interview history.
- Editing a Question in place. `[ASSUMPTION]`
- Tagging Questions by type or topic. Retrieval relies on the Question text alone. `[ASSUMPTION]`
- Voice or video. `[ASSUMPTION]`
- Hosting. Planned as a late learning step after the local version works.

## 7. Success metrics

**Primary**
- **SM-1:** From a Resume upload, Simon can complete a full Interview Session and receive a Coaching Report on the free local setup. Validates FR-4 to FR-9.
- **SM-2:** Simon can explain, in his own words, how the Question Bank is chunked, embedded and retrieved; how a React component, state and a backend call fit together; and which modern C# features he used and why. This is the learning goal from the brief.

**Secondary**
- **SM-3:** A Provider switch, local to paid, works by configuration alone. Validates FR-9.

**Counter-metrics (do not optimize)**
- **SM-C1:** Number of features shipped. Counterbalances SM-1. Cutting a feature to understand the rest is the right trade.

## 8. Open questions
1. Is a free hosted API tier acceptable as a fallback for local models?
2. Is a shared, editable Question Bank acceptable once the site is hosted, given there are no accounts?

## 9. Assumptions index
- §2.3 UJ-1 — persona name and role are illustrative.
- §3 Glossary — one shared Question Bank for the whole site.
- FR-6 — at most one follow-up per Question.
- §4.1 — anyone using the site can view and change the Question Bank in v1.
- FR-1 — PDF and plain text files use the same "Q<number>." pattern as the sample Word file.
- FR-3 / §6.2 — editing a Question is deferred; delete and re-add instead.
- FR-5 — an Interview Session is about 10 questions.
- FR-5 — behavioral questions come from the Resume in v1.
- FR-8 — questions without a Reference Answer are judged against a simple rubric (situation, action, result for behavioral).
- §6.2 — Question tagging is out of v1.
- §4.5 NFRs — responses stream to the user.
- §6.2 — voice and video are out.
