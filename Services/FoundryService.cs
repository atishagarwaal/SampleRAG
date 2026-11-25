using System.ClientModel.Primitives;
using Azure.Identity;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using SampleRAG.Options;

namespace SampleRAG.Services;

/// <summary>
/// Provides authenticated OpenAI-compatible chat and embedding clients backed by Azure AI Foundry.
/// </summary>
public class FoundryService
{
    private const string AzureAiScope = "https://cognitiveservices.azure.com/.default";

    /// <summary>
    /// Client for generating chat completions.
    /// </summary>
    public ChatClient Chat { get; }

    /// <summary>
    /// Client for generating text embeddings.
    /// </summary>
    public EmbeddingClient Embeddings { get; }

    /// <summary>
    /// Initializes the Foundry clients using Microsoft Entra ID authentication.
    /// </summary>
    public FoundryService(IOptions<FoundryOptions> options)
    {
        var foundry = options.Value;

        // Authenticate with Microsoft Entra ID using the default Azure credential chain
        var credentialOptions = new DefaultAzureCredentialOptions();
        if (!string.IsNullOrWhiteSpace(foundry.TenantId))
        {
            credentialOptions.TenantId = foundry.TenantId;
        }

        var credential = new DefaultAzureCredential(credentialOptions);
        var tokenPolicy = new BearerTokenPolicy(credential, AzureAiScope);

        // Create the Foundry OpenAI-compatible root client
#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var openAIClient = new OpenAIClient(
            authenticationPolicy: tokenPolicy,
            options: new OpenAIClientOptions
            {
                Endpoint = new Uri(foundry.Endpoint)
            });
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        Chat = openAIClient.GetChatClient(foundry.ChatDeployment);
        Embeddings = openAIClient.GetEmbeddingClient(foundry.EmbeddingDeployment);
    }
}
