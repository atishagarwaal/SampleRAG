using OpenAI.Embeddings;

namespace SampleRAG.Services;

/// <summary>
/// Turns text into a vector using the Foundry embedding deployment.
/// The same client is used for document chunks and for user questions.
/// </summary>
public class EmbeddingService
{
    private readonly EmbeddingClient _embeddingClient;

    public EmbeddingService(EmbeddingClient embeddingClient)
    {
        _embeddingClient = embeddingClient;
    }

    /// <summary>
    /// Returns the embedding as a float array that Azure AI Search can store and query.
    /// </summary>
    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var response = await _embeddingClient.GenerateEmbeddingAsync(text);

        // The client exposes the vector as read-only memory. Copy it so callers can keep the values.
        return response.Value.ToFloats().ToArray();
    }
}
