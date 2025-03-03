using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class Transaction
    {
        [Key] // Primary Key
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; } // Foreign Key to Users table

        [Required]
        [MaxLength(10)]
        public string StockSymbol { get; set; } = string.Empty; // Example: "AAPL", "TSLA"

        [Required]
        public int Quantity { get; set; } // Number of stocks bought/sold

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; } // Price per stock at the time of transaction

        [Required]
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow; // Timestamp of the transaction

        [Required]
        public bool IsPurchase { get; set; } // True for buy, False for sell

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}
