# Review: Stack versions and reality checks

Subject: `ARCHITECTURE-SPINE.md` (draft, 2026-09-22). Reviewed on 2026-09-22. Spine not modified.

## Method

- Registry lookups: NuGet flat-container API, `npm view`, GitHub releases API, nodejs.org dist index, dotnetcli release metadata.
- Vendor pages: ollama.com library and tags, docs.ollama.com, learn.microsoft.com, the nomic-embed-text-v1.5 Hugging Face card, OllamaSharp source on GitHub.
- Executed probes on this machine (.NET SDK 10.0.401, Node 24.11.1, npm 11.6.2): a net10.0 web project with the exact spine package versions; an unmodified `create-vite@9.2.1` react-ts template with TS 7.0.2, react-router 8.4.0 and vitest 5.0.1 added, run through `tsc -b`, `vite build` and a vitest smoke test; a Kestrel SSE run over curl; an EF Core SQLite BLOB and DateTime round trip; a PdfPig text-extraction test.
- Nothing was run against a live Ollama (not installed), so model behavior is desk-checked only.

## Verdict

The Stack table is real and current. Every listed version exists on its registry and is the current stable release, with one exception: Node.js 24.11.1 is an old patch of the 24 LTS line. No hallucinated package or version. The committed decisions hold up against the live libraries, with one build-breaking mis-statement (Core cannot call `GetResponseAsync<T>` with only `.Abstractions`), one scaffold mismatch (create-vite pins TypeScript 6, not 7), and several live-default gaps around Ollama (context size, VRAM fit, grammar-constrained quoting) that the spine does not yet address.

## Findings (ordered by severity)

### F1. HIGH: Core cannot call `GetResponseAsync<T>` if it references only `Microsoft.Extensions.AI.Abstractions`

Spine line 35 ("It references `Microsoft.Extensions.AI.Abstractions` only") conflicts with AD-11 ("Any output the code reads uses `IChatClient.GetResponseAsync<T>`").

Evidence: learn.microsoft.com lists `ChatClientStructuredOutputExtensions.GetResponseAsync<T>` in Assembly `Microsoft.Extensions.AI.dll` (package Microsoft.Extensions.AI), not Abstractions. A net10.0 project referencing only `Microsoft.Extensions.AI.Abstractions` 10.10.0 fails with `CS0308: The non-generic method 'IChatClient.GetResponseAsync(...)' cannot be used with type arguments`. The same code builds once `Microsoft.Extensions.AI` 10.10.0 is referenced.

Consequence: the "Core references Abstractions only" rule (and any architecture test enforcing it) makes AD-11 unimplementable in Core as written. Fix options: (a) allow Core to reference `Microsoft.Extensions.AI` (still provider-free); or (b) put the typed call behind a Core port implemented in Infrastructure. Either way the sentence needs correcting. The Stack row already lists both packages, so only the prose is wrong.

### F2. MEDIUM: `npm create vite@latest -- --template react-ts` does not produce the spine's versions

Live scaffold (`create-vite` 9.2.1, the `latest` tag), `template-react-ts/package.json`: `react ^19.2.8`, `react-dom ^19.2.8`, `@vitejs/plugin-react ^6.1.1`, `vite ^8.3.0`, `@types/node ^24.13.3`, `oxlint ^1.81.0`, and `typescript ~6.0.2`. The spine says TypeScript 7.0.2 (npm `latest`; 7.0.1 was `rc`, `next` is 7.1.0-dev).

What I verified: replacing the pin with `typescript@7.0.2`, adding `react-router@8.4.0` and `vitest@5.0.1`, then `npm run build` (`tsc -b && vite build`) succeeds on Vite 8.3.0, and a vitest test importing `createMemoryRouter` from `react-router` passes. The scaffold's tsconfig (`erasableSyntaxOnly`, `verbatimModuleSyntax`, `moduleResolution: bundler`) is accepted by tsc 7.0.2. So the combination is compatible; it is just not what the scaffold gives you.

Also note the scaffold ships oxlint (not ESLint), has no test runner and no router. The spine says "Vite (react-ts template)" and lists Vitest and react-router; a builder agent will get TS 6 and no Vitest by default.

Recommendation: state that the scaffold is the starting point, that `typescript` is bumped to 7.0.2 (or accept 6.0.x and change the table), that Vitest is added by hand, and that lint is oxlint unless changed.

