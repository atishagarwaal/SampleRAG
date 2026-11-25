using System.ComponentModel.DataAnnotations;

namespace SampleRAG.Models;

/// <summary>
/// Body of <c>POST /api/chat/ask</c>.
/// </summary>
public class ChatRequest
{
    /// <summary>
    /// Question to answer from the indexed documents.
    /// </summary>
    [Required]
    [MinLength(3)]
    public string Question { get; set; } = string.Empty;
}
