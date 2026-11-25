using SampleRAG.Models;
using UglyToad.PdfPig;

namespace SampleRAG.Services;

/// <summary>
/// Handles document ingestion, including PDF text extraction and chunking.
/// </summary>
public class DocumentService
{
    private const int DefaultChunkSize = 1000;
    private const int DefaultChunkOverlap = 200;

    /// <summary>
    /// Extracts text from each page of a PDF stream and splits it into overlapping chunks.
    /// </summary>
    /// <param name="fileStream">Readable stream containing the PDF file.</param>
    /// <param name="fileName">Name of the source PDF file, stored as metadata on each chunk.</param>
    /// <param name="cancellationToken">Token used to cancel processing between pages.</param>
    /// <returns>A list of <see cref="DocumentChunk"/> items extracted from the PDF.</returns>
    public Task<List<DocumentChunk>> ProcessPdfAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var chunks = new List<DocumentChunk>();

        using var pdf = PdfDocument.Open(fileStream);

        foreach (var page in pdf.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pageText = page.Text;

            // Skip blank or image-only pages that contain no extractable text
            if (string.IsNullOrWhiteSpace(pageText))
                continue;

            var pageChunks = SplitText(pageText, DefaultChunkSize, DefaultChunkOverlap);

            foreach (var content in pageChunks)
            {
                chunks.Add(new DocumentChunk
                {
                    DocumentName = fileName,
                    PageNumber = page.Number,
                    Content = content
                });
            }
        }

        // PdfPig reads the stream synchronously. Wrap the result so callers can stay async.
        return Task.FromResult(chunks);
    }

    /// <summary>
    /// Splits text into fixed-size chunks using a sliding window with character overlap
    /// so context is preserved across chunk boundaries.
    /// </summary>
    private static List<string> SplitText(string text, int chunkSize, int overlap)
    {
        var chunks = new List<string>();

        // Step is smaller than the window so the next chunk repeats the tail of this one.
        int stepSize = chunkSize - overlap;
        int start = 0;

        while (start < text.Length)
        {
            int length = Math.Min(chunkSize, text.Length - start);
            chunks.Add(text.Substring(start, length));

            // Stop once the current chunk reaches the end of the text
            if (start + length >= text.Length)
                break;

            start += stepSize;
        }

        return chunks;
    }
}