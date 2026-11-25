using SampleRAG.Models;
using UglyToad.PdfPig;

namespace SampleRAG.Services;

/// <summary>
/// Handles PDF text extraction and overlapping character-window chunking.
/// </summary>
public class DocumentService
{
    private const int DefaultChunkSize = 1000;
    private const int DefaultChunkOverlap = 200;

    public List<DocumentChunk> ProcessPdf(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var chunks = new List<DocumentChunk>();
        using var pdf = PdfDocument.Open(fileStream);

        foreach (var page in pdf.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(page.Text))
                continue;

            foreach (var content in SplitText(page.Text, DefaultChunkSize, DefaultChunkOverlap))
            {
                chunks.Add(new DocumentChunk
                {
                    DocumentName = fileName,
                    PageNumber = page.Number,
                    Content = content
                });
            }
        }

        return chunks;
    }

    private static IEnumerable<string> SplitText(string text, int chunkSize, int overlap)
    {
        int stepSize = chunkSize - overlap;

        for (int start = 0; start < text.Length; start += stepSize)
        {
            int length = Math.Min(chunkSize, text.Length - start);
            yield return text.Substring(start, length);

            if (start + length >= text.Length)
                yield break;
        }
    }
}
