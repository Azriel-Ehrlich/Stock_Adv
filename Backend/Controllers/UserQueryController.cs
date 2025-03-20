using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/user-query")]
    public class UserQueryController : ControllerBase
    {
        private readonly FirebaseAuthService _firebaseAuthService;
        private readonly UserService _userService;
        private readonly CloudinaryService _cloudinaryService;


        public UserQueryController(FirebaseAuthService firebaseAuthService, UserService userService, CloudinaryService cloudinaryService)
        {
            _firebaseAuthService = firebaseAuthService;
            _userService = userService;
            _cloudinaryService = cloudinaryService;
        }



        // Login user using email & password (Firebase Authentication)
        [HttpPost("login")]
        public async Task<IActionResult> LoginUser([FromBody] LoginRequest request)
        {
            try
            {
                // Step 1: Verify user credentials with Firebase
                var firebaseIdToken = await _firebaseAuthService.LoginWithEmailAndPasswordAsync(request.Email, request.Password);

                // Step 2: Decode token to get Firebase User ID
                var decodedToken = await _firebaseAuthService.VerifyTokenAsync(firebaseIdToken);
                var firebaseUserId = decodedToken.Uid;

                // Step 3: Check if user exists in the local database
                var user = await _userService.GetUserByFirebaseIdAsync(firebaseUserId);
                if (user == null)
                {
                    return NotFound("User not found in database.");
                }

                // Step 4: Return user data
                return Ok(new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.FirebaseUserId,
                    user.ProfilePicture,
                    Token = firebaseIdToken // Returning token for future authentication
                });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { Error = "Invalid credentials", Details = ex.Message });
            }
        }






        [HttpGet("{firebaseId}")]
        public async Task<IActionResult> GetUser(string firebaseId)
        {
            var user = await _userService.GetUser(firebaseId);
            if (user == null) return NotFound("User not found");
            return Ok(user);
        }


        [HttpGet("{firebaseId}/stocks")]
        public async Task<IActionResult> GetUserStocks(string firebaseId)
        {
            var stocks = await _userService.GetUserStocks(firebaseId);
            return Ok(stocks);
        }

        [HttpGet("{firebaseId}/transactions")]
        public async Task<IActionResult> GetUserTransactions(string firebaseId)
        {
            var transactions = await _userService.GetUserTransactions(firebaseId);
            return Ok(transactions);
        }


        //Get user balance
        [HttpGet("balance/{firebaseUserId}")]
        public async Task<IActionResult> GetUserBalance(string firebaseUserId)
        {
            try
            {
                var balance = await _userService.GetUserBalanceAsync(firebaseUserId);
                return Ok(new { Balance = balance });
            }
            catch (Exception ex)
            {
                return NotFound(new { Error = ex.Message });
            }
        }




    }



    // Request model for user login
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }




}
