using System.ComponentModel.DataAnnotations;

namespace SampleRAG.Options;

/// <summary>
/// Configuration settings for connecting to Azure AI Search.
/// </summary>
public class AzureSearchOptions
{
    public const string SectionName = "AzureSearch";

    [Required]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    [Required]
    public string IndexName { get; set; } = string.Empty;

    /// <summary>
    /// Number of documents sent to Azure AI Search per indexing request.
    /// Azure AI Search accepts at most 1,000 documents per batch.
    /// </summary>
    [Range(1, 1000)]
    public int UploadBatchSize { get; set; } = 500;
}
