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

        //Fetch stock data for multiple tickers
        [HttpPost("prices")]
        public async Task<IActionResult> GetStockPrices([FromBody] StockRequest request)
        {
            var stockPrices = await _stockService.GetStockPricesAsync(request.Tickers);
            return Ok(stockPrices);
        }
    }

    //Model for API request
    public class StockRequest
    {
        public List<string> Tickers { get; set; } = new();
    }
}
