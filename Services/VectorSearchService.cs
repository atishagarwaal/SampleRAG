
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using SampleRAG.Models;

namespace SampleRAG.Services;

public class VectorSearchService
{
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

    // Upload document chunks and their embedding vectors.
    public async Task UploadChunksAsync(
        IEnumerable<(DocumentChunk Chunk, float[] Vector)> items,
        CancellationToken cancellationToken = default)
    {
        const int batchSize = 500;

        var batch = new List<SearchDocument>(batchSize);

        foreach (var item in items)
        {
            var document = new SearchDocument
            {
                ["id"] = item.Chunk.Id,
                ["documentName"] = item.Chunk.DocumentName,
                ["pageNumber"] = item.Chunk.PageNumber,
                ["content"] = item.Chunk.Content,
                ["contentVector"] = item.Vector,
            };

            batch.Add(document);

            if (batch.Count >= batchSize)
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

    private async Task UploadBatchAsync(
        List<SearchDocument> documents,
        CancellationToken cancellationToken)
    {
        var options = new IndexDocumentsOptions
        {
            ThrowOnAnyError = true
        };

        var response = await _searchClient.IndexDocumentsAsync(
            IndexDocumentsBatch.Upload(documents),
            options,
            cancellationToken);

        // Ensure the indexing operation completed successfully.
        _ = response.Value;
    }

    // Retrieve the most relevant chunks using vector similarity.
    public async Task<List<DocumentChunk>> SearchAsync(
        float[] queryVector,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        var options = new SearchOptions
        {
            Size = topK
        };

        options.Select.Add("id");
        options.Select.Add("documentName");
        options.Select.Add("pageNumber");
        options.Select.Add("content");

        options.VectorSearch = new VectorSearchOptions
        {
            Queries =
            {
                new VectorizedQuery(
                    new ReadOnlyMemory<float>(queryVector))
                {
                    KNearestNeighborsCount = topK,
                    Fields = { "contentVector" }
                }
            }
        };

        var response = await _searchClient.SearchAsync<SearchDocument>(
            options,
            cancellationToken);

        var chunks = new List<DocumentChunk>();

        await foreach (var result in response.Value.GetResultsAsync())
        {
            var document = result.Document;

            chunks.Add(new DocumentChunk
            {
                Id = document["id"]?.ToString() ?? string.Empty,
                DocumentName =
                    document["documentName"]?.ToString() ?? string.Empty,
                PageNumber = Convert.ToInt32(document["pageNumber"]),
                Content = document["content"]?.ToString() ?? string.Empty
            });
        }

        return chunks;
    }
}
