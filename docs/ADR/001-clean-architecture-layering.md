# ADR 001: Clean Architecture Layering

## Status

Accepted

## Context

We need a RAG reference implementation that:

1. Runs locally in Milestone 1
2. Can adopt Azure services in Milestone 2 without rewriting business logic
3. Remains understandable as a teaching artifact

## Decision

Adopt Clean Architecture with five projects:

- `Rag.Domain` – entities and enums only
- `Rag.Application` – ports (interfaces) and future use-case services
- `Rag.Infrastructure` – shared infrastructure helpers
- `Rag.Infrastructure.Local` – Milestone 1 concrete adapters
- `Rag.Api` – composition root and HTTP API

The API references `Infrastructure.Local` directly. Application code depends only on abstractions defined in `Rag.Application`.

## Consequences

**Positive**

- Milestone 2 adds `Rag.Infrastructure.Azure` (or similar) and changes one DI registration block
- Domain and Application remain testable without HTTP or external services
- Clear onboarding path for engineers reading the codebase

**Negative**

- More projects than a minimal demo would require
- Composition root must be disciplined—no leaking concrete types into Application

## Alternatives considered

**Single project** – Faster to scaffold but couples HTTP, storage, and LLM calls; unsuitable for Milestone 2 swap.

**Semantic Kernel as orchestration layer** – Deferred. SK adds opinionated abstractions that overlap with our ports. We will use thin HTTP/SDK adapters behind Application interfaces unless SK proves necessary for a specific capability.
