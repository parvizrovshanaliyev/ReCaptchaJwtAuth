using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ReCaptchaJwtAuth.API.Services;
using ReCaptchaJwtAuth.API.Settings;
using Xunit;

namespace ReCaptchaJwtAuth.Tests.Services
{
    public class JwtTokenServiceTests
    {
        private readonly JwtSettings _validJwtSettings = new JwtSettings
        {
            SecretKey = "securekey",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            TokenExpiryMinutes = 60
        };

        [Fact]
        public void GenerateToken_ValidInputs_ReturnsTokenWithCorrectClaims()
        {
            // Arrange
            var options = Options.Create(_validJwtSettings);
            var service = new JwtTokenService(options);

            // Act
            var token = service.GenerateToken("123", "test@example.com");

            // Parse token
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Assert
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "123");
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "test@example.com");
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Exp);
            jwtToken.Issuer.Should().Be(_validJwtSettings.Issuer);
            jwtToken.Audiences.Should().Contain(_validJwtSettings.Audience);
        }

        [Fact]
        public void GenerateToken_MissingSecretKey_ThrowsException()
        {
            // Arrange
            var invalidJwtSettings = new JwtSettings
            {
                SecretKey = null, // Missing SecretKey
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                TokenExpiryMinutes = 60
            };
            var options = Options.Create(invalidJwtSettings);

            // Act
            var service = new JwtTokenService(options);

            // Assert
            Assert.Throws<ArgumentNullException>(() => service.GenerateToken("123", "test@example.com"))
                .Message.Should().Contain("SecretKey");
        }

        [Fact]
        public void GenerateToken_ValidInputs_TokenHasValidSignature()
        {
            // Arrange
            var options = Options.Create(_validJwtSettings);
            var service = new JwtTokenService(options);

            // Act
            var token = service.GenerateToken("123", "test@example.com");

            // Parse token
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Assert: Validate signature
            handler.ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_validJwtSettings.SecretKey)),
                    ValidateIssuer = true,
                    ValidIssuer = _validJwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _validJwtSettings.Audience,
                    ValidateLifetime = true
                },
                out _).Should().NotBeNull("because the token should have a valid signature");
        }

        [Fact]
        public void GenerateToken_ValidInputs_TokenHasCorrectExpiration()
        {
            // Arrange
            var options = Options.Create(_validJwtSettings);
            var service = new JwtTokenService(options);

            // Act
            var token = service.GenerateToken("123", "test@example.com");

            // Parse token
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Assert: Validate expiration claim
            var expirationClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp);
            expirationClaim.Should().NotBeNull("because the token must have an expiration claim");

            var expirationUnixTime = long.Parse(expirationClaim.Value);
            var expirationDate = DateTimeOffset.FromUnixTimeSeconds(expirationUnixTime).UtcDateTime;

            expirationDate.Should().BeCloseTo(
                DateTime.UtcNow.AddMinutes(_validJwtSettings.TokenExpiryMinutes),
                TimeSpan.FromSeconds(5),
                "because the token should expire after the configured duration");
        }
    }
}
