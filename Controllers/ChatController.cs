using Microsoft.AspNetCore.Mvc;
using SampleRAG.Models;
using SampleRAG.Services;

namespace SampleRAG.Controllers;

/// <summary>
/// HTTP entry point for questions answered from the indexed documents.
/// </summary>
[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly RAGService _ragService;

    public ChatController(RAGService ragService)
    {
        _ragService = ragService;
    }

    /// <summary>
    /// Answers <see cref="ChatRequest.Question"/> and returns the text plus the pages it came from.
    /// </summary>
    [HttpPost("ask")]
    public async Task<IActionResult> Ask(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = await _ragService.AskAsync(
            request.Question,
            cancellationToken);

        return Ok(response);
    }
}
