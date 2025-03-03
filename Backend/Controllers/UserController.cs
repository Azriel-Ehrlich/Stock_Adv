using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly FirebaseAuthService _firebaseAuthService;
        private readonly UserService _userService;

        public UserController(FirebaseAuthService firebaseAuthService, UserService userService)
        {
            _firebaseAuthService = firebaseAuthService;
            _userService = userService;
        }

        // Register new user with email & password
        [HttpPost("register")]
        public async Task<IActionResult> RegisterWithPassword([FromBody] RegisterUserRequest request)
        {
            try
            {
                // Step 1: Create user in Firebase
                var firebaseUserId = await _firebaseAuthService.CreateUserWithPasswordAsync(request.Email, request.Password);

                // Step 2: Check if user already exists
                var existingUser = await _userService.GetUserByFirebaseIdAsync(firebaseUserId);
                if (existingUser != null)
                {
                    return Conflict("User already exists.");
                }

                // Step 3: Add user to database
                var newUser = new User
                {
                    FirebaseUserId = firebaseUserId,
                    Username = request.Username,
                    Email = request.Email,
                    ProfilePicture = request.ProfilePicture
                };

                await _userService.AddUserAsync(newUser);
                return Ok(new { UserId = firebaseUserId, Message = "User created successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
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
    }

    // Request model for user registration
    public class RegisterUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
    }

    // Request model for user login
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
