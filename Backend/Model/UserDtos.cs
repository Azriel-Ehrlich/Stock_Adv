public class UserDto
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string? ProfilePicture { get; set; }
}

public class UserStockDto
{
    public string StockSymbol { get; set; }
    public int Quantity { get; set; }
}

public class TransactionDto
{
    public string StockSymbol { get; set; }
    public int Quantity { get; set; }
    public string TransactionType { get; set; }
    public decimal Price { get; set; }
    public DateTime Date { get; set; }
}

public class StockDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty; // Company name
    public decimal CurrentPrice { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal PreviousClose { get; set; }
    public decimal ChangePercent { get; set; }
    public string Volume { get; set; } = "N/A"; // Formatted volume
    public decimal YearLow { get; set; }
    public decimal YearHigh { get; set; }
    public string MarketCap { get; set; } = "N/A"; // Formatted market cap
    public string PE { get; set; } = "N/A"; // Price-to-earnings ratio
    public string EPS { get; set; } = "N/A"; // Earnings per share
    public string Dividend { get; set; } = "N/A"; // Dividend info
}

