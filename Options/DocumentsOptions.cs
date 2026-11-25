using System.ComponentModel.DataAnnotations;

namespace SampleRAG.Options;

/// <summary>
/// Configuration settings for document processing.
/// </summary>
public class DocumentsOptions
{
    public const string SectionName = "Documents";

    public string FolderPath { get; set; } = "Documents";

    [Range(1, int.MaxValue)]
    public int ChunkSize { get; set; } = 1000;

    [Range(0, int.MaxValue)]
    public int ChunkOverlap { get; set; } = 200;
}
