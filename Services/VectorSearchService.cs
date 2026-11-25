using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Options;
using SampleRAG.Models;
using SampleRAG.Options;

namespace SampleRAG.Services;

/// <summary>
/// Uploads document chunks to Azure AI Search and retrieves the nearest chunks for a query vector.
/// </summary>
public class VectorSearchService(IOptions<AzureSearchOptions> options)
{
    private const int UploadBatchSize = 500;

    private readonly SearchClient _searchClient = new(
        new Uri(options.Value.Endpoint),
        options.Value.IndexName,
        new AzureKeyCredential(options.Value.ApiKey));

    /// <summary>
    /// Uploads chunks and their embedding vectors in fixed-size batches.
    /// </summary>
    public async Task UploadChunksAsync(
        IEnumerable<(DocumentChunk Chunk, float[] Vector)> items,
        CancellationToken cancellationToken = default)
    {
        var uploadOptions = new IndexDocumentsOptions { ThrowOnAnyError = true };

        foreach (var batch in items.Select(i => ToSearchDocument(i.Chunk, i.Vector)).Chunk(UploadBatchSize))
        {
            await _searchClient.IndexDocumentsAsync(
                IndexDocumentsBatch.Upload(batch),
                uploadOptions,
                cancellationToken);
        }
    }

    /// <summary>
    /// Returns the chunks whose embeddings are closest to <paramref name="queryVector"/>.
    /// </summary>
    public async Task<List<DocumentChunk>> SearchAsync(
        float[] queryVector,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        var searchOptions = new SearchOptions
        {
            Size = topK,
            Select =
            {
                SearchIndexSchema.Id,
                SearchIndexSchema.DocumentName,
                SearchIndexSchema.PageNumber,
                SearchIndexSchema.Content
            },
            VectorSearch = new VectorSearchOptions
            {
                Queries =
                {
                    new VectorizedQuery(queryVector)
                    {
                        KNearestNeighborsCount = topK,
                        Fields = { SearchIndexSchema.ContentVector }
                    }
                }
            }
        };

        var response = await _searchClient.SearchAsync<SearchDocument>(searchOptions, cancellationToken);
        var chunks = new List<DocumentChunk>();

        await foreach (var result in response.Value.GetResultsAsync())
        {
            chunks.Add(ToDocumentChunk(result.Document));
        }

        return chunks;
    }

    private static SearchDocument ToSearchDocument(DocumentChunk chunk, float[] vector) => new()
    {
        [SearchIndexSchema.Id] = chunk.Id,
        [SearchIndexSchema.DocumentName] = chunk.DocumentName,
        [SearchIndexSchema.PageNumber] = chunk.PageNumber,
        [SearchIndexSchema.Content] = chunk.Content,
        [SearchIndexSchema.ContentVector] = vector
    };

    private static DocumentChunk ToDocumentChunk(SearchDocument document) => new()
    {
        Id = document[SearchIndexSchema.Id]?.ToString() ?? string.Empty,
        DocumentName = document[SearchIndexSchema.DocumentName]?.ToString() ?? string.Empty,
        PageNumber = Convert.ToInt32(document[SearchIndexSchema.PageNumber]),
        Content = document[SearchIndexSchema.Content]?.ToString() ?? string.Empty
    };
}
