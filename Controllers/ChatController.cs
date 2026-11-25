using Microsoft.AspNetCore.Mvc;
using SampleRAG.Models;
using SampleRAG.Services;

namespace SampleRAG.Controllers;

/// <summary>
/// User-facing controller for asking questions and getting answers from the indexed documents.
/// </summary>
[ApiController]
[Route("api/chat")]
public class ChatController(RAGService ragService) : ControllerBase
{
    /// <summary>
    /// Answers question and returns the text plus the sources it came from.
    /// </summary>
    [HttpPost("ask")]
    public async Task<ActionResult<ChatResponse>> Ask(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        var response = await ragService.AskAsync(request.Question, cancellationToken);
        return Ok(response);
    }
}
