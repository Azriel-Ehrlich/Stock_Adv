using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/rag")]
public class RagController : ControllerBase
{
    private readonly QdrantService _qdrantService;
    private readonly OllamaService _ollamaService;

    public RagController(QdrantService qdrantService, OllamaService ollamaService)
    {
        _qdrantService = qdrantService;
        _ollamaService = ollamaService;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] QuestionRequest request)
    {
        // Retrieve relevant context from Qdrant
        var contextTexts = await _qdrantService.QueryRelevantDataAsync(request.Question);

        // If no relevant context was found, return a predefined response
        if (contextTexts == null || contextTexts.Count == 0)
        {
            return Ok(new { Answer = "The context does not provide enough information." });
        }

        // Build the refined prompt
        string context = string.Join("\n\n", contextTexts);
        Console.WriteLine(context);
        string response = await _ollamaService.QueryOllama(context, request.Question);

        return Ok(new { Answer = response });
    }
}

// Model for request body
public class QuestionRequest
{
    public string Question { get; set; }
}
