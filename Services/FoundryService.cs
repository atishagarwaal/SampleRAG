using System.ClientModel.Primitives;
using Azure.Identity;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using SampleRAG.Options;

namespace SampleRAG.Services;

/// <summary>
/// This is a client for the Foundry service.
/// </summary>
public class FoundryService
{
    private const string AzureAiScope = "https://cognitiveservices.azure.com/.default";

    /// <summary>
    /// Client for asking questions to the chat model.
    /// </summary>
    public ChatClient Chat { get; }

    /// <summary>
    /// Client for creating embeddings using the embedding model.
    /// </summary>
    public EmbeddingClient Embeddings { get; }

    /// <summary>
    /// Initializes the Foundry clients using Microsoft Entra ID authentication.
    /// </summary>
    public FoundryService(IOptions<FoundryOptions> options)
    {
        // Get the Foundry endpoint and credentials from the configuration.
        var foundry = options.Value;

        // Authenticate with Microsoft Entra ID using the default Azure credential chain
        var credentialOptions = new DefaultAzureCredentialOptions();
        if (!string.IsNullOrWhiteSpace(foundry.TenantId))
        {
            credentialOptions.TenantId = foundry.TenantId;
        }

        var credential = new DefaultAzureCredential(credentialOptions);
        var tokenPolicy = new BearerTokenPolicy(credential, AzureAiScope);

        // Create the OpenAI client.
#pragma warning disable OPENAI001
        var openAIClient = new OpenAIClient(
            authenticationPolicy: tokenPolicy,
            options: new OpenAIClientOptions
            {
                Endpoint = new Uri(foundry.Endpoint)
            });
#pragma warning restore OPENAI001

        // Create the chat and embedding clients.
        Chat = openAIClient.GetChatClient(foundry.ChatDeployment);
        Embeddings = openAIClient.GetEmbeddingClient(foundry.EmbeddingDeployment);
    }
}
