using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using YahooFinanceApi;

namespace Backend.Services
{
    public class StockService
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;
        private const string YahooSearchUrl = "https://query2.finance.yahoo.com/v1/finance/search?q=";

        public StockService(AppDbContext context, HttpClient httpClient)
        {
            _context = context;
            _httpClient = httpClient;
        }

        // Search stock symbol by name (e.g., "Apple" -> "AAPL")
        public async Task<string?> SearchStockSymbolAsync(string query)
        {
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

        // Get stock prices for a list of tickers
        public async Task<Dictionary<string, StockDto>> GetStockPricesAsync(List<string> tickers)
        {
            var stockData = new Dictionary<string, StockDto>();

            try
            {
                var securities = await Yahoo.Symbols(tickers.ToArray())
                                            .Fields(Field.RegularMarketPrice, Field.RegularMarketOpen, Field.RegularMarketDayHigh,
                                                    Field.RegularMarketDayLow, Field.RegularMarketPreviousClose, Field.RegularMarketChangePercent,
                                                    Field.RegularMarketVolume, Field.MarketCap, Field.TrailingPE,
                                                    Field.EpsTrailingTwelveMonths, Field.LongName,Field.TrailingAnnualDividendRate)
                                            .QueryAsync();

                foreach (var ticker in tickers)
                {
                    if (securities.ContainsKey(ticker))
                    {
                        stockData[ticker] = new StockDto
                        {
                            Symbol = ticker,
                            Name = securities[ticker][Field.LongName], // Fetch company name
                            CurrentPrice = Convert.ToDecimal(securities[ticker][Field.RegularMarketPrice]),
                            OpenPrice = Convert.ToDecimal(securities[ticker][Field.RegularMarketOpen]),
                            HighPrice = Convert.ToDecimal(securities[ticker][Field.RegularMarketDayHigh]),
                            LowPrice = Convert.ToDecimal(securities[ticker][Field.RegularMarketDayLow]),
                            PreviousClose = Convert.ToDecimal(securities[ticker][Field.RegularMarketPreviousClose]),
                            ChangePercent = Convert.ToDecimal(securities[ticker][Field.RegularMarketChangePercent]),
                            Volume =  Convert.ToInt64(securities[ticker][Field.RegularMarketVolume]).ToString("N0"), // Format volume with commas
                            MarketCap =  $"{Convert.ToDecimal(securities[ticker][Field.MarketCap]) / 1_000_000_000:N1}B", // Format market cap in billions
                              
                            PE = Convert.ToDecimal(securities[ticker][Field.TrailingPE]).ToString("N1"),
               
                            EPS = Convert.ToDecimal(securities[ticker][Field.EpsTrailingTwelveMonths]).ToString("N2"),
                                
                            Dividend = Convert.ToDecimal(securities[ticker][Field.TrailingAnnualDividendRate]).ToString("N2"),
                               
                            YearLow = Convert.ToDecimal(securities[ticker][Field.RegularMarketDayLow]) * 0.8m,  // Estimated
                            YearHigh = Convert.ToDecimal(securities[ticker][Field.RegularMarketDayHigh]) * 1.2m  // Estimated
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching stock data: {ex.Message}");
            }

            return stockData;
        }






        //Buy stock (Adds to user portfolio & transaction history)
        public async Task<bool> BuyStockAsync(string firebaseId, string stockSymbol, int quantity)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.FirebaseUserId == firebaseId);
            if (user == null)
            {
                throw new Exception("User not found.");
            }

            var stockPrices = await GetStockPricesAsync(new List<string> { stockSymbol });
            if (!stockPrices.ContainsKey(stockSymbol) || stockPrices[stockSymbol].CurrentPrice <= 0)
            {
                throw new Exception("Invalid stock symbol or unable to fetch stock price.");
            }

            decimal stockPrice = stockPrices[stockSymbol].CurrentPrice;
            decimal totalCost = stockPrice * quantity;

            var balance = await _context.Balances.FirstOrDefaultAsync(b => b.UserId == user.Id);
            if (balance == null)
            {
                throw new Exception("Balance record not found for the user.");
            }

            if (balance.Amount < totalCost)
            {
                throw new Exception("Insufficient funds.");
            }

            balance.Amount -= totalCost;

            var transaction = new Transaction
            {
                UserId = user.Id,
                StockSymbol = stockSymbol,
                Quantity = quantity,
                Price = stockPrice,
                TransactionDate = DateTime.UtcNow.ToLocalTime(),
                IsPurchase = true
            };

            _context.Transactions.Add(transaction);

            var userStock = await _context.UserStocks.FirstOrDefaultAsync(us => us.UserId == user.Id && us.StockSymbol == stockSymbol);
            if (userStock == null)
            {
                userStock = new UserStock
                {
                    UserId = user.Id,
                    StockSymbol = stockSymbol,
                    Quantity = quantity
                };
                _context.UserStocks.Add(userStock);
            }
            else
            {
                userStock.Quantity += quantity;
            }

            await _context.SaveChangesAsync();
            return true;
        }



        //Sell stock (Removes from user portfolio & adds to transaction history)
        public async Task<bool> SellStockAsync(string firebaseId, string stockSymbol, int quantity)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.FirebaseUserId == firebaseId);
            if (user == null)
            {
                throw new Exception("User not found.");
            }

            var stockPrices = await GetStockPricesAsync(new List<string> { stockSymbol });
            if (!stockPrices.ContainsKey(stockSymbol) || stockPrices[stockSymbol].CurrentPrice <= 0)
            {
                throw new Exception("Invalid stock symbol or unable to fetch stock price.");
            }

            decimal stockPrice = stockPrices[stockSymbol].CurrentPrice;
            decimal totalSaleValue = stockPrice * quantity;

            var userStock = await _context.UserStocks.FirstOrDefaultAsync(us => us.UserId == user.Id && us.StockSymbol == stockSymbol);
            if (userStock == null || userStock.Quantity < quantity)
            {
                throw new Exception("Not enough stocks to sell.");
            }

            var balance = await _context.Balances.FirstOrDefaultAsync(b => b.UserId == user.Id);
            if (balance == null)
            {
                throw new Exception("Balance record not found for the user.");
            }

            balance.Amount += totalSaleValue;

            userStock.Quantity -= quantity;
            if (userStock.Quantity == 0)
            {
                _context.UserStocks.Remove(userStock);
            }

            var transaction = new Transaction
            {
                UserId = user.Id,
                StockSymbol = stockSymbol,
                Quantity = quantity,
                Price = stockPrice,
                TransactionDate = DateTime.UtcNow,
                IsPurchase = false
            };

            _context.Transactions.Add(transaction);

            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<List<StockHistoryDto>> GetStockHistoryAsync(string ticker, DateTime startDate, DateTime endDate)
        {
            var historyData = new List<StockHistoryDto>();

            try
            {
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

                // Convert dates to UNIX timestamps (seconds since 1970)
                long startUnix = ((DateTimeOffset)startDate).ToUnixTimeSeconds();
                long endUnix = ((DateTimeOffset)endDate).ToUnixTimeSeconds();

                // Constructing the API request URL
                string url = $"https://query1.finance.yahoo.com/v8/finance/chart/{ticker}?period1={startUnix}&period2={endUnix}&interval=1wk";

                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode(); // Throws an error if the status is not 200

                string responseBody = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(responseBody);

                // Extracting data from JSON response
                var root = doc.RootElement;
                var chart = root.GetProperty("chart");
                var result = chart.GetProperty("result")[0];
                var timestamps = result.GetProperty("timestamp");
                var indicators = result.GetProperty("indicators").GetProperty("quote")[0];

                var opens = indicators.GetProperty("open").EnumerateArray();
                var closes = indicators.GetProperty("close").EnumerateArray();
                var highs = indicators.GetProperty("high").EnumerateArray();
                var lows = indicators.GetProperty("low").EnumerateArray();
                var volumes = indicators.GetProperty("volume").EnumerateArray();

                foreach (var timestamp in timestamps.EnumerateArray())
                {
                    historyData.Add(new StockHistoryDto
                    {
                        Date = (DateTimeOffset.FromUnixTimeSeconds(timestamp.GetInt64()).UtcDateTime).ToString("dd/MM/yy"), // Store as DateTime
                        Open = opens.MoveNext() ? Convert.ToDecimal(opens.Current.GetDouble()) : 0,
                        Close = closes.MoveNext() ? Convert.ToDecimal(closes.Current.GetDouble()) : 0,
                        High = highs.MoveNext() ? Convert.ToDecimal(highs.Current.GetDouble()) : 0,
                        Low = lows.MoveNext() ? Convert.ToDecimal(lows.Current.GetDouble()) : 0,
                        Volume = volumes.MoveNext() ? Convert.ToInt64(volumes.Current.GetDouble()) : 0
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching historical data: {ex.Message}");
            }

            return historyData;
        }

    }
}


