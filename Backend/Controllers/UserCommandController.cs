using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/user-command")]
    public class UserCommandController : ControllerBase
    {
        private readonly FirebaseAuthService _firebaseAuthService;
        private readonly UserService _userService;
        private readonly CloudinaryService _cloudinaryService;


        public UserCommandController(FirebaseAuthService firebaseAuthService, UserService userService, CloudinaryService cloudinaryService)
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
                // Add user to the database
                await _userService.AddUserAsync(newUser);

                //Create initial balance for the new user
                await _userService.AddUserBalanceAsync(newUser.Id);

                return Ok(new { UserId = firebaseUserId, Message = "User created successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        

        //Login/Register with Google (New)
        // Update this method in your UserController class
        [HttpPost("login-google")]
        public async Task<IActionResult> LoginWithGoogle([FromBody] GoogleLoginRequest request)
        {
            try
            {
                // Use the new method to handle Google authentication
                var firebaseIdToken = await _firebaseAuthService.SignInWithGoogleTokenAsync(request.IdToken);

                // Verify the Firebase token
                var decodedToken = await _firebaseAuthService.VerifyTokenAsync(firebaseIdToken);
                var firebaseUserId = decodedToken.Uid;

                // Get email from token claims
                var email = decodedToken.Claims.ContainsKey("email") ? decodedToken.Claims["email"].ToString() : null;

                if (email == null)
                {
                    return BadRequest("Google account must have an email.");
                }

                // Check if user already exists in your database
                var user = await _userService.GetUserByFirebaseIdAsync(firebaseUserId);
                // if user does not exist, create a new user
                if (user == null)
                {
                    // Get name from token claims
                    var name = decodedToken.Claims.ContainsKey("name") ? decodedToken.Claims["name"].ToString() : "User";

                    // Register new user in your database
                    user = new User
                    {
                        FirebaseUserId = firebaseUserId,
                        Email = email,
                        Username = name
                    };

                    // add the user to the database
                    await _userService.AddUserAsync(user);

                    //Create initial balance for the new user
                    await _userService.AddUserBalanceAsync(user.Id);
                }

                // Return user data with token
                return Ok(new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.FirebaseUserId,
                    user.ProfilePicture,
                    Token = firebaseIdToken // Return the Firebase ID token
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
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

        [HttpGet("{firebaseId}")]
        public async Task<IActionResult> GetUser(string firebaseId)
        {
            var user = await _userService.GetUser(firebaseId);
            if (user == null) return NotFound("User not found");
            return Ok(user);
        }



        //Update user balance
        [HttpPost("balance/update")]
        public async Task<IActionResult> UpdateUserBalance([FromBody] UpdateBalanceRequest request)
        {
            var success = await _userService.UpdateUserBalanceAsync(request.FirebaseUserId, request.AmountChange);
            if (!success)
            {
                return BadRequest("Failed to update balance.");
            }

            return Ok(new { Message = "Balance updated successfully" });
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

    //Request model for balance update
    public class UpdateBalanceRequest
    {
        public string FirebaseUserId { get; set; } = string.Empty;
        public decimal AmountChange { get; set; }
    }

}
