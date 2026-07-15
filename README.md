# Architecture Through Engineering – RAG

A production-oriented Retrieval-Augmented Generation (RAG) reference implementation in .NET 10, built to teach architecture through working software.

**Milestone 1** runs entirely on a developer machine. **Milestone 2** will swap local infrastructure for Azure services without changing Domain or Application layers.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker (optional, for containerized runs)

## Quick start

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Rag.Api
```

- Health check: `GET /health`
- Swagger UI (Development): `https://localhost:<port>/swagger`
- Upload document: `POST /api/documents` (multipart, `.txt` or `.md` only)
- Get document metadata: `GET /api/documents/{id}`

### Local infrastructure

```bash
docker compose up -d   # starts Qdrant (used in later slices)
```

## Solution structure

```text
src/
  Rag.Domain              Business concepts
  Rag.Application         Use cases and abstractions (ports)
  Rag.Infrastructure      Shared infrastructure contracts
  Rag.Infrastructure.Local  Milestone 1 local implementations
  Rag.Api                 HTTP API (composition root)

tests/
  Rag.Application.Tests
  Rag.Infrastructure.Tests
  Rag.Api.IntegrationTests

docs/
  Architecture.md C4 model, architectural flows, and system overview
  ADR/            Architectural decision records
```

## Documentation

- [Architecture](docs/Architecture.md)
- [ADR 001: Clean Architecture layering](docs/ADR/001-clean-architecture-layering.md)

## Development Approach

This repository uses modern AI-assisted engineering tools to accelerate implementation. Architecture, design decisions, reviews, and technical direction remain human-driven and are recorded in architecture documentation and ADRs.

Development proceeds incrementally. Each slice includes architectural intent, trade-offs, implementation, and review before the next slice begins.
