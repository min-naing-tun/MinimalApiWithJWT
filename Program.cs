using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// --- JWT CONFIGURATION ---
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes("minnaingtunJWTAuthTest2026!@#$%^");

// --- JWT CONFIGURE SERVICES ---
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false, // Set to true in production
        ValidateAudience = false, // Set to true in production
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();


app.UseAuthentication();
app.UseAuthorization();



app.MapPost("/login", (UserLogin user) =>
{
    if (user.Username == "admin" && user.Password == "minnaingtun")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("MNT Custom Claim Type 1", "Min Naing Htun"),
                new Claim("MNT Custom Claim Type 2", "Nyi Naing Chay")
            }),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return Results.Ok(new { Token = tokenHandler.WriteToken(token) });
    }

    return Results.Unauthorized();
});

// Protected Endpoint: Checks the Token via Middleware
app.MapGet("/secure-data", [Authorize] () =>
{
    return Results.Ok(new { Message = "You have accessed protected data!", Date = DateTime.Now });
});

app.MapGet("/getRandomNumber", [Authorize] (int count) =>
{
    if(count <= 0)
    {
        return Results.BadRequest(new { Message = "Count must be greater than zero." });
    }
    else
    {
        var numbers = new List<int>();
        var random = new Random();

        for (int i = 0; i < count; i++)
        {
            var randomNumber = random.Next(1, 10000);
            numbers.Add(randomNumber);
        }

        return Results.Ok(numbers);
    }
});

app.MapGet("/generate-content", async (int paras) =>
{
    using var client = new HttpClient();

    // Fetching from Bacon Ipsum for clean JSON
    var response = await client.GetFromJsonAsync<List<string>>(
        $"https://baconipsum.com/api/?type=meat-and-filler&paras={paras}&format=json"
    );

    return Results.Ok(new
    {
        Source = "BaconIpsum",
        ParagraphCount = paras,
        Content = response
    });
});

// Public Endpoint
app.MapGet("/", () => "API is running...");

app.Run();

// DTO for Login
public record UserLogin(string Username, string Password);