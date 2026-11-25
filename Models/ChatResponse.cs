namespace SampleRAG.Models;

/// <summary>
/// Answer returned by <c>POST /api/chat/ask</c>, with the pages the answer was drawn from.
/// </summary>
public class ChatResponse
{
    /// <summary>
    /// Grounded answer, or a short message when nothing relevant was found.
    /// </summary>
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// Distinct document pages that supplied the context. Empty when no chunks matched.
    /// </summary>
    public List<SourceReference> Sources { get; set; } = [];
}

/// <summary>
/// A document page cited in a <see cref="ChatResponse"/>.
/// </summary>
public class SourceReference
{
    public string DocumentName { get; set; } = string.Empty;

    /// <summary>
    /// 1-based page number in <see cref="DocumentName"/>.
    /// </summary>
    public int PageNumber { get; set; }
}
