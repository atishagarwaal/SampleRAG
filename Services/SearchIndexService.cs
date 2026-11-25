using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Extensions.Options;
using SampleRAG.Options;

namespace SampleRAG.Services;

/// <summary>
/// Creates the Azure AI Search index that stores document chunks and their embeddings.
/// </summary>
public class SearchIndexService(IOptions<AzureSearchOptions> options)
{
    private readonly SearchIndexClient _indexClient = new(
        new Uri(options.Value.Endpoint),
        new AzureKeyCredential(options.Value.ApiKey));

    private readonly string _indexName = options.Value.IndexName;

    /// <summary>
    /// Creates the vector index when it is missing. Existing indexes are left as-is.
    /// </summary>
    public async Task CreateIndexIfNotExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _indexClient.GetIndexAsync(_indexName, cancellationToken);
            return;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Index does not exist yet.
        }

        var index = new SearchIndex(_indexName)
        {
            Fields =
            {
                new SimpleField(SearchIndexSchema.Id, SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
                new SearchableField(SearchIndexSchema.DocumentName) { IsFilterable = true },
                new SimpleField(SearchIndexSchema.PageNumber, SearchFieldDataType.Int32) { IsFilterable = true },
                new SearchableField(SearchIndexSchema.Content),
                new SearchField(SearchIndexSchema.ContentVector, SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = SearchIndexSchema.EmbeddingDimensions,
                    VectorSearchProfileName = SearchIndexSchema.VectorProfile
                }
            },
            VectorSearch = new VectorSearch
            {
                Algorithms = { new HnswAlgorithmConfiguration(SearchIndexSchema.HnswAlgorithm) },
                Profiles = { new VectorSearchProfile(SearchIndexSchema.VectorProfile, SearchIndexSchema.HnswAlgorithm) }
            }
        };

        await _indexClient.CreateIndexAsync(index, cancellationToken);
    }
}
