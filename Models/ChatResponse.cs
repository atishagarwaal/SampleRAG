namespace SampleRAG.Models;

public class ChatResponse
{
    public string Answer { get; set; } = string.Empty;

    public List<SourceReference> Sources { get; set; } = [];
}

public class SourceReference
{
    public string DocumentName { get; set; } = string.Empty;

    public int PageNumber { get; set; }
}