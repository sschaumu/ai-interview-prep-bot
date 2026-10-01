# Addendum: ai-interview-prep-bot PRD

Technical detail and options that belong in architecture, not in the PRD. See also the brief's addendum for AI setup options.

## Bulk import notes

- Word, PDF and plain text each need a text-extraction step. Extraction quality varies, and scanned PDFs have no text layer.
- If files use a consistent pattern (Q:/A: or numbered items), parse with rules. If not, an LLM-assisted extraction step is needed. Small local models are unreliable at this, so it is a reason to keep the Provider swappable.
- The import report (FR-1) should list unreadable parts so nothing disappears silently.

## Retrieval notes

- Question Bank is the RAG corpus. Each Question and its Reference Answer are a natural unit to embed.
- Changing the embedding Provider produces incompatible vectors, hence FR-10.
- The Resume is per-session prompt context. Extracting structured facts (skills, roles, projects) to steer which Questions are retrieved is an option.

## Coaching notes

- Coaching quality depends on the model. Reference Answers give it something to compare against. Structured output and a scoring rubric are the likely approach.

## Downstream instruction

- Learning commentary: build agents should explain each new React, C# or AI concept as it is introduced. Configure with `bmad-customize` before the build stage.

## Findings from the sample files (2026-09-21)

**Sample question bank** (`NET_Azure_Interview_Question_Bank.docx`)
- 87 Questions, numbered Q1 to Q87 with no gaps. Each is a Word "Heading 2" reading "Q<n>. <question>?", followed by the answer as plain paragraphs and bullet lists until the next heading.
- Before Q1 there is a title, a subtitle line and a "Useful Links" list. These are not Questions and should be skipped.
- Every Question has a Reference Answer. Answers vary from one paragraph to about ten paragraphs and bullets, and later Questions are scenario-style (for example Q86 and Q87).
- No topic tags, categories or difficulty. The content is all .NET and Azure.
- Parse on the "Q<n>." text pattern, not on Word heading styles. Heading styles do not survive PDF or text, while the pattern does.
- Reference Answers are long. Feeding several into a small local model's prompt at once will use a lot of its context, so retrieve the Question and pull the answer only when coaching that answer.

**Sample resume** (`Simon Schaumuller Resume 2026.docx`)
- About 55 short paragraphs (roughly two pages), with sections Professional Summary, Core Skills, Experience and Side Projects.
- Heading styles are inconsistent (some body paragraphs are styled as headings), so do not rely on styles to find sections. Treat the extracted text as one block and let the model find the sections.
- Short enough to sit whole in the prompt, so no retrieval is needed for the Resume.