### F3. MEDIUM: Ollama's live default context is 4k tokens, and the spine has no `num_ctx` decision

docs.ollama.com/context-length: below 24 GB VRAM the default context is 4k, regardless of the model's 128K ceiling (gemma3:4b tag page says 128K). The 4 GB dev GPU (GTX 1650 Ti, 4096 MiB, detected on this machine) is in that tier. A Resume, planned questions, retrieved bank Questions and a growing 10-question transcript will approach 4k. AD-12 budgets coaching prompts only; interview turns have no budget (the earlier review-reconcile-inputs review raised the same point).

OllamaSharp supports setting it: `ChatOptions.AdditionalProperties["num_ctx"]` is mapped to `RequestOptions.NumCtx` (AbstractionMapper.cs, `TryAddOllamaOption(... OllamaOption.NumCtx ...)`). Recommend a `Providers:Chat:ContextLength` option (for example 8192) and a transcript-truncation rule. A larger num_ctx raises KV-cache VRAM, which interacts with F4.

### F4. MEDIUM: gemma3:4b probably does not fully fit 4 GB VRAM (unconfirmed)

ollama.com/library/gemma3/tags: `gemma3:4b` (the `latest` tag) is 3.3 GB on disk, `4b-it-qat` is 4.0 GB, `1b` is 815 MB (32K, text only), `12b` is 8.1 GB. The 4b model is multimodal, so the vision projector adds to the footprint, and KV cache and CUDA buffers come on top. On a 4 GB card that also drives a Windows desktop, expect partial CPU offload. The spine table does not claim a VRAM fit; the `.memlog.md` says "fits 4 GB VRAM tightly, may spill to CPU" and marks it an assumption to confirm. That is the honest position but it is not carried into the spine. Ollama is not installed here, so I could not measure.

Recommendation: keep gemma3:4b as default only if a first-run check (`ollama ps` showing the CPU/GPU split, plus tokens per second) is written down as a gate, and name a fallback (gemma3:1b is text-only and 815 MB but weaker; qwen3:4b has a thinking mode that OllamaSharp maps via `Think`, which must be disabled for streaming chat).

### F5. MEDIUM: Ollama grammar-constrained output has an open bug with quoted text, which AD-11 invites

AD-11 requires each strength and weak spot to carry "a short quote from that answer". Open Ollama issue #18094 (reported on 0.32.15): with the `format` JSON schema, models terminate generation early or silently swap `"` for `'` when the source text contains double-quoted terms. Reproduced with gemma3:12b (truncation) and gemma4:e4b / qwen3.5:9b (substitution); gemma3:4b is not named but uses the same grammar path. Interview answers regularly contain quoted terms.

Mitigations to record: the single retry in AD-11 should cover truncated JSON, the quote field should tolerate substitution, and the prompt rules should follow docs.ollama.com advice (also put the schema text in the prompt, temperature 0). `format` itself works with gemma3: OllamaSharp passes `ChatResponseFormatJson.Schema` into `ChatRequest.Format`, and Ollama docs say structured outputs work locally for text and vision models. The decision stands.

### F6. MEDIUM: PdfPig `Page.Text` has no line breaks; AD-13's "one paragraph per line" needs the right API

Probe with PdfPig 0.1.16 on a 4-line PDF: `page.Text` returned `Q1. What is ...?It inverts control ...Q2. Explain async/await.It is a state machine.` with no separators, so the line-anchored `Q<number>.` parser in AD-13 would find only `Q1.`. `ContentOrderTextExtractor.GetText(page)` (namespace `UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor`) returned proper newline-separated lines. The adapter must use it. Even then PDFs have lines, not paragraphs, and multi-column or wrapped layouts vary, so "one paragraph per line" cannot be guaranteed for PDF; the import report is the safety net. Version claim is fine: 0.1.16 (2026-08-22) is the newest stable; only `0.1.17-alpha-*` nightlies are newer. It has net6/8/9 and netstandard assets, no net10.0 asset, and runs on net10.0 via the net9.0 asset. PdfPig is pre-1.0, so pin exact.

### F7. LOW: Azure OpenAI coverage works only through the v1 endpoint, and the spine does not say so

