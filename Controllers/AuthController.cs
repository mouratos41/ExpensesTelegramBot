using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ExpenseTrackerApi.Data;
using ExpenseTrackerApi.Models;

namespace ExpenseTrackerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly AppDbContext _context;

    public AuthController(IConfiguration config, AppDbContext context)
    {
        _config = config;
        _context = context;
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] AuthRequest request)
    {
        if (_context.Users.Any(u => u.Username == request.Username))
            return BadRequest("Το Username υπάρχει ήδη.");

        var newUser = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            TelegramChatId = request.TelegramChatId
        };

        _context.Users.Add(newUser);
        _context.SaveChanges();

        return Ok(new { message = "Ο χρήστης δημιουργήθηκε επιτυχώς!" });
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] AuthRequest request)
    {
        var user = _context.Users.SingleOrDefault(u => u.Username == request.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized("Λάθος Username ή Password.");

        // Δημιουργούμε τα Tokens
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();

        // Σώζουμε το Refresh Token στη βάση
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // Το Refresh Token λήγει σε 7 μέρες
        
        _context.SaveChanges();

        return Ok(new TokenResponse(accessToken, refreshToken));
    }

    [HttpPost("refresh")]
    public IActionResult Refresh([FromBody] TokenResponse request)
    {
        var principal = GetPrincipalFromExpiredToken(request.AccessToken);
        var username = principal.Identity?.Name;

        var user = _context.Users.SingleOrDefault(u => u.Username == username);

        if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return BadRequest("Invalid client request");
        }

        var newAccessToken = GenerateAccessToken(user);
        var newRefreshToken = GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        _context.SaveChanges();

        return Ok(new TokenResponse(newAccessToken, newRefreshToken));
    }

    private string GenerateAccessToken(User user)
    {
        var jwtKey = _config["Jwt:Key"];
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("UserId", user.Id.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false, 
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
        
        if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid token");

        return principal;
    }
}

// --- DTOs ---
public record AuthRequest(string Username, string Password, long TelegramChatId);
public record TokenResponse(string AccessToken, string RefreshToken);