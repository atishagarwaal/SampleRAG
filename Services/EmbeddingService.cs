using OpenAI.Embeddings;

namespace SampleRAG.Services;

public class EmbeddingService
{
    private readonly EmbeddingClient _embeddingClient;

    public EmbeddingService(EmbeddingClient embeddingClient)
    {
        _embeddingClient = embeddingClient;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var response = await _embeddingClient.GenerateEmbeddingAsync(text);

        return response.Value.ToFloats().ToArray();
    }
}