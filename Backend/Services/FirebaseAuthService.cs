using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth; // Add this import
using Google.Apis.Auth.OAuth2;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Backend.Services
{
    public class FirebaseAuthService
    {
        private readonly FirebaseApp _firebaseApp;
        private readonly string _firebaseApiKey;

        public FirebaseAuthService(IConfiguration configuration)
        {
            // Initialize Firebase if not already initialized
            if (FirebaseApp.DefaultInstance == null)
            {
                _firebaseApp = FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(configuration["Firebase:CredentialPath"])
                });
            }
            else
            {
                _firebaseApp = FirebaseApp.DefaultInstance;
            }

            _firebaseApiKey = configuration["Firebase:ApiKey"];
        }

        // Verify Firebase ID token
        public async Task<FirebaseToken> VerifyTokenAsync(string idToken)
        {
            return await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
        }

        // Create a new Firebase user with email and password
        public async Task<string> CreateUserWithPasswordAsync(string email, string password)
        {
            var userRecordArgs = new UserRecordArgs
            {
                Email = email,
                Password = password
            };

            var userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(userRecordArgs);
            return userRecord.Uid;
        }

        // Login user with email and password, return ID token
        public async Task<string> LoginWithEmailAndPasswordAsync(string email, string password)
        {
            using var client = new HttpClient();
            var requestData = new
            {
                email,
                password,
                returnSecureToken = true
            };

            var jsonRequest = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_firebaseApiKey}", content);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Invalid email or password.");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);
            return doc.RootElement.GetProperty("idToken").GetString();
        }


        // NEW METHOD: Sign in with Google Token
        public async Task<string> SignInWithGoogleTokenAsync(string googleIdToken)
        {
            try
            {
                // Step 1: Verify the Google ID token
                var payload = await GoogleJsonWebSignature.ValidateAsync(googleIdToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { "903141592573-jp9pf3tnl62t0r4hf5mgk88do69sjikb.apps.googleusercontent.com" }
                });

                string email = payload.Email;
                string name = payload.Name;
                string pictureUrl = payload.Picture;

                // Step 2: Check if user exists in Firebase
                try
                {
                    // Try to get existing user
                    var existingUser = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email);

                    // User exists, create a custom token
                    string customToken = await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync(existingUser.Uid);

                    // Exchange custom token for ID token
                    return await ExchangeCustomTokenForIdTokenAsync(customToken);
                }
                catch (FirebaseAuthException)
                {
                    // User doesn't exist, create a new one
                    var userArgs = new UserRecordArgs
                    {
                        Email = email,
                        DisplayName = name,
                        PhotoUrl = pictureUrl,
                        EmailVerified = true
                    };

                    var newUser = await FirebaseAuth.DefaultInstance.CreateUserAsync(userArgs);

                    // Create a custom token for the new user
                    string customToken = await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync(newUser.Uid);

                    // Exchange for ID token
                    return await ExchangeCustomTokenForIdTokenAsync(customToken);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error signing in with Google: {ex.Message}");
            }
        }

        // NEW METHOD: Exchange custom token for ID token
        private async Task<string> ExchangeCustomTokenForIdTokenAsync(string customToken)
        {
            using var client = new HttpClient();
            var requestData = new
            {
                token = customToken,
                returnSecureToken = true
            };

            var jsonRequest = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"https://identitytoolkit.googleapis.com/v1/accounts:signInWithCustomToken?key={_firebaseApiKey}", content);

            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error exchanging custom token: {jsonResponse}");
            }

            using var doc = JsonDocument.Parse(jsonResponse);
            return doc.RootElement.GetProperty("idToken").GetString();
        }


        public async Task ChangeUserPasswordAsync(string firebaseUserId, string newPassword)
        {
            var userRecordArgs = new UserRecordArgs
            {
                Uid = firebaseUserId,
                Password = newPassword
            };

            await FirebaseAuth.DefaultInstance.UpdateUserAsync(userRecordArgs);
        }

        public async Task SendPasswordResetEmailAsync(string email)
        {
            await FirebaseAuth.DefaultInstance.GeneratePasswordResetLinkAsync(email);
        }

    }
}
