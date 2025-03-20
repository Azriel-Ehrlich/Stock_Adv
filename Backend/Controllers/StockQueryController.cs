using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/stocks-query")]
    public class StockQueryController : ControllerBase
    {
        private readonly StockService _stockService;

        public StockQueryController(StockService stockService)
        {
            _stockService = stockService;
        }

        [HttpPost("prices")]
        public async Task<IActionResult> GetStockPrices([FromBody] StockRequest request)
        {
            var stockPrices = await _stockService.GetStockPricesAsync(request.Tickers);
            return Ok(stockPrices);
        }

        // Search for stock symbol by company name
        [HttpGet("search")]
        public async Task<IActionResult> SearchStock([FromQuery] string query)
        {
            var symbol = await _stockService.SearchStockSymbolAsync(query);
            if (symbol == null)
            {
                return NotFound(new { Error = "Stock not found." });
            }

            return Ok(new { Symbol = symbol });
        }

       

        [HttpGet("history")]
        public async Task<IActionResult> GetStockHistory(string ticker, DateTime startDate, DateTime endDate)
        {
            var historyData = await _stockService.GetStockHistoryAsync(ticker, startDate, endDate);
            return Ok(historyData);
        }

    }

    public class StockRequest
    {
        public List<string> Tickers { get; set; } = new List<string>();
    }

   
}
