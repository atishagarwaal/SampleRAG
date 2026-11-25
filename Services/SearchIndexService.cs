using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Extensions.Options;
using SampleRAG.Models;
using SampleRAG.Options;

namespace SampleRAG.Services;

/// <summary>
/// Creates the Azure AI Search index that stores document chunks and their embeddings.
/// </summary>
public class SearchIndexService
{
    private readonly SearchIndexClient _indexClient;
    private readonly string _indexName;

    /// <summary>
    /// Initializes the search index client and the index name.
    /// </summary>
    /// <param name="options">The options for the Azure AI Search index.</param>
    public SearchIndexService(IOptions<AzureSearchOptions> options)
    {
        // Get the search options from the configuration.
        var searchIndexOptions = options.Value;

        // Create the search index client.
        _indexClient = new SearchIndexClient(
            new Uri(searchIndexOptions.Endpoint),
            new AzureKeyCredential(searchIndexOptions.ApiKey));

        // Set the index name.
        _indexName = searchIndexOptions.IndexName;
    }

    /// <summary>
    /// Creates the vector index when it is missing. Existing indexes are left as-is.
    /// </summary>
    public async Task CreateIndexIfNotExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if the index exists.
            await _indexClient.GetIndexAsync(_indexName, cancellationToken);
            return;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Index does not exist yet.
        }

        // Create the index.
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
