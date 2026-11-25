using SampleRAG.Models;
using SampleRAG.Options;
using UglyToad.PdfPig;

namespace SampleRAG.Services;

/// <summary>
/// Processes pdf documents and extracts the text.
/// </summary>
public class DocumentService
{
    /// <summary>
    /// Processes a pdf document and extracts the text.
    /// </summary>
    /// <param name="fileStream">The stream of the pdf document.</param>
    /// <param name="fileName">The name of the pdf document.</param>
    /// <param name="options">The document processing options.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A list of <see cref="DocumentChunk"/> objects.</returns>
    public List<DocumentChunk> ProcessPdf(
        Stream fileStream,
        string fileName,
        DocumentsOptions options,
        CancellationToken cancellationToken = default)
    {
        // Create a list to store the chunks.
        var chunks = new List<DocumentChunk>();

        // Open the pdf document.
        using var pdf = PdfDocument.Open(fileStream);

        // Process each page.
        foreach (var page in pdf.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            // If the page text is empty, skip it.
            if (string.IsNullOrWhiteSpace(page.Text))
            {
                continue;
            }

            // Split the page text into chunks.
            foreach (var content in SplitText(page.Text, options.ChunkSize, options.ChunkOverlap))
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

    /// <summary>
    /// Splits the text into chunks.
    /// </summary>
    /// <param name="text">The text to split.</param>
    /// <param name="chunkSize">The size of each chunk.</param>
    /// <param name="overlap">The overlap between chunks.</param>
    /// <returns>An enumerable of strings, each representing a chunk of the text.</returns>
    private static IEnumerable<string> SplitText(string text, int chunkSize, int overlap)
    {
        // Calculate the step size.
        int step = chunkSize - overlap;

        // Loop through the text and yield each chunk.
        for (int start = 0; start < text.Length; start += step)
        {
            // Calculate the end of the chunk.
            int end = Math.Min(start + chunkSize, text.Length);

            // Yield the chunk.
            yield return text[start..end];

            // If the end of the text is reached, break.
            if (end == text.Length)
            {
                yield break;
            }
        }
    }
}
