using Microsoft.Extensions.Options;
using SampleRAG.Models;
using SampleRAG.Options;

namespace SampleRAG.Services;

/// <summary>
/// Reads pdf documents, embeds each chunk, and uploads the results.
/// </summary>
public class DocumentIngestionService(
    DocumentService documentService,
    EmbeddingService embeddingService,
    VectorSearchService vectorSearchService,
    IOptions<DocumentsOptions> documentsOptions,
    ILogger<DocumentIngestionService> logger)
{
    private readonly DocumentsOptions _documentsOptions = documentsOptions.Value;

    /// <summary>
    /// Reads pdf documents, embeds each chunk, and uploads the results.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ProcessDocumentsAsync(CancellationToken cancellationToken)
    {
        // Get the folder path from the configuration.
        var folderPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            _documentsOptions.FolderPath);

        // Create the directory if it doesn't exist.
        Directory.CreateDirectory(folderPath);

        // Get all the pdf documents in the directory.
        var files = Directory.GetFiles(folderPath, "*.pdf", SearchOption.TopDirectoryOnly);

        // Log the number of documents found.
        logger.LogInformation("Found {Count} PDF documents.", files.Length);

        // Process each document.
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Ingest the pdf document.
            await IngestPdfAsync(file, cancellationToken);
        }
    }

    /// <summary>
    /// Ingest a pdf document.
    /// </summary>
    /// <param name="filePath">The path to the pdf document.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task IngestPdfAsync(string filePath, CancellationToken cancellationToken)
    {
        // Get the file name.
        var fileName = Path.GetFileName(filePath);
        logger.LogInformation("Processing {FileName}", fileName);

        // Open the file.
        await using var stream = File.OpenRead(filePath);

        // Process the pdf document and get the chunks.
        var chunks = documentService.ProcessPdf(stream, fileName, _documentsOptions, cancellationToken);

        // If no chunks are found, return.
        if (chunks.Count == 0)
        {
            return;
        }

        // Generate embeddings for the chunks.
        var vectors = await embeddingService.GenerateEmbeddingsAsync(
            chunks.Select(c => c.Content),
            cancellationToken);

        // Upload the chunks to the vector database.
        await vectorSearchService.UploadChunksAsync(
            chunks.Zip(vectors),
            cancellationToken);

        logger.LogInformation("Indexed {Count} chunks from {FileName}.", chunks.Count, fileName);
        logger.LogInformation("Vector information: {VectorInformation}", vectors.Select(v => v.ToString()).ToList());
    }
}
