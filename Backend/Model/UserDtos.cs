public class UserDto
{
    public string Username { get; set; }
    public string Email { get; set; }
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
