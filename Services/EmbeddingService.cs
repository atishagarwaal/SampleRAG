namespace SampleRAG.Services;

/// <summary>
/// Turns text into vectors using the Foundry embedding model.
/// </summary>
public class EmbeddingService(FoundryService foundryService)
{
    /// <summary>
    /// Generates an embedding for the given text.
    /// </summary>
    /// <param name="text">The text to generate an embedding for.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A <see cref="float[]"/> containing the embedding.</returns>
    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        // Generate an embedding for the given text.
        var response = await foundryService.Embeddings.GenerateEmbeddingAsync(
            text,
            cancellationToken: cancellationToken);

        // Return the embedding.
        return response.Value.ToFloats().ToArray();
    }

    /// <summary>
    /// Generates embeddings for the given texts.
    /// </summary>
    /// <param name="texts">The texts to generate embeddings for.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A list of <see cref="float[]"/> containing the embeddings.</returns>
    public async Task<List<float[]>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        // Generate embeddings for the given texts.
        var response = await foundryService.Embeddings.GenerateEmbeddingsAsync(
            texts,
            cancellationToken: cancellationToken);

        // Return the embeddings.
        return response.Value.Select(e => e.ToFloats().ToArray()).ToList();
    }
}
