using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using SampleRAG.Models;

namespace SampleRAG.Services;

/// <summary>
/// Uploads document chunks to Azure AI Search and retrieves the nearest chunks for a query vector.
/// </summary>
public class VectorSearchService
{
    /// <summary>
    /// Stay under the service limit of 1000 documents per indexing request.
    /// </summary>
    private const int UploadBatchSize = 500;

    private readonly SearchClient _searchClient;

    public VectorSearchService(IConfiguration configuration)
    {
        var endpoint = configuration["AzureSearch:Endpoint"]
            ?? throw new InvalidOperationException(
                "Azure Search endpoint is missing.");

        var apiKey = configuration["AzureSearch:ApiKey"]
            ?? throw new InvalidOperationException(
                "Azure Search API key is missing.");

        var indexName = configuration["AzureSearch:IndexName"]
            ?? throw new InvalidOperationException(
                "Azure Search index name is missing.");

        _searchClient = new SearchClient(
            new Uri(endpoint),
            indexName,
            new AzureKeyCredential(apiKey));
    }

    /// <summary>
    /// Uploads chunks and their embedding vectors, splitting the work into fixed-size batches.
    /// </summary>
    public async Task UploadChunksAsync(
        IEnumerable<(DocumentChunk Chunk, float[] Vector)> items,
        CancellationToken cancellationToken = default)
    {
        var batch = new List<SearchDocument>(UploadBatchSize);

        foreach (var item in items)
        {
            batch.Add(ToSearchDocument(item.Chunk, item.Vector));

            if (batch.Count >= UploadBatchSize)
            {
                await UploadBatchAsync(batch, cancellationToken);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await UploadBatchAsync(batch, cancellationToken);
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
        var options = new SearchOptions
        {
            // Cap the payload at the same count the vector query asks for.
            Size = topK
        };

        options.Select.Add(SearchIndexSchema.Id);
        options.Select.Add(SearchIndexSchema.DocumentName);
        options.Select.Add(SearchIndexSchema.PageNumber);
        options.Select.Add(SearchIndexSchema.Content);

        options.VectorSearch = new VectorSearchOptions
        {
            Queries =
            {
                new VectorizedQuery(new ReadOnlyMemory<float>(queryVector))
                {
                    KNearestNeighborsCount = topK,
                    Fields = { SearchIndexSchema.ContentVector }
                }
            }
        };

        var response = await _searchClient.SearchAsync<SearchDocument>(
            options,
            cancellationToken);

        var chunks = new List<DocumentChunk>();

        await foreach (var result in response.Value.GetResultsAsync())
        {
            chunks.Add(ToDocumentChunk(result.Document));
        }

        return chunks;
    }

    private static SearchDocument ToSearchDocument(DocumentChunk chunk, float[] vector)
    {
        return new SearchDocument
        {
            [SearchIndexSchema.Id] = chunk.Id,
            [SearchIndexSchema.DocumentName] = chunk.DocumentName,
            [SearchIndexSchema.PageNumber] = chunk.PageNumber,
            [SearchIndexSchema.Content] = chunk.Content,
            [SearchIndexSchema.ContentVector] = vector,
        };
    }

    private static DocumentChunk ToDocumentChunk(SearchDocument document)
    {
        return new DocumentChunk
        {
            Id = document[SearchIndexSchema.Id]?.ToString() ?? string.Empty,
            DocumentName = document[SearchIndexSchema.DocumentName]?.ToString() ?? string.Empty,
            // Numeric index fields may be returned as Int32 or Int64.
            PageNumber = Convert.ToInt32(document[SearchIndexSchema.PageNumber]),
            Content = document[SearchIndexSchema.Content]?.ToString() ?? string.Empty
        };
    }

    private async Task UploadBatchAsync(
        List<SearchDocument> documents,
        CancellationToken cancellationToken)
    {
        // Fail the batch when any document is rejected instead of reporting a partial success.
        var options = new IndexDocumentsOptions
        {
            ThrowOnAnyError = true
        };

        await _searchClient.IndexDocumentsAsync(
            IndexDocumentsBatch.Upload(documents),
            options,
            cancellationToken);
    }
}
