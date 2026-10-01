# Addendum: ai-interview-prep-bot

Detail that belongs in the PRD or architecture, not the brief.

## Setup requirements for the AI side (options, not decisions)

- **LLM provider:** local via Ollama (free default). Paid options later: Azure OpenAI, OpenAI, Anthropic.
- **Embedding model:** separate from the chat model. Local via Ollama by default. Different embedding models produce incompatible vectors, so switching means re-embedding the whole bank.
- **Vector store:** free options range from in-memory or file-based (simplest for learning) to Postgres with pgvector or Qdrant.
- **Ingestion:** repeatable process that loads, chunks, embeds and stores the question bank.
- **Secrets:** API keys live in the C# backend only (user secrets in development), never in the React frontend.
- **.NET AI layer:** Microsoft.Extensions.AI (`IChatClient`, `IEmbeddingGenerator`) or Semantic Kernel, or the provider SDK directly. Calling the SDK directly teaches more about what happens underneath. Decide in the architecture step.

## Design notes from discovery

- The question bank is the natural RAG corpus. The resume is per-session context and likely does not need retrieval; extracting structured facts (skills, roles, projects) from it to steer question retrieval is the interesting part.
- End-of-interview coaching is its own skill area: structured output, rubric scoring and evaluating free-form answers.

## Downstream instruction to set up

- Learning commentary: build agents should explain each new React, C# or AI concept as it is introduced (what it is, why it is used here, which line to read). Configure through `bmad-customize` after the brief is finished.
