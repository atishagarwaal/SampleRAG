
using Microsoft.AspNetCore.Mvc;
using SampleRAG.Models;
using SampleRAG.Services;

namespace SampleRAG.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly RAGService _ragService;

    public ChatController(RAGService ragService)
    {
        _ragService = ragService;
    }

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
