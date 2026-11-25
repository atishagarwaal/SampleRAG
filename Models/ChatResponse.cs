namespace SampleRAG.Models;

/// <summary>
/// Answer returned by <c>POST /api/chat/ask</c>, with the pages the answer was drawn from.
/// </summary>
public class ChatResponse
{
    public string Answer { get; set; } = string.Empty;

    public List<SourceReference> Sources { get; set; } = [];
}

/// <summary>
/// A document page cited in a <see cref="ChatResponse"/>.
/// </summary>
public record SourceReference(string DocumentName, int PageNumber);
