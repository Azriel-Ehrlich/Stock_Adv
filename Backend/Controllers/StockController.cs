using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/stocks")]
    public class StockController : ControllerBase
    {
        private readonly StockService _stockService;

        public StockController(StockService stockService)
        {
            _stockService = stockService;
        }

        [HttpPost("prices")]
        public async Task<IActionResult> GetStockPrices([FromBody] StockRequest request)
        {
            var stockPrices = await _stockService.GetStockPricesAsync(request.Tickers);
            return Ok(stockPrices);
        }

        //Search for stock symbol by company name
        [HttpGet("search")]
        public async Task<IActionResult> SearchStock([FromQuery] string query)
        {
            var symbol = await _stockService.SearchStockSymbolAsync(query);
            if (symbol == null)
            {
                return NotFound("Stock not found.");
            }

            return Ok(new { Symbol = symbol });
        }
    }

    public class StockRequest
    {
        public List<string> Tickers { get; set; } = new List<string>();
    }
}
