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


    public async Task<string> GetDailyInvestmentAdvice()
    {
        string prompt = @"
You are an AI Investment Advisor providing daily stock market insights. Your response will be displayed directly in a widget, so you MUST follow this exact format:

TITLE: [Compelling investment insight title - under 40 characters]
CONTENT: [2-3 sentences analyzing current market trends and giving specific actionable advice]
POINTS:
- success: [One positive market opportunity - one sentence]
- warning: [One specific risk to monitor - one sentence]
- info: [One concrete actionable recommendation - one sentence]

Example output:
TITLE: Tech Sector Momentum Rising
CONTENT: Market indicators suggest a potential bullish trend in tech stocks. Consider increasing positions in AAPL and MSFT while monitoring inflation data expected tomorrow. Your portfolio shows strong diversification but might benefit from increased exposure to renewable energy sector.
POINTS:
- success: Tech sector showing strong momentum with AI leaders outperforming
- warning: Watch inflation data release (Mar 15) for potential market volatility
- info: Consider adding renewable energy stocks for better sector balance

IMPORTANT RULES:
1. If you're uncertain about market conditions, create plausible advice based on current investment best practices.
2. ALWAYS maintain the exact format with TITLE:, CONTENT:, and POINTS: keywords.
3. ALWAYS include exactly three points with the labels success:, warning:, and info:.
4. Provide ONLY the formatted output with no additional text before or after.

Response correctly formatted? Double-check before submitting:
✓ Has TITLE:, CONTENT:, and POINTS: keywords
✓ Has exactly three points with success:, warning:, and info: labels
✓ Contains only the required format with no other text.";

        var requestData = new
        {
            model = "gemma:2b", // ניתן לשנות בהתאם למודל הזמין
            prompt = prompt,
            stream = false
        };

        var json = JsonSerializer.Serialize(requestData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("http://localhost:11434/api/generate", content);
        var jsonResponse = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(jsonResponse);
        return doc.RootElement.GetProperty("response").GetString();
    }

}
