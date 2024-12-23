using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ReCaptchaJwtAuth.API.Settings;

namespace ReCaptchaJwtAuth.API.Services;

public interface IJwtTokenService
{
    string GenerateToken(string userId, string email);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;

    public JwtTokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value ?? throw new ArgumentNullException(nameof(jwtSettings), "JWT settings are required.");
        
        // Validate Secret Key length (must be 32 characters/256 bits)
        if (_jwtSettings.SecretKey.Length < 32)
        {
            throw new ArgumentException("The JWT SecretKey must be at least 32 characters long (256 bits).");
        }
    }

    public string GenerateToken(string userId, string email)
    {
        // Prepare claims
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Create the signing key
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Set the expiration time, with a default value if not provided
        var expirationTime = DateTime.UtcNow.AddMinutes(_jwtSettings.TokenExpiryMinutes > 0 ? _jwtSettings.TokenExpiryMinutes : 60);

        // Create JWT token
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expirationTime,
            signingCredentials: credentials
        );

        // Write the token as a string and return it
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}