
using SampleRAG.Models;

namespace SampleRAG.Services;

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

    public async Task ProcessDocumentsAsync(
        CancellationToken cancellationToken)
    {
        var folderPath = _configuration["Documents:FolderPath"]
            ?? "Documents";

        var fullPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            folderPath);

        Directory.CreateDirectory(fullPath);

        var files = Directory.GetFiles(
            fullPath,
            "*.pdf",
            SearchOption.TopDirectoryOnly);

        _logger.LogInformation(
            "Found {Count} PDF documents.", files.Length);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileName = Path.GetFileName(file);

            _logger.LogInformation(
                "Processing {FileName}", fileName);

            await using var stream = File.OpenRead(file);

            var chunks = await _documentService.ProcessPdfAsync(
                stream,
                fileName,
                cancellationToken);

            var indexedItems =
                new List<(DocumentChunk Chunk, float[] Vector)>();

            foreach (var chunk in chunks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var vector =
                    await _embeddingService.GenerateEmbeddingAsync(
                        chunk.Content);

                indexedItems.Add((chunk, vector));
            }

            await _vectorSearchService.UploadChunksAsync(
                indexedItems,
                cancellationToken);

            _logger.LogInformation(
                "Indexed {Count} chunks from {FileName}.",
                chunks.Count,
                fileName);
        }
    }
}
