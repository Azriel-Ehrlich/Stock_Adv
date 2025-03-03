using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public class User
    {
        [Key] // Primary Key
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string FirebaseUserId { get; set; } = string.Empty; // Unique ID from Firebase

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty; // Unique username

        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty; // User email (for convenience)

        public string? ProfilePicture { get; set; } // Optional profile picture URL
    }
}
