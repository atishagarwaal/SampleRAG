namespace SampleRAG.Services;

/// <summary>
/// Turns text into vectors using the Foundry embedding deployment.
/// </summary>
public class EmbeddingService(FoundryService foundryService)
{
    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var response = await foundryService.Embeddings.GenerateEmbeddingAsync(
            text,
            cancellationToken: cancellationToken);

        return response.Value.ToFloats().ToArray();
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        var response = await foundryService.Embeddings.GenerateEmbeddingsAsync(
            texts,
            cancellationToken: cancellationToken);

        return response.Value.Select(e => e.ToFloats().ToArray()).ToList();
    }
}