`Microsoft.Extensions.AI.OpenAI` 10.10.0 (depends on `OpenAI` 2.13.0) builds `IChatClient` and `IEmbeddingGenerator<string, Embedding<float>>` from `OpenAIClient.GetChatClient(m).AsIChatClient()` and `GetEmbeddingClient(m).AsIEmbeddingGenerator()`; compiled here with a custom `Endpoint`. For Azure OpenAI, learn.microsoft.com documents the GA v1 API: plain `OpenAIClient` with `Endpoint = https://<resource>.openai.azure.com/openai/v1/` and an `ApiKeyCredential`, no `api-version`. `Azure.AI.OpenAI` is only needed for the older api-version style (newest NuGet release is `2.9.0-beta.1`, still beta). So AD-2's claim is true with two caveats for the config docs: the Endpoint must end in `/openai/v1/`, and `Model` is the Azure deployment name.

### F8. LOW: Node.js 24.11.1 is stale; current Node 24 LTS is 24.21.0

nodejs.org: v24.21.0 (2026-09-07, LTS "Krypton") is current on the 24 line; Node 26.9.0 is Current, not LTS. 24.11.1 satisfies every engines range in the tree (react-router `>=22.22.0`, vitest `^22.12 || ^24 || >=26`, vite and plugin-react `^20.19 || >=22.12`), but `jsdom` 30.1.0 (the usual Vitest DOM environment) requires `^24.15.0`; npm resolved 29.1.1 here. This is the locally installed version, so the table records reality; say "24.x LTS, minimum 24.15" or update.

### F9. LOW: EF Core SQLite returns `DateTime` with `Kind=Unspecified`, so AD-15's "ISO-8601 UTC" is not automatic

Probe: `CreatedAt = DateTime.UtcNow` stored and re-read came back `Kind=Unspecified` and serialized as `"2026-09-22T00:06:04.584636"` with no `Z`. Use `DateTimeOffset`, or a `ValueConverter` that sets `DateTimeKind.Utc` on read. The ER diagram says `datetime`; specify the mapping.

### F10. LOW: SSE framing detail for AD-6

`TypedResults.ServerSentEvents` exists in ASP.NET Core 10 and works from `MapPost` (compiled and run on Kestrel). The `IAsyncEnumerable<string>` overload takes one event name for all items, so the spine's three event types (`token`, `done`, `error`) need the `IAsyncEnumerable<SseItem<T>>` overload. Wire check: a token `" Hello\nworld"` is sent as two `data:` lines (`data:  Hello` and `data: world`), and an empty payload as `data: `. A hand-written `fetch` reader must strip exactly one leading space and join `data:` lines with `\n`, or the spine should say token payloads are JSON-encoded strings, which is simpler and safer.

### F11. LOW: minor consistency items

- AD-7 says `id (integer, autoincrement)`, the ER diagram says `long id`. SQLite `INTEGER PRIMARY KEY` is 64-bit; pick `long` in C#.
- `OllamaApiClient` holds one `SelectedModel` used for both chat and embeddings, so the composition root needs two client instances (chat and embedding models differ). Worth a note for the factory.
- nomic-embed-text needs the literal prefixes `search_document: ` and `search_query: ` (colon plus trailing space). AD-10 writes them without the space.

## Confirmations by requested claim

