using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class QdrantService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaService _ollamaService;
    private const string CollectionName = "investing_data";
    private const string QdrantUrl = "http://localhost:6333";

    public QdrantService(HttpClient httpClient, OllamaService ollamaService)
    {
        _httpClient = httpClient;
        _ollamaService = ollamaService;
    }

    public async Task<List<string>> QueryRelevantDataAsync(string question)
    {
        // Convert the question into a vector using Ollama
        var questionVector = await _ollamaService.GetEmbeddingFromOllama(question);

        // Prepare JSON payload
        var requestData = new
        {
            vector = questionVector,
            limit = 3,
            with_payload = true
        };

        var json = JsonSerializer.Serialize(requestData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Send HTTP request to Qdrant
        var response = await _httpClient.PostAsync($"{QdrantUrl}/collections/{CollectionName}/points/search", content);
        response.EnsureSuccessStatusCode(); // Throw exception on failure
       
        var jsonResponse = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonResponse);

        // Extract relevant text from search results
        return doc.RootElement.GetProperty("result")
            .EnumerateArray()
            .Select(point => point.GetProperty("payload").GetProperty("text").GetString())
            .Where(text => !string.IsNullOrEmpty(text))
            .ToList();
    }
}
