# Architecture Overview

## Purpose

A production-oriented RAG reference implementation in .NET 10. Milestone 1 runs entirely on a developer machine. Milestone 2 swaps local infrastructure for Azure services without changing Domain or Application layers.

When retrieval returns no relevant context, the system refuses to fabricate an answer.

## C4 Model (Levels 1–3)

```mermaid
flowchart TB
    subgraph C1["C1 — System Context"]
        Developer(["Developer"])
        Ollama(["Ollama<br/>embeddings + chat"])
        Qdrant(["Qdrant<br/>vector search"])

        subgraph RAG["RAG System"]
            subgraph C2["C2 — Containers"]
                API["ASP.NET Core API<br/>HTTP · Swagger · DI composition root"]
                DocStore["Document Store<br/>SQLite metadata · local files"]

                subgraph C3["C3 — Components"]
                    Controllers["Controllers<br/>Documents, Health"]
                    UseCases["Use Case Services<br/>DocumentUploadService"]
                    Ports["Ports<br/>IDocumentRepository · IVectorStore · IChatCompletionService"]
                    Adapters["Local Adapters<br/>SqliteDocumentRepository · LocalDocumentFileStore"]
                    Domain["Domain<br/>Document · DocumentChunk · RetrievedChunk"]

                    Controllers --> UseCases
                    UseCases --> Ports
                    UseCases --> Domain
                    Ports -.->|implements| Adapters
                end
            end
        end

        Developer -->|"upload .txt/.md, chat"| API
        API --> DocStore
        API -.->|"Slice 4+"| Ollama
        API -.->|"Slice 5+"| Qdrant
    end
```

| Level | What it shows |
|-------|---------------|
| **C1 — System Context** | Who uses the system and which external services it depends on |
| **C2 — Containers** | Deployable runtime units inside the system boundary |
| **C3 — Components** | Clean Architecture structure inside the API container |

Dashed arrows (`-.->`) mark dependencies planned in upcoming slices. Solid arrows are implemented today.

## Dependency rule

Project references point inward only. **Application never references Infrastructure.**

```text
Api → Application, Infrastructure.Local     (composition root wires both)
Infrastructure.Local → Infrastructure, Application
Infrastructure → Application
Application → Domain
Domain → (nothing)
```

| Project | May reference | Must not reference |
|---------|---------------|-------------------|
| `Rag.Domain` | — | anything |
| `Rag.Application` | `Domain` | `Infrastructure`, `Api` |
| `Rag.Infrastructure` | `Application` | `Api`, `Infrastructure.Local` |
| `Rag.Infrastructure.Local` | `Infrastructure`, `Application` | `Api` |
| `Rag.Api` | `Application`, `Infrastructure.Local` | — (composition root) |

At runtime, use cases depend on **ports** (`IDocumentRepository`). The Api registers **adapters** (`SqliteDocumentRepository`) via DI — without Application knowing the concrete type.

## RAG pipeline (target state)

```text
Upload → Parse → Chunk → Embed → Vector Store
                                      ↓
Chat Query → Retrieve → Grounded LLM Response
```

## Key ports (Application abstractions)

| Port | Milestone 1 | Milestone 2 swap |
|------|---------------|------------------|
| `IDocumentRepository` | SQLite | Azure SQL / Cosmos |
| `IDocumentFileStore` | Local filesystem | Azure Blob Storage |
| `IDocumentParser` | Text / Markdown | Same interface |
| `ITextChunker` | Fixed-size chunker | Same interface |
| `IEmbeddingGenerator` | Ollama | Azure OpenAI |
| `IVectorStore` | Qdrant | Azure AI Search |
| `IChatCompletionService` | Ollama | Azure OpenAI |

## Cross-cutting concerns (Api)

- Structured logging via `Microsoft.Extensions.Logging`
- `ProblemDetails` for consistent error responses
- Health checks at `/health`
- Swagger UI in Development
