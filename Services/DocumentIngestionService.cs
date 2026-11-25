using SampleRAG.Models;

namespace SampleRAG.Services;

/// <summary>
/// Reads PDFs from the configured folder, embeds each chunk, and uploads the results.
/// </summary>
public class DocumentIngestionService(
    DocumentService documentService,
    EmbeddingService embeddingService,
    VectorSearchService vectorSearchService,
    IConfiguration configuration,
    ILogger<DocumentIngestionService> logger)
{
    public async Task ProcessDocumentsAsync(CancellationToken cancellationToken)
    {
        var folderPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            configuration["Documents:FolderPath"] ?? "Documents");

        Directory.CreateDirectory(folderPath);

        var files = Directory.GetFiles(folderPath, "*.pdf", SearchOption.TopDirectoryOnly);
        logger.LogInformation("Found {Count} PDF documents.", files.Length);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await IngestPdfAsync(file, cancellationToken);
        }
    }

    private async Task IngestPdfAsync(string filePath, CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(filePath);
        logger.LogInformation("Processing {FileName}", fileName);

        await using var stream = File.OpenRead(filePath);
        var chunks = documentService.ProcessPdf(stream, fileName, cancellationToken);

        if (chunks.Count == 0)
            return;

        var vectors = await embeddingService.GenerateEmbeddingsAsync(
            chunks.Select(c => c.Content),
            cancellationToken);

        await vectorSearchService.UploadChunksAsync(
            chunks.Zip(vectors),
            cancellationToken);

        logger.LogInformation("Indexed {Count} chunks from {FileName}.", chunks.Count, fileName);
    }
}
