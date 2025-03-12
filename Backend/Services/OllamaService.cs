using Azure;
using Azure.Core;
using Microsoft.VisualBasic;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class OllamaService
{
    private readonly HttpClient _httpClient;
    private const string OllamaEmbeddingUrl = "http://localhost:11434/api/embeddings";
    private const string OllamaGenerateUrl = "http://localhost:11434/api/generate";

    public OllamaService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
    }

    public async Task<List<float>> GetEmbeddingFromOllama(string text)
    {
        var requestData = new { model = "nomic-embed-text", prompt = text };
        var json = JsonSerializer.Serialize(requestData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(OllamaEmbeddingUrl, content);
        var jsonResponse = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(jsonResponse);
        return doc.RootElement.GetProperty("embedding")
            .EnumerateArray()
            .Select(e => e.GetSingle())
            .ToList();
    }

    public async Task<string> QueryOllama(string context, string question)
    {
        // Building the refined prompt
        string prompt = $@"
You are an AI assistant answering based **only** on the provided context.

**Context:**
{context}

**User Question:**
{question}

**Instructions:**
- **Your answer MUST contain at least one direct quote** from the context.
- **Format your quote exactly like this:**  
  'According to the text: ""...""'.
- If the context **does not provide enough information**, respond with:
  'The context does not provide enough information to answer this question.'
- **DO NOT** use external knowledge beyond the given context.
- **DO NOT** paraphrase the quotes—use the exact wording from the context, you must quote at least one phrase from the context.

**Example of a correct response:**
'According to the text: ""Investors should...""'.

Now answer the question strictly following these instructions.
";


        var requestData = new
        {
            model = "gemma:2b", // Change to your Ollama model
            prompt = prompt,
            stream = false
        };

        var json = JsonSerializer.Serialize(requestData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(OllamaGenerateUrl, content);
        var jsonResponse = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(jsonResponse);

        // Extracting the response from the JSON
        return doc.RootElement.GetProperty("response").GetString();
    }
}
