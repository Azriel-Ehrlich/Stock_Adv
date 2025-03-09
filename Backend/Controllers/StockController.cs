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

        // Buy stock
        [HttpPost("buy")]
        public async Task<IActionResult> BuyStock([FromBody] BuySellRequest request)
        {
            try
            {
                await _stockService.BuyStockAsync(request.FirebaseUserId, request.StockSymbol, request.Quantity);
                return Ok(new { Message = "Stock purchased successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // Sell stock
        [HttpPost("sell")]
        public async Task<IActionResult> SellStock([FromBody] BuySellRequest request)
        {
            try
            {
                await _stockService.SellStockAsync(request.FirebaseUserId, request.StockSymbol, request.Quantity);
                return Ok(new { Message = "Stock sold successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }

    public class StockRequest
    {
        public List<string> Tickers { get; set; } = new List<string>();
    }

    public class BuySellRequest
    {
        public string FirebaseUserId { get; set; } = string.Empty;
        public string StockSymbol { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
