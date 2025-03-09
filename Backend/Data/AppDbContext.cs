using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } // Users table
        public DbSet<UserStock> UserStocks { get; set; }// User and his/her stocks table

        public DbSet<Transaction> Transactions { get; set; }// Transactions history table

        public DbSet<Balance> Balances { get; set; }// User account balance table

    }
}
