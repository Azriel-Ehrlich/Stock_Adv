using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class UserStock
    {
        [Key] // Primary Key
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; } // Foreign Key to Users table

        [Required]
        [MaxLength(10)]
        public string StockSymbol { get; set; } = string.Empty; // Example: "AAPL", "TSLA"

        [Required]
        public int Quantity { get; set; } // Number of stocks owned

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}
