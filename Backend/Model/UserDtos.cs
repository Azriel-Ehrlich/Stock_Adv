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
    public decimal CurrentPrice { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal PreviousClose { get; set; }
    public decimal ChangePercent { get; set; }
}

