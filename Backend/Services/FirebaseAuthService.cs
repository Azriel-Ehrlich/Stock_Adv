using FirebaseAdmin;
using FirebaseAdmin.Auth;
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

        //Verify Google Token for Authentication (New)
        public async Task<FirebaseToken?> VerifyGoogleTokenAsync(string idToken)
        {
            try
            {
                return await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
            }
            catch
            {
                return null; // Invalid token
            }
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