| # | Claim | Result |
| --- | --- | --- |
| 1 | OllamaSharp 5.4.30 implements `IChatClient` and `IEmbeddingGenerator`; compatible with M.E.AI 10.10.0 | CONFIRMED. Source: `class OllamaApiClient : IOllamaApiClient, IChatClient, IEmbeddingGenerator<string, Embedding<float>>`. Latest release (2026-07-24); net10.0 nuspec group depends on M.E.AI and Abstractions `>= 10.8.0`; restored alongside 10.10.0 with no conflict and compiled. Not exercised against a live Ollama. |
| 2 | M.E.AI.OpenAI 10.10.0 yields `IChatClient` and `IEmbeddingGenerator` for OpenAI and Azure OpenAI | CONFIRMED, with the F7 caveat (Azure via the v1 endpoint). Compiled. |
| 3 | `GetResponseAsync<T>` exists in M.E.AI 10.x and works with Ollama's format schema | EXISTS, but in `Microsoft.Extensions.AI`, not Abstractions (F1). `useJsonSchemaResponseFormat` defaults to true per learn.microsoft.com. OllamaSharp maps the schema to `ChatRequest.Format`. Works in principle; live bug F5. |
| 4 | `TypedResults.ServerSentEvents` in ASP.NET Core 10 | CONFIRMED (compiled and ran; see F10). |
| 5 | PdfPig 0.1.16 stable line, works on .NET 10 | CONFIRMED (latest stable, ran on net10.0). See F6 for the extraction caveat. |
| 6 | react-router 8.4.0, typescript 7.0.2, vite 8.3.0, vitest 5.0.1 real, current, mutually compatible and with React 19.3 | ALL REAL and each is the npm `latest` tag today (also react 19.3.0, react-dom 19.3.0, @vitejs/plugin-react 6.1.1, @types/react 19.3.0). Peers agree: plugin-react 6.1.1 needs `vite ^8`; vitest 5.0.1 accepts `vite ^6.4 || ^7 || ^8`; react-router 8.4.0 needs `react >= 19.2.7`. Build and vitest smoke pass. The scaffold does not agree (F2). |
| 7 | gemma3:4b and nomic-embed-text exist; fit ~4 GB VRAM; gemma3 supports structured outputs | EXIST (gemma3:4b 3.3 GB, 128K, multimodal; nomic-embed-text 274 MB, v1.5, 2K context on the Ollama page). VRAM fit unconfirmed and probably partial offload (F4). Structured outputs work, with the open quoting bug (F5). Ollama 0.34.2 is the current stable (2026-09-15); 0.34.3-rc1 is a prerelease. |
| 8 | nomic-embed-text needs `search_document:` / `search_query:` prefixes | CONFIRMED. The v1.5 model card says the text prompt "must include a task instruction prefix" (`search_document`, `search_query`, `clustering`, `classification`). The Ollama library page does not mention them and Ollama does not add them, so the AD-10 decorator is needed. 768 dimensions confirmed (Matryoshka down to 64). |
| 9 | EF Core SQLite 10.0.12 stores float32 BLOB as `byte[]` | CONFIRMED. 768 floats copied to a 3072-byte `byte[]`, column type `BLOB`, read back exact. 10.0.12 is the latest stable 10.0 (11.0 is at rc.1). DateTime caveat F9. |

## Stack table check

| Row | Spine | Live (2026-09-22) | Status |
| --- | --- | --- | --- |
| .NET SDK / TFM | 10.0.401 / net10.0 | SDK 10.0.401 (installed here), runtime 10.0.12, LTS, active | OK |
| Microsoft.Extensions.AI and .Abstractions | 10.10.0 | 10.10.0 | OK |
| OllamaSharp | 5.4.30 | 5.4.30 | OK |
| Microsoft.Extensions.AI.OpenAI | 10.10.0 | 10.10.0 (depends on OpenAI 2.13.0) | OK |
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.12 | 10.0.12 | OK |
| DocumentFormat.OpenXml | 3.5.1 | 3.5.1 | OK |
| PdfPig | 0.1.16 | 0.1.16 (0.1.17 alphas only) | OK |
| xunit.v3 | 4.0.1 | 4.0.1 | OK (existence only) |
| Node.js / npm | 24.11.1 / 11.6.2 | 24.21.0 is current 24.x LTS | Stale (F8) |
| React / react-dom | 19.3.0 | 19.3.0 | OK |
| TypeScript | 7.0.2 | 7.0.2 (scaffold pins ~6.0.2) | OK; scaffold mismatch (F2) |
| Vite / plugin-react | 8.3.0 / 6.1.1 | 8.3.0 / 6.1.1 | OK |
| react-router | 8.4.0 | 8.4.0 | OK |
| Vitest | 5.0.1 | 5.0.1 | OK |
| Ollama | 0.34.2 | 0.34.2 (0.34.3-rc1 pre-release out) | OK |
| gemma3:4b / nomic-embed-text | as named | both exist | OK; see F3 to F5 |

## Not verified

- Behavior against a running Ollama 0.34.2 (not installed): actual VRAM use of gemma3:4b, structured-output quality, whether the quoting bug hits gemma3:4b.
- xunit.v3 4.0.1 with `dotnet test` on SDK 10 (Microsoft Testing Platform vs VSTest mode) and its runner packages.
- Azure OpenAI end to end (only compiled against a placeholder endpoint).
- DocumentFormat.OpenXml text extraction behavior.
