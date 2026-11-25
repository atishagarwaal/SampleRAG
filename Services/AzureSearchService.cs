using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Options;
using SampleRAG.Models;
using SampleRAG.Options;

namespace SampleRAG.Services;

/// <summary>
/// This is a client for the Azure AI Search service. It is used to upload chunks and their embedding vectors to the search index.
/// </summary>
public class AzureSearchService
{
    private readonly SearchClient _searchClient;
    private readonly int _uploadBatchSize;

    /// <summary>
    /// Initializes the Azure AI Search client.
    /// </summary>
    /// <param name="options">The options for the Azure AI Search index.</param>
    public AzureSearchService(IOptions<AzureSearchOptions> options)
    {
        _searchClient = new SearchClient(
           new Uri(options.Value.Endpoint),
           options.Value.IndexName,
           new AzureKeyCredential(options.Value.ApiKey));
        _uploadBatchSize = options.Value.UploadBatchSize;
    }

    /// <summary>
    /// It uploads chunks and their embedding vectors to the search index.
    /// </summary>
    /// <param name="items">The items to upload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task UploadChunksAsync(
        IEnumerable<(DocumentChunk Chunk, float[] Vector)> items,
        CancellationToken cancellationToken = default)
    {
        // Create the upload options.
        var uploadOptions = new IndexDocumentsOptions { ThrowOnAnyError = true };

        // Upload the chunks in batches.
        foreach (var batch in items.Select(i => ToSearchDocument(i.Chunk, i.Vector)).Chunk(_uploadBatchSize))
        {
            // Upload the batch.
            await _searchClient.IndexDocumentsAsync(
                IndexDocumentsBatch.Upload(batch),
                uploadOptions,
                cancellationToken);
        }
    }

    /// <summary>
    /// It searches the search index for chunks whose embeddings are closest to the query's embedding vector.
    /// </summary>
    /// <param name="queryVector">The query's embedding vector.</param>
    /// <param name="topK">The number of nearest neighbors to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<List<DocumentChunk>> SearchAsync(
        float[] queryVector,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        // Create a list to store the chunks.
        var chunks = new List<DocumentChunk>();

        // Create the search options.
        var searchOptions = new SearchOptions
        {
            Size = topK, // The number of nearest neighbors to return.
            Select =
            {
                SearchIndexSchema.Id, // The id of the chunk.
                SearchIndexSchema.DocumentName, // The name of the document.
                SearchIndexSchema.PageNumber, // The page number of the chunk.
                SearchIndexSchema.Content // The content of the chunk.
            },
            VectorSearch = new VectorSearchOptions
            {
                Queries =
                {
                    new VectorizedQuery(queryVector) // The query vector.
                    {
                        KNearestNeighborsCount = topK, // The number of nearest neighbors to return.
                        Fields = { SearchIndexSchema.ContentVector } // The fields to return.
                    }
                }
            }
        };

        // Search the search index.
        var response = await _searchClient.SearchAsync<SearchDocument>(searchOptions, cancellationToken);

        // Add the chunks to the list.
        await foreach (var result in response.Value.GetResultsAsync())
        {
            chunks.Add(ToDocumentChunk(result.Document));
        }

        // Return the chunks.
        return chunks;
    }

    /// <summary>
    /// It converts a chunk and its embedding vector to a search document.
    /// </summary>
    /// <param name="chunk">The chunk to convert.</param>
    /// <param name="vector">The embedding vector of the chunk.</param>
    /// <returns>The search document.</returns>
    private static SearchDocument ToSearchDocument(DocumentChunk chunk, float[] vector) => new()
    {
        [SearchIndexSchema.Id] = chunk.Id,
        [SearchIndexSchema.DocumentName] = chunk.DocumentName,
        [SearchIndexSchema.PageNumber] = chunk.PageNumber,
        [SearchIndexSchema.Content] = chunk.Content,
        [SearchIndexSchema.ContentVector] = vector
    };

    /// <summary>
    /// It converts a search document to a chunk.
    /// </summary>
    /// <param name="document">The search document to convert.</param>
    /// <returns>The chunk.</returns>
    private static DocumentChunk ToDocumentChunk(SearchDocument document) => new()
    {
        Id = document[SearchIndexSchema.Id]?.ToString() ?? string.Empty,
        DocumentName = document[SearchIndexSchema.DocumentName]?.ToString() ?? string.Empty,
        PageNumber = Convert.ToInt32(document[SearchIndexSchema.PageNumber]),
        Content = document[SearchIndexSchema.Content]?.ToString() ?? string.Empty
    };
}
