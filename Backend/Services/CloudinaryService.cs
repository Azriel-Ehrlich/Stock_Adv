using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;

namespace Backend.Services
{
    public class CloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly HttpClient _httpClient;

        public CloudinaryService(IConfiguration configuration)
        {
            var cloudinarySettings = configuration.GetSection("Cloudinary").Get<CloudinarySettings>();
            var account = new Account(
                cloudinarySettings.CloudName,
                cloudinarySettings.ApiKey,
                cloudinarySettings.ApiSecret
            );
            _cloudinary = new Cloudinary(account);
            _httpClient = new HttpClient();
        }

        public async Task<string> UploadImageAsync(Stream fileStream, string fileName, string folder = "profile_pictures")
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                Folder = folder,
                Transformation = new Transformation().Width(400).Height(400).Gravity("face").Crop("fill").Quality("auto")
            };
            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            return uploadResult.SecureUrl.ToString();
        }

        public async Task<string> GetDefaultProfileImageAsync(string username)
        {
            try
            {
                // Create a unique identifier
                var uniqueId = $"{username}_{Guid.NewGuid()}";

                // Options: bottts, avataaars, identicon, human, etc.
                string style = "avataaars";
                string seed = Uri.EscapeDataString(username);

                // Generate avatar URL (more visually appealing than letters)
                string avatarUrl = $"https://api.dicebear.com/7.x/{style}/png?seed={seed}&size=200";

                Console.WriteLine($"Generating avatar from: {avatarUrl}");

                // Download the generated avatar
                byte[] imageData = await _httpClient.GetByteArrayAsync(avatarUrl);

                // Upload to Cloudinary
                using var stream = new MemoryStream(imageData);
                var fileName = $"default_profile_{uniqueId}.png";

                return await UploadImageAsync(stream, fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating default profile image: {ex.Message}");
                throw;
            }
        }
    }

    public class CloudinarySettings
    {
        public string CloudName { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
    }
}