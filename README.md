# SampleRAG

## 1. Overview

**SampleRAG** is an ASP.NET Core Web API (.NET 10) demonstrating a Retrieval-Augmented Generation (RAG) pipeline built with **Azure AI Foundry** (OpenAI-compatible chat and embedding models) and **Azure AI Search** (vector search).

On startup, the application automatically creates the Azure AI Search vector index (if it does not already exist), extracts and chunks PDF documents from the local `Documents/` folder, generates embeddings for each chunk, and uploads them to the search index. Clients can then query the documents via a REST API endpoint (`POST /api/chat/ask`) and receive grounded answers along with document/page source references.

---

## 2. Description

### Architecture & Workflow

1. **Document Ingestion (Startup)**
   - `SearchIndexService` ensures the configured Azure AI Search index exists, configured with an HNSW vector search profile (`1536` dimensions on `contentVector`).
   - `DocumentIngestionService` scans the configured `Documents` folder (default: `Documents/`, which includes sample PDFs `Brewing_Guide.pdf` and `Coffee_Menu_and_Prices.pdf`).
   - `DocumentService` extracts text from PDF files using **PdfPig** and splits the content into chunks.
   - `EmbeddingService` (backed by `FoundryService`) generates vector embeddings for each chunk.
   - `VectorSearchService` batches and uploads the chunks and their vectors to Azure AI Search.

2. **Question Answering (`POST /api/chat/ask`)**
   - `ChatController` accepts a JSON request containing a `question` and delegates to `RAGService`.
   - `RAGService` embeds the question, retrieves the top 5 most similar chunks from Azure AI Search via vector similarity search, and builds a grounded context prompt.
   - The prompt is sent to the Azure AI Foundry chat deployment, which returns a concise answer strictly based on the retrieved context, along with `sources` (`documentName` and `pageNumber`).

### Azure Resources & How It Works

This project uses three Azure resources:

- **Azure AI Search (`sampleaisearch-poc`)** — Stores document chunks and vectors for search.
- **Microsoft Foundry (`samplerag-resource`)** — Hosts the AI models.
- **Foundry Project (`samplerag`)** — Holds the deployed chat and embedding models.

![Azure Resources](docs/images/01-azure-resources.png)

#### 1. Model Deployments & Access (IAM)

Two models are deployed in Microsoft Foundry:
- **`text-embedding-3-small`** turns text into vectors.
- **`gpt-4.1-nano`** answers user questions using the retrieved text.

![Microsoft Foundry Model Deployments](docs/images/09-foundry-model-deployments.png)

The app signs in with **Microsoft Entra ID** (`DefaultAzureCredential`) instead of using API keys. This is needed for two reasons:
- **Better security (no secrets in config):** You don't have to store or rotate sensitive API keys in `appsettings.json`. Instead, the app automatically gets short-lived tokens from your Azure sign-in (`az login` locally, or Managed Identity in Azure).
- **Required for authorization (Control plane vs. Data plane):** Even if you are the **Owner** or **Contributor** who created the Foundry resources, Azure separates permissions into two layers:
  - **Control plane (`Owner` / `Contributor`)** — Lets you *manage* resources (create resources, deploy models, change settings).
  - **Data plane (`Cognitive Services OpenAI User` / `Azure AI User`)** — Lets you *call* the models (send chat prompts and generate embeddings).

  By design, **Owner/Contributor does not automatically include data-plane access**. Because Entra ID enforces RBAC (unlike API keys), you must explicitly assign a data-plane role to your user on the Foundry resource and project—otherwise model calls fail with `403 Forbidden`.

![Foundry Resource IAM](docs/images/07-foundry-resource-iam.png)

![Foundry Project IAM](docs/images/08-foundry-project-iam.png)

#### 2. Azure AI Search & Vector Index

Azure AI Search (`sampleaisearch-poc`) stores the searchable knowledge base.

![Azure AI Search Overview](docs/images/02-ai-search-overview.png)

When the app starts, it creates `samplerag-index` and indexes the PDFs from `Documents/` into 12 chunks.

![Azure AI Search Indexes](docs/images/03-ai-search-indexes.png)

The index stores each chunk's text (`content`), source info (`documentName`, `pageNumber`), and its 1536-dimension vector (`contentVector`) using the `vector-profile` (`hnsw-config`). See [Vector Profile Explained](docs/vector-profile.md) for details on how it works.

![Index Fields](docs/images/05-index-fields.png)

![Vector Profiles](docs/images/06-vector-profiles.png)

You can view the indexed PDF chunks in **Search explorer**. When a user asks a question, the app finds the most relevant chunks here and sends them to `gpt-4.1-nano` to generate the answer.

![Search Explorer Documents](docs/images/04-search-explorer-documents.png)

### Project Structure

```text
SampleRAG/
├── Controllers/
│   └── ChatController.cs          # REST endpoint: POST /api/chat/ask
├── Documents/                     # Source PDFs ingested at startup
│   ├── Brewing_Guide.pdf
│   └── Coffee_Menu_and_Prices.pdf
├── Models/
│   ├── ChatRequest.cs             # Request payload
│   ├── ChatResponse.cs            # Answer + source references
│   └── DocumentChunk.cs           # Chunk model (id, documentName, pageNumber, content)
├── Options/
│   └── FoundryOptions.cs          # Strongly-typed configuration for Azure AI Foundry
├── Services/
│   ├── DocumentIngestionService.cs
│   ├── DocumentService.cs         # PDF text extraction & chunking (PdfPig)
│   ├── EmbeddingService.cs        # Text embedding generation
│   ├── FoundryService.cs          # Azure AI Foundry client (Microsoft Entra ID auth)
│   ├── RAGService.cs              # End-to-end RAG orchestration
│   ├── SearchIndexService.cs      # Azure AI Search index provisioning
│   └── VectorSearchService.cs     # Vector upload and similarity search
├── Program.cs                     # DI setup, startup indexing, OpenAPI/Swagger UI
├── appsettings.json               # Application configuration
├── SampleRAG.csproj               # .NET 10 Web API project file
└── SampleRAG.slnx                 # Solution file
```

---

## 3. End-to-End RAG Workflow

The RAG pipeline operates in two distinct phases: an **offline ingestion phase** that runs automatically on application startup, and an **online query phase** triggered whenever a client calls `POST /api/chat/ask`.

Each box names the class that performs that step. `Program.cs` starts Phase 1 by calling `SearchIndexService` (field names and the HNSW profile come from `SearchIndexSchema`), then `DocumentIngestionService`, which runs extraction, embedding, and upload for every PDF. Phase 2 starts at `ChatController`, which passes the question to `RAGService`. `FoundryService` supplies the Entra-authenticated clients used by `EmbeddingService` and by the chat call.

```mermaid
flowchart LR
    subgraph Ingestion["Phase 1: Ingestion (Program.cs → DocumentIngestionService)"]
        A["PDF Files\n(Documents/)"] --> B["Extract & Chunk\nDocumentService\n(PdfPig: 1000 chars,\n200 overlap)"]
        B --> C["Generate Embeddings\nEmbeddingService\nvia FoundryService\n(text-embedding-3-small)"]
        C --> D["Upload in Batches\nVectorSearchService\n(up to 500/batch)"]
    end

    S["Create Index\nSearchIndexService\n+ SearchIndexSchema"] --> E[("Azure AI Search\n(samplerag-index\nHNSW vector index)")]
    D --> E

    subgraph Query["Phase 2: Retrieval & Generation (ChatController → RAGService)"]
        F["User Question\nChatController"] --> G["Embed Question\nEmbeddingService\nvia FoundryService"]
        G --> H["Vector Search\nVectorSearchService\n(Top 5 kNN chunks)"]
        E --> H
        H --> I["Build Grounded Prompt\nRAGService"]
        I --> J["Generate Answer\nFoundryService\n(gpt-4.1-nano)"]
        J --> K["JSON Response\nRAGService\n(Answer + Deduplicated Sources)"]
    end
```

### Phase 1: Document Ingestion & Indexing (Startup)

1. **Index Provisioning (`SearchIndexService`)** — On startup, `Program.cs` ensures the Azure AI Search index (`samplerag-index`) exists with the schema (`id`, `documentName`, `pageNumber`, `content`, `contentVector`) and an HNSW vector profile configured for `1536`-dimensional vectors.
2. **PDF Discovery & Text Extraction (`DocumentIngestionService` & `DocumentService`)** — Scans the configured `Documents/` folder for `*.pdf` files and extracts text page by page using **PdfPig**, skipping blank or image-only pages.
3. **Sliding-Window Chunking (`DocumentService`)** — Splits each page's text into **1,000-character** chunks with a **200-character overlap** so context is preserved across chunk boundaries. Each `DocumentChunk` retains its `documentName` and `pageNumber` metadata.
4. **Embedding Generation (`EmbeddingService` → Microsoft Foundry)** — Sends each chunk's text to the deployed `text-embedding-3-small` model (authenticated via Microsoft Entra ID) to produce a `1536`-dimensional vector.
5. **Batch Upload (`VectorSearchService` → Azure AI Search)** — Uploads the chunks and their vectors to Azure AI Search in batches of up to **500 documents**.

