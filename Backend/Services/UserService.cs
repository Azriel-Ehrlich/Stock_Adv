using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
    public class UserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        // Get user by Firebase ID
        public async Task<User?> GetUserByFirebaseIdAsync(string firebaseId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.FirebaseUserId == firebaseId);
        }

        // Get user by ID
        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        // Get all users
        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        // Add a new user to the database
        public async Task<User> AddUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        // Update user details
        public async Task<bool> UpdateUserAsync(int id, User updatedUser)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            user.Email = updatedUser.Email;
            await _context.SaveChangesAsync();
            return true;
        }

        // Delete user from the database
        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        // Get user details
        public async Task<UserDto> GetUser(string firebaseId)
        {
            var user = await _context.Users
                .Where(u => u.FirebaseUserId == firebaseId)
                .Select(u => new UserDto
                {
                    Username = u.Username,
                    Email = u.Email,
                   ProfilePicture = u.ProfilePicture
                })
                .FirstOrDefaultAsync();

            return user;
        }

        // Get all stocks owned by a user
        public async Task<List<UserStockDto>> GetUserStocks(string firebaseId)
        {
            var stocks = await _context.UserStocks
                .Where(us => us.User.FirebaseUserId == firebaseId)
                .Select(us => new UserStockDto
                {
                    StockSymbol = us.StockSymbol,
                    Quantity = us.Quantity
                })
                .ToListAsync();

            return stocks;
        }
        // Get all transactions for a user
        public async Task<List<TransactionDto>> GetUserTransactions(string firebaseId)
        {
            var transactions = await _context.Transactions
                .Where(t => t.User.FirebaseUserId == firebaseId)
                .Select(t => new TransactionDto
                {
                    StockSymbol = t.StockSymbol,
                    Quantity = t.Quantity,
                    TransactionType = t.IsPurchase ? "Buy" : "Sell",
                    Price = t.Price,
                    Date = t.TransactionDate
                })
                .ToListAsync();

            return transactions;
        }


    }
}
