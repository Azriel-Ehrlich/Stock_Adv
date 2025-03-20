using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/stocks-command")]
    public class StockCommandController : ControllerBase
    {
        private readonly StockService _stockService;

        public StockCommandController(StockService stockService)
        {
            _stockService = stockService;
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


    public class BuySellRequest
    {
        public string FirebaseUserId { get; set; } = string.Empty;
        public string StockSymbol { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
