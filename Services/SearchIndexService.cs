using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace SampleRAG.Services;

/// <summary>
/// Creates the Azure AI Search index that stores document chunks and their embeddings.
/// </summary>
public class SearchIndexService
{
    private readonly SearchIndexClient _indexClient;
    private readonly string _indexName;

    public SearchIndexService(IConfiguration configuration)
    {
        var endpoint = configuration["AzureSearch:Endpoint"]
            ?? throw new InvalidOperationException(
                "Azure Search endpoint is missing.");

        var apiKey = configuration["AzureSearch:ApiKey"]
            ?? throw new InvalidOperationException(
                "Azure Search API key is missing.");

        _indexName = configuration["AzureSearch:IndexName"]
            ?? throw new InvalidOperationException(
                "Azure Search index name is missing.");

        _indexClient = new SearchIndexClient(
            new Uri(endpoint),
            new AzureKeyCredential(apiKey));
    }

    /// <summary>
    /// Creates the vector index when it is missing.
    /// An index that already exists is left as-is, including its fields.
    /// </summary>
    public async Task CreateIndexIfNotExistsAsync()
    {
        try
        {
            await _indexClient.GetIndexAsync(_indexName);
            return;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // 404 means the index has not been created yet.
        }

        var index = new SearchIndex(_indexName)
        {
            Fields =
            {
                new SimpleField(SearchIndexSchema.Id, SearchFieldDataType.String)
                {
                    IsKey = true,
                    IsFilterable = true
                },

                new SearchableField(SearchIndexSchema.DocumentName)
                {
                    IsFilterable = true
                },

                new SimpleField(
                    SearchIndexSchema.PageNumber,
                    SearchFieldDataType.Int32)
                {
                    IsFilterable = true
                },

                new SearchableField(SearchIndexSchema.Content),

                new SearchField(
                    SearchIndexSchema.ContentVector,
                    SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = SearchIndexSchema.EmbeddingDimensions,
                    VectorSearchProfileName = SearchIndexSchema.VectorProfile
                }
            },

            // The vector field names the profile; the profile names the algorithm.
            VectorSearch = new VectorSearch
            {
                Algorithms =
                {
                    new HnswAlgorithmConfiguration(SearchIndexSchema.HnswAlgorithm)
                },

                Profiles =
                {
                    new VectorSearchProfile(
                        SearchIndexSchema.VectorProfile,
                        SearchIndexSchema.HnswAlgorithm)
                }
            }
        };

        await _indexClient.CreateIndexAsync(index);
    }
}
