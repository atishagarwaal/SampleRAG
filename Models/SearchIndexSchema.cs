namespace SampleRAG.Models;

/// <summary>
/// This is the schema for the search index.
/// </summary>
internal static class SearchIndexSchema
{
    public const string Id = "id";
    public const string DocumentName = "documentName";
    public const string PageNumber = "pageNumber";
    public const string Content = "content";
    public const string ContentVector = "contentVector";
    public const string VectorProfile = "vector-profile";
    public const string HnswAlgorithm = "hnsw-config";
    public const int EmbeddingDimensions = 1536;
}
