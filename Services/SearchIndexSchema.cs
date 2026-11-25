namespace SampleRAG.Services;

/// <summary>
/// Field names and vector settings that index creation and search must use together.
/// <see cref="EmbeddingDimensions"/> has to match the embedding deployment:
/// text-embedding-3-small returns 1536 values.
/// </summary>
internal static class SearchIndexSchema
{
    public const string Id = "id";
    public const string DocumentName = "documentName";
    public const string PageNumber = "pageNumber";
    public const string Content = "content";
    public const string ContentVector = "contentVector";

    /// <summary>
    /// Name of the profile that <see cref="ContentVector"/> points at.
    /// The profile selects which algorithm searches that field.
    /// </summary>
    public const string VectorProfile = "vector-profile";

    /// <summary>
    /// HNSW configuration used for approximate nearest-neighbor search.
    /// </summary>
    public const string HnswAlgorithm = "hnsw-config";

    public const int EmbeddingDimensions = 1536;
}
