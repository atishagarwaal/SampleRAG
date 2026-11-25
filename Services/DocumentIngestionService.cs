using SampleRAG.Models;

namespace SampleRAG.Services;

/// <summary>
/// Reads PDFs from the configured folder, embeds each chunk, and uploads the results.
/// Invoked once at startup from <c>Program.cs</c>.
/// </summary>
public class DocumentIngestionService
{
    private readonly DocumentService _documentService;
    private readonly EmbeddingService _embeddingService;
    private readonly VectorSearchService _vectorSearchService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DocumentIngestionService> _logger;

    public DocumentIngestionService(
        DocumentService documentService,
        EmbeddingService embeddingService,
        VectorSearchService vectorSearchService,
        IConfiguration configuration,
        ILogger<DocumentIngestionService> logger)
    {
        _documentService = documentService;
        _embeddingService = embeddingService;
        _vectorSearchService = vectorSearchService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Processes every PDF in the documents folder.
    /// Each run assigns new chunk ids, so starting the app again uploads another copy of the same files.
    /// </summary>
    public async Task ProcessDocumentsAsync(CancellationToken cancellationToken)
    {
        var folderPath = EnsureDocumentsFolder();
        var files = Directory.GetFiles(folderPath, "*.pdf", SearchOption.TopDirectoryOnly);

        _logger.LogInformation("Found {Count} PDF documents.", files.Length);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await IngestPdfAsync(file, cancellationToken);
        }
    }

    private string EnsureDocumentsFolder()
    {
        var folderPath = _configuration["Documents:FolderPath"] ?? "Documents";
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), folderPath);

        // Create the folder so a fresh checkout can start before any PDFs are added.
        Directory.CreateDirectory(fullPath);
        return fullPath;
    }

    private async Task IngestPdfAsync(string filePath, CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(filePath);

        _logger.LogInformation("Processing {FileName}", fileName);

        await using var stream = File.OpenRead(filePath);

        var chunks = await _documentService.ProcessPdfAsync(
            stream,
            fileName,
            cancellationToken);

        // Pair each chunk with its vector before upload. Search cannot rank a chunk that has no embedding.
        var indexedItems = new List<(DocumentChunk Chunk, float[] Vector)>(chunks.Count);

        foreach (var chunk in chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var vector = await _embeddingService.GenerateEmbeddingAsync(chunk.Content);
            indexedItems.Add((chunk, vector));
        }

        await _vectorSearchService.UploadChunksAsync(indexedItems, cancellationToken);

        _logger.LogInformation(
            "Indexed {Count} chunks from {FileName}.",
            chunks.Count,
            fileName);
    }
}
