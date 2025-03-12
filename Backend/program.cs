using Backend.Services;
using Backend.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Load configuration from appsettings.json
var configuration = builder.Configuration;

// 🔹 Add database context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

// 🔹 Register services
builder.Services.AddSingleton<FirebaseAuthService>(); // Firebase authentication service
builder.Services.AddScoped<UserService>(); // User management service
builder.Services.AddSingleton<CloudinaryService>(); // Cloudinary service
builder.Services.AddScoped<StockService>(); // Stock data service
builder.Services.AddSingleton<QdrantService>();
builder.Services.AddHttpClient<OllamaService>();
builder.Services.AddHttpClient();

// 🔹 Add controllers
builder.Services.AddControllers();

// 🔹 Enable Swagger (for API testing)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 🔹 Configure middleware for development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 🔹 Enable HTTPS redirection
app.UseHttpsRedirection();

// 🔹 Enable authorization
app.UseAuthorization();

// 🔹 Map API controllers
app.MapControllers();

// 🔹 Run the application
app.Run();
