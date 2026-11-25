# Avoiding Duplicate Indexing with a File Hash Check

## Why Duplicate Indexing Happens

Currently, `Program.cs` runs `DocumentIngestionService.ProcessDocumentsAsync` on every application startup, and `DocumentChunk.Id` defaults to `Guid.NewGuid().ToString()`. As a result, restarting the app re-processes every PDF in `Documents/` and uploads duplicate chunks to Azure AI Search.

---

## The File Hash Check Strategy

Instead of checking only by `documentName`, compute a **SHA-256 hash** of each PDF file's contents and store it in a `fileHash` field in the Azure AI Search index (or in a local manifest file).

This handles both:

- **Unchanged PDFs** — skipped immediately without re-parsing or calling the embedding API.
- **Updated PDFs (same filename, new content)** — detected automatically, old chunks removed, and new chunks indexed.

### Flow

```text
PDF file
  │
  ▼
Compute SHA-256 hash
  │
  ▼
Query index for existing `fileHash` by `documentName`
  │
  ├── Matches existing hash ──► Skip file (no embedding calls, no upload)
  │
  ├── Different hash ─────────► Delete old chunks for `documentName` ──► Chunk, embed, upload
  │
  └── Not found in index ─────► Chunk, embed, upload
```

---

## Implementation Overview

### 1. Add `fileHash` to the Index Schema (`Services/SearchIndexService.cs`)

Add a filterable `fileHash` field when creating the index:

```csharp
new SimpleField("fileHash", SearchFieldDataType.String)
{
    IsFilterable = true
}
```

> **Note:** Azure AI Search does not automatically add new fields to an existing index if `CreateIndexIfNotExistsAsync` returns early. You will need to update or recreate the index once to include `fileHash`.

### 2. Add `FileHash` to `Models/DocumentChunk.cs`

```csharp
public string FileHash { get; set; } = string.Empty;
```

Include `["fileHash"] = item.Chunk.FileHash` when building each `SearchDocument` in `VectorSearchService.UploadChunksAsync`.

### 3. Compute the SHA-256 Hash

```csharp
using System.Security.Cryptography;

private static async Task<string> ComputeFileHashAsync(
    string filePath,
    CancellationToken cancellationToken)
{
    await using var stream = File.OpenRead(filePath);
    var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
    return Convert.ToHexString(hashBytes);
}
```

### 4. Query & Delete Helpers (`Services/VectorSearchService.cs`)

- **Get existing hash:** Query with `Filter = $"documentName eq '{escapedName}'"` and `Size = 1`, selecting `fileHash`.
- **Delete stale chunks:** If the stored hash differs from the newly computed hash, fetch all chunk `id`s matching `documentName eq '{escapedName}'` and delete them with `IndexDocumentsBatch.Delete("id", ids)` before uploading the new chunks.

### 5. Check Before Processing (`Services/DocumentIngestionService.cs`)

For each PDF:

1. Compute `fileHash`.
2. Look up the stored hash in Azure AI Search.
3. If `storedHash == fileHash`, log and `continue`.
4. If `storedHash != null`, delete the old chunks for `fileName`.
5. Extract chunks, set `chunk.FileHash = fileHash`, generate embeddings, and upload.
