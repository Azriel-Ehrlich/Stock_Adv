using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly FirebaseAuthService _firebaseAuthService;
        private readonly UserService _userService;
        private readonly CloudinaryService _cloudinaryService;


        public UserController(FirebaseAuthService firebaseAuthService, UserService userService, CloudinaryService cloudinaryService)
        {
            _firebaseAuthService = firebaseAuthService;
            _userService = userService;
            _cloudinaryService = cloudinaryService;
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

                string profilePictureUrl;

                if (string.IsNullOrEmpty(request.ProfilePicture))
                {
                    // If no profile picture provided, use default
                    profilePictureUrl = await _cloudinaryService.GetDefaultProfileImageAsync(request.Username);
                }
                else
                {
                    // User provided a profile picture URL (base64 data or external URL)
                    // For base64 images, you would decode and upload here
                    profilePictureUrl = request.ProfilePicture;

                }


                // Step 4: Add user to database
                var newUser = new User
                {
                    FirebaseUserId = firebaseUserId,
                    Username = request.Username,
                    Email = request.Email,
                    ProfilePicture = profilePictureUrl
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

        // ✅ Login/Register with Google (New)
        [HttpPost("login-google")]
        public async Task<IActionResult> LoginWithGoogle([FromBody] GoogleLoginRequest request)
        {
            var decodedToken = await _firebaseAuthService.VerifyGoogleTokenAsync(request.IdToken);
            if (decodedToken == null)
            {
                return Unauthorized("Invalid Google token.");
            }

            var firebaseUserId = decodedToken.Uid;
            var email = decodedToken.Claims.ContainsKey("email") ? decodedToken.Claims["email"].ToString() : null;

            if (email == null)
            {
                return BadRequest("Google account must have an email.");
            }

            // Check if user already exists
            var user = await _userService.GetUserByFirebaseIdAsync(firebaseUserId);
            if (user == null)
            {
                // Register new user
                user = new User
                {
                    FirebaseUserId = firebaseUserId,
                    Email = email
                };

                await _userService.AddUserAsync(user);
            }

            return Ok(user);
        }
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                // Verify the Firebase ID token
                var decodedToken = await _firebaseAuthService.VerifyTokenAsync(request.IdToken);
                var firebaseUserId = decodedToken.Uid;

                // Update the password in Firebase
                await _firebaseAuthService.ChangeUserPasswordAsync(firebaseUserId, request.NewPassword);

                return Ok(new { Message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                await _firebaseAuthService.SendPasswordResetEmailAsync(request.Email);
                return Ok(new { Message = "Password reset email sent successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
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
    //Request model for Google Login
    public class GoogleLoginRequest
    {
        public string IdToken { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        public string IdToken { get; set; } = string.Empty; // The user's Firebase token
        public string NewPassword { get; set; } = string.Empty; // The new password
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }

}