### Phase 2: Retrieval & Grounded Generation (`POST /api/chat/ask`)

1. **Question Embedding (`RAGService` → `EmbeddingService`)** — Converts the incoming user `question` into a `1536`-dimensional query vector using the same `text-embedding-3-small` model so it lives in the same vector space as the indexed chunks.
2. **Vector Similarity Search (`VectorSearchService` → Azure AI Search)** — Runs a k-nearest-neighbor (kNN) vector search against `contentVector` to retrieve the **top 5** most semantically relevant chunks (`topK = 5`). If no chunks are found, the API immediately returns a fallback message.
3. **Prompt Augmentation (`RAGService`)** — Formats the retrieved chunks (labeled with their `Source` file name and `Page` number) into a context block and pairs it with a strict system prompt that instructs the model to:
   - Answer using *only* the provided context.
   - Avoid inventing facts or making unsupported assumptions.
   - Explicitly state when the answer is not in the documents.
   - Ignore any prompt-injection instructions found inside the retrieved text.
4. **Grounded Answer Generation (`FoundryService` → Microsoft Foundry)** — Sends the system and user messages to the `gpt-4.1-nano` chat deployment to generate a concise, grounded response.
5. **Source Attribution (`RAGService` → `ChatController`)** — Deduplicates the retrieved chunks by `(documentName, pageNumber)` and returns a `ChatResponse` JSON payload containing both the generated `answer` and its `sources`.

---

## 4. Pre-requisites

- **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)**
- **Azure AI Foundry / Azure OpenAI Resource** with:
  - A **chat model deployment** (e.g., `gpt-4o`, `gpt-4o-mini`, etc.).
  - An **embedding model deployment** that outputs **1536-dimensional** vectors (e.g., `text-embedding-3-small` or `text-embedding-ada-002`).
  - **Microsoft Entra ID authentication** configured (the app uses `DefaultAzureCredential` with scope `https://cognitiveservices.azure.com/.default`). Ensure your signed-in identity (e.g., via `az login` or Visual Studio) has the **Cognitive Services OpenAI User** role on the resource.
- **Azure AI Search Service**:
  - Service endpoint URL and an Admin API key (required to create the index and upload documents).
- **Azure CLI** (recommended for local Entra ID authentication via `az login`).

---

## 5. Build and Run

### 1. Configure Settings

Update `appsettings.json` (or use `appsettings.Development.json` / .NET User Secrets) with your Azure AI Foundry and Azure AI Search details:

```json
{
  "Documents": {
    "FolderPath": "Documents"
  },
  "Foundry": {
    "Endpoint": "https://<your-foundry-resource>.openai.azure.com/openai/v1",
    "ChatDeployment": "<your-chat-deployment-name>",
    "EmbeddingDeployment": "<your-embedding-deployment-name>",
    "TenantId": "<your-entra-tenant-id>"
  },
  "AzureSearch": {
    "Endpoint": "https://<your-search-service>.search.windows.net",
    "ApiKey": "<your-azure-search-admin-api-key>",
    "IndexName": "<your-index-name>"
  }
}
```

### 2. Sign in to Azure (for Entra ID Auth)

```bash
az login
```

### 3. Restore and Build

From the solution root (`SampleRAG`):

```bash
dotnet restore
dotnet build
```

### 4. Run the Application

```bash
dotnet run
```

By default, the API starts on:
- **HTTP:** `http://localhost:5021`
- **HTTPS:** `https://localhost:7276` (when using the `https` launch profile)

On startup, the app will create the Azure AI Search index (if needed) and ingest all `.pdf` files in the `Documents/` folder.

### 5. Test the API

In Development mode, Swagger UI is available at:
- `http://localhost:5021/swagger`

Or send a `POST` request directly to `/api/chat/ask`.

#### Sample Request

```bash
curl -X POST http://localhost:5021/api/chat/ask \
  -H "Content-Type: application/json" \
  -d '{"question": "What coffee drinks are on the menu?"}'
```

#### Sample Response

```json
{
  "answer": "The menu includes Espresso (Double Shot), Americano, Cappuccino, Cafe Latte, Mocha, Iced Latte, Cold Brew (16-hour steep), and Iced Caramel Macchiato.",
  "sources": [
    {
      "documentName": "Coffee_Menu_and_Prices.pdf",
      "pageNumber": 2
    }
  ]
}
```

> The answer wording is generated by the model, so your output will differ slightly. The `sources` list shows the document and page the answer was drawn from.
