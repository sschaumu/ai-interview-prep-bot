---
title: "Product Brief: ai-interview-prep-bot"
status: final
created: 2026-09-21
updated: 2026-09-21
---

# Product Brief: ai-interview-prep-bot

## Executive summary

ai-interview-prep-bot is a website where you upload a resume and take a mock job interview with an AI chat bot. The bot asks questions about the resume and questions drawn from a large existing question bank, follows up on your answers, and at the end coaches you on how you did.

It is also a learning vehicle. The builder, Simon, is refreshing modern C#, learning React from scratch, and learning how AI applications work, with retrieval-augmented generation (RAG) at the center. The project is finished when the bot works end to end **and** Simon can explain the core concepts it used in his own words. Working software without understanding counts as a miss.

It is free to run by default, using local models on a 16 GB machine. The AI providers are swappable by configuration, so a paid provider can be adopted later without code changes.

## The problem

Mock interviews are hard to get. Generic question lists don't react to what you say and don't know your background, and human practice partners are scarce. Separately, learning React, RAG and AI application patterns from tutorials tends not to stick. It's easy to copy code you don't understand.

A project with a real, useful outcome forces the concepts to be used, and forces the gaps to show.

## The solution

A React web app backed by a C# API:

1. A user uploads a resume.
2. The bot runs a chat-based mock interview. It mixes resume-specific questions with questions retrieved from the question bank (technical and behavioral), and follows up on answers.
3. At the end, the bot gives coaching feedback: specific strengths and weak spots in the answers, not generic advice.

Retrieval over the question bank (RAG) picks relevant questions for the role, the resume and the conversation so far. The resume is short enough to sit in the prompt; its role is to steer which questions are retrieved and asked.

## What makes this different

This isn't a market play, and there's no moat. Its value comes from two things:

- **Tailored practice.** Questions come from a large personal bank and are shaped by the actual resume.
- **Learning by building.** Every major component (retrieval, structured answer evaluation, provider abstraction, the React UI) exists partly because it teaches something.

## Who this serves

- **Primary: Simon**, the builder and learner. Experienced in C# (refresher on new features), a beginner in React, new to RAG and AI application development. Commentary that explains concepts as they appear is a requirement, not a nice-to-have.
- **Secondary: anyone who uploads a resume** to practice interviewing. For v1 this is anonymous and per session, with no accounts.

## Learning goals

Each goal is met when Simon can explain it in his own words without looking anything up.

- **RAG:** how the question bank is chunked, embedded, stored and retrieved, and why.
- **React (from zero):** how a component, state and a call to the backend fit together.
- **Modern C#:** which recent language and framework features were used, and why.
- **AI application patterns:** provider abstraction, multi-turn conversation handling, structured output and evaluating free-form answers.

## Success criteria

**The bot works when:**
- A resume upload leads to a mock interview mixing resume questions with bank questions.
- The bot follows up on answers rather than reading a list.
- It ends with coaching feedback naming specific strengths and weak spots.
- All of this runs on the free local setup.
- Switching to a paid provider is a configuration change, not a code change.

**Simon has learned when** he can explain each learning goal above.

## Scope

**In for v1**
- Chat-based mock interview in the browser [ASSUMPTION: text only, no voice].
- Resume upload [ASSUMPTION: PDF and plain text], used for the current session only.
- RAG over the question bank [ASSUMPTION: ingested from a file-based export; format not yet known].
- End-of-interview coaching feedback.
- Local models by default; LLM and embedding providers swappable by configuration.
- A repeatable ingestion step, because changing the embedding model means re-embedding the whole bank.

**Out for v1**
- User accounts, saved resumes and interview history.
- Voice or video.
- Hosting. It is planned as a late learning step once the local version works, and the free constraint still applies.
- Paid services (available later through the swap, not used by default).

## Constraints and known risks

- **Free by default; 16 GB RAM.** Only small local models (roughly 3B to 8B parameters, quantized) are practical. GPU availability is unknown.
- **Local-model quality risk.** Small models are weaker at natural follow-ups and at judging free-form answers. The coaching feature is most exposed. Provider swappability is the mitigation.
- **Question bank unknowns** (format, size, whether answers or tags exist). Without reference answers, coaching rests on the LLM's judgment alone.
- **Scope vs. learning load.** Learning React from zero while building the retrieval and evaluation logic is a lot. It is self-paced with no deadline, so the answer to pressure is to cut features, not learning.

## Vision

If it works, it becomes a place to practice for real interviews: saved history, progress over time and more question banks. It could also be hosted for others. None of that is a v1 goal, and the learning outcome comes first.

## Open questions

- What format and size is the question bank, and does it include reference answers or tags?
- Is a free hosted API tier acceptable as a fallback?
- Should the interview have a fixed length? [ASSUMPTION: about 10 questions]
