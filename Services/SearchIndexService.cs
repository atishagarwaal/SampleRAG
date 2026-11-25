using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace SampleRAG.Services;

public class SearchIndexService
{
    private readonly SearchIndexClient _indexClient;
    private readonly IConfiguration _configuration;

    public SearchIndexService(IConfiguration configuration)
    {
        _configuration = configuration;

        var endpoint = configuration["AzureSearch:Endpoint"]!;
        var apiKey = configuration["AzureSearch:ApiKey"]!;

        _indexClient = new SearchIndexClient(
            new Uri(endpoint),
            new AzureKeyCredential(apiKey));
    }

    public async Task CreateIndexIfNotExistsAsync()
    {
        var indexName =
            _configuration["AzureSearch:IndexName"]!;

        try
        {
            await _indexClient.GetIndexAsync(indexName);

            return; // Index already exists
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Index does not exist. Create it.
        }

        var index = new SearchIndex(indexName)
        {
            Fields =
            {
                new SimpleField("id", SearchFieldDataType.String)
                {
                    IsKey = true,
                    IsFilterable = true
                },

                new SearchableField("documentName")
                {
                    IsFilterable = true
                },

                new SimpleField(
                    "pageNumber",
                    SearchFieldDataType.Int32)
                {
                    IsFilterable = true
                },

                new SearchableField("content"),

                new SearchField(
                    "contentVector",
                    SearchFieldDataType.Collection(
                        SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = 1536,
                    VectorSearchProfileName = "vector-profile"
                }
            },

            VectorSearch = new VectorSearch
            {
                Algorithms =
                {
                    new HnswAlgorithmConfiguration("hnsw-config")
                },

                Profiles =
                {
                    new VectorSearchProfile(
                        "vector-profile",
                        "hnsw-config")
                }
            }
        };

        await _indexClient.CreateIndexAsync(index);
    }
}