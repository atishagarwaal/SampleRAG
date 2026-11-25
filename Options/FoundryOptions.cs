using System.ComponentModel.DataAnnotations;

namespace SampleRAG.Options;

/// <summary>
/// Configuration settings for connecting to Azure AI Foundry.
/// Bound from the <see cref="SectionName"/> section of application configuration.
/// </summary>
public class FoundryOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Foundry";

    /// <summary>
    /// Base endpoint URL of the Azure AI Foundry OpenAI-compatible resource.
    /// </summary>
    [Required]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Deployment name of the chat completion model.
    /// </summary>
    [Required]
    public string ChatDeployment { get; set; } = string.Empty;

    /// <summary>
    /// Deployment name of the text embedding model.
    /// </summary>
    [Required]
    public string EmbeddingDeployment { get; set; } = string.Empty;

    /// <summary>
    /// Optional Microsoft Entra ID tenant ID to use for authentication.
    /// </summary>
    public string? TenantId { get; set; }
}
