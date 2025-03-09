using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Backend.Services
{
    public class StockService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public StockService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _apiKey = configuration["Polygon:ApiKey"]; //we will read the api key from appsettings.json
        }

        //Fetch stock data for multiple tickers
        public async Task<List<StockData>> GetStockPricesAsync(List<string> tickers)
        {
            var symbols = string.Join(",", tickers);
            var url = $"https://api.polygon.io/v2/snapshot/locale/us/markets/stocks/tickers?apiKey={_apiKey}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to fetch stock data from Polygon.io");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PolygonStockResponse>(jsonResponse);

            var stockList = new List<StockData>();

            foreach (var stock in result.Tickers)
            {
                if (tickers.Contains(stock.Ticker))
                {
                    stockList.Add(new StockData
                    {
                        Symbol = stock.Ticker,
                        Price = stock.LastTrade.Price
                    });
                }
            }

            return stockList;
        }
    }

    //Model for Stock Data
    public class StockData
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    //Model for Polygon API Response
    public class PolygonStockResponse
    {
        public List<PolygonStock> Tickers { get; set; } = new();
    }

    public class PolygonStock
    {
        public string Ticker { get; set; } = string.Empty;
        public PolygonLastTrade LastTrade { get; set; } = new();
    }

    public class PolygonLastTrade
    {
        public decimal Price { get; set; }
    }
}
