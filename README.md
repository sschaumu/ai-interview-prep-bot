# AI Interview Prep Bot

A local-first mock interview app: React + Vite frontend, .NET 10 minimal API backend, SQLite storage, Ollama for local models.

This is also a learning project: it is a hands-on way to explore building with local LLMs and embeddings, a .NET + React stack, and the BMAD AI-driven development workflow.

## Prerequisites

- .NET SDK 10.0.401
- Node.js 24 LTS (npm 11)
- [Ollama](https://ollama.com) 0.34 or later

## First-run setup

```bash
ollama pull nomic-embed-text   # embedding model used for Questions
ollama pull gemma3:4b          # chat model (used from Epic 2)
ollama ps                      # check the chat model fits your GPU; if not, use qwen3:4b or smaller
```

## Run

Start the backend (Kestrel on `http://localhost:5080`, loopback only) and the frontend (Vite on `http://localhost:5173`) in two terminals:

```bash
dotnet run --project backend/src/InterviewPrepBot.Api
npm --prefix frontend install
npm --prefix frontend run dev
```

Open <http://localhost:5173>. Vite proxies `/api` to Kestrel, so the browser only calls relative URLs.

In Development the SQLite file is created at `data/interview-prep.db` (git-ignored) and migrated at startup. Outside Development the `Storage:Path` setting is relative to the Api project's content root.

## Configuration

`backend/src/InterviewPrepBot.Api/appsettings.json`:

| Section | Purpose |
|---|---|
| `Providers:Embedding` | `Type` (`ollama`), `Model`, `Endpoint`, `Dimensions` |
| `Storage:Path` | SQLite file |
| `Security:AllowedOrigins` | Origins allowed on non-GET requests; anything else gets `403 invalid-request` |

API keys, when a hosted Provider is added, come from .NET user-secrets in development or environment variables elsewhere. No endpoint returns configuration.

## Test

```bash
dotnet test --solution backend/InterviewPrepBot.slnx
npm --prefix frontend test -- --run
npm --prefix frontend run build
```

Backend tests use fake embedders and never call a real model.
