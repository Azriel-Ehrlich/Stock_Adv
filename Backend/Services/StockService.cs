using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using YahooFinanceApi;

namespace Backend.Services
{
    public class StockService
    {

        private readonly HttpClient _httpClient;
        private const string YahooSearchUrl = "https://query2.finance.yahoo.com/v1/finance/search?q=";

        public StockService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        //Search stock symbol by name (e.g., "Apple" -> "AAPL")
        public async Task<string?> SearchStockSymbolAsync(string query)
        {
            // add user agent to avoid errors
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

            var response = await _httpClient.GetAsync($"{YahooSearchUrl}{query}&quotesCount=1&newsCount=0");

            if (!response.IsSuccessStatusCode)
            {
              
                throw new Exception("Failed to fetch stock data from Yahoo Finance.");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);

            if (doc.RootElement.TryGetProperty("quotes", out var quotes) && quotes.GetArrayLength() > 0)
            {
                var firstResult = quotes[0];
                return firstResult.TryGetProperty("symbol", out var symbol) ? symbol.GetString() : null;
            }

            return null;
        }



        //Get stock prices for a list of tickers
        public async Task<Dictionary<string, decimal>> GetStockPricesAsync(List<string> tickers)
        {
            var stockPrices = new Dictionary<string, decimal>();

            try
            {
                // Fetch stock data for the requested tickers
                var securities = await Yahoo.Symbols(tickers.ToArray())
                                           .Fields(Field.RegularMarketPrice)
                                           .QueryAsync();

                foreach (var ticker in tickers)
                {
                    if (securities.ContainsKey(ticker))
                    {
                        var price = securities[ticker][Field.RegularMarketPrice];
                        stockPrices[ticker] = Convert.ToDecimal(price);
                    }
                    else
                    {
                        stockPrices[ticker] = -1; // If stock not found
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching stock prices: {ex.Message}");
            }

            return stockPrices;
        }
    }
}
