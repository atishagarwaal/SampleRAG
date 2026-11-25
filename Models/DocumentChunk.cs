namespace SampleRAG.Models;

/// <summary>
/// Represents a single text chunk extracted from an uploaded document.
/// </summary>
public class DocumentChunk
{
    /// <summary>
    /// Unique identifier for this chunk.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Name of the source document this chunk was extracted from.
    /// </summary>
    public string DocumentName { get; set; } = string.Empty;

    /// <summary>
    /// 1-based page number in the source document where this chunk originated.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Text content of the chunk.
    /// </summary>
    public string Content { get; set; } = string.Empty;
}