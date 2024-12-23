using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using ReCaptchaJwtAuth.API.Models;
using ReCaptchaJwtAuth.API.Persistence.Data;
using ReCaptchaJwtAuth.API.Services;
using ReCaptchaJwtAuth.Tests.Setups;
using ErrorOr;

namespace ReCaptchaJwtAuth.Tests.Services
{
    public class AccountServiceTests
    {
        private readonly Mock<IGoogleReCaptchaService> _mockReCaptchaService;
        private readonly Mock<IJwtTokenService> _mockJwtTokenService;
        private readonly ApplicationDbContext _dbContext;
        private readonly AccountService _accountService;

        public AccountServiceTests()
        {
            _mockReCaptchaService = new Mock<IGoogleReCaptchaService>();
            _mockJwtTokenService = new Mock<IJwtTokenService>();
            _dbContext = DatabaseContextSetup.CreateInMemoryDbContext();

            _accountService = new AccountService(_dbContext, _mockReCaptchaService.Object, _mockJwtTokenService.Object);
        }

        private async Task<User> CreateTestUserAsync(string email, string password)
        {
            var passwordHasher = new PasswordHasher<User>();
            var user = new User
            {
                Email = email,
                PasswordHash = passwordHasher.HashPassword(null, password)
            };

            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            return user;
        }

        [Fact]
        public async Task LoginAsync_ValidInputs_ReturnsToken()
        {
            // Arrange
            var user = await CreateTestUserAsync("test@example.com", "password");

            _mockReCaptchaService
                .Setup(s => s.VerifyReCaptchaAsync(It.IsAny<string>(), "login", CancellationToken.None))
                .ReturnsAsync(true); // For success

            _mockJwtTokenService
                .Setup(s => s.GenerateToken(user.Id.ToString(), user.Email))
                .Returns("valid-jwt-token");

            var loginRequest = new LoginRequest
            {
                Email = user.Email,
                Password = "password",
                ReCaptchaToken = "valid-token",
                Action = "login"
            };

            // Act
            var result = await _accountService.LoginAsync(loginRequest,
                CancellationToken.None);

            // Assert
            result.IsError.Should().BeFalse();
            result.Value.Should().Be("valid-jwt-token");

            // Verify mock interactions
            _mockReCaptchaService.Verify(
                s => s.VerifyReCaptchaAsync("valid-token", "login", It.IsAny<CancellationToken>()), Times.Once);
            _mockJwtTokenService.Verify(s => s.GenerateToken(user.Id.ToString(), user.Email), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ReturnsError()
        {
            // Arrange
            var user = await CreateTestUserAsync("test@example.com", "password");

            _mockReCaptchaService
                .Setup(s => s.VerifyReCaptchaAsync(It.IsAny<string>(), "login", CancellationToken.None))
                .ReturnsAsync(true); // For success

            var loginRequest = new LoginRequest
            {
                Email = user.Email,
                Password = "wrong-password",
                ReCaptchaToken = "valid-token",
                Action = "login"
            };

            // Act
            var result = await _accountService.LoginAsync(loginRequest,
                CancellationToken.None);

            // Assert
            result.IsError.Should().BeTrue();
            result.FirstError.Description.Should().Be("Invalid email or password.");

            // Verify reCAPTCHA validation occurred
            _mockReCaptchaService.Verify(
                s => s.VerifyReCaptchaAsync(It.IsAny<string>(), "login", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_InvalidReCaptcha_ReturnsError()
        {
            // Arrange
            var user = await CreateTestUserAsync("test@example.com", "password");

            _mockReCaptchaService
                .Setup(s => s.VerifyReCaptchaAsync(It.IsAny<string>(), "login", CancellationToken.None))
                .ReturnsAsync(ErrorOr<bool>.From(new List<Error>
                {
                    Error.Validation("ReCaptcha.Failed", "The reCAPTCHA validation failed.")
                }));

            var loginRequest = new LoginRequest
            {
                Email = user.Email,
                Password = "password",
                ReCaptchaToken = "invalid-token",
                Action = "login"
            };

            // Act
            var result = await _accountService.LoginAsync(loginRequest,
                CancellationToken.None);

            // Assert
            result.IsError.Should().BeTrue();
            result.FirstError.Description.Should().Be("The reCAPTCHA validation failed.");
        }

        [Fact]
        public async Task LoginAsync_NonexistentUser_ReturnsError()
        {
            // Arrange
            _mockReCaptchaService
                .Setup(s => s.VerifyReCaptchaAsync(It.IsAny<string>(), "login", CancellationToken.None))
                .ReturnsAsync(true); // For success
            
            var loginRequest = new LoginRequest
            {
                Email = "nonexistent@example.com",
                Password = "password",
                ReCaptchaToken = "valid-token",
                Action = "login"
            };

            // Act
            var result = await _accountService.LoginAsync(loginRequest,
                CancellationToken.None);

            // Assert
            result.IsError.Should().BeTrue();
            result.FirstError.Description.Should().Be("The user with the specified email was not found.");
        }

        [Fact]
        public async Task LoginAsync_MissingReCaptchaToken_ReturnsError()
        {
            // Arrange
            _mockReCaptchaService
                .Setup(s => s.VerifyReCaptchaAsync(It.IsAny<string>(), "login", CancellationToken.None))
                .ReturnsAsync(ErrorOr<bool>.From(new List<Error>
                {
                    Error.Validation("ReCaptcha.MissingToken", "The reCAPTCHA token is required.")
                }));
            
            var loginRequest = new LoginRequest
            {
                Email = "test@example.com",
                Password = "password",
                ReCaptchaToken = null!,
                Action = "login"
            };

            // Act
            var result = await _accountService.LoginAsync(loginRequest,
                CancellationToken.None);

            // Assert
            result.IsError.Should().BeTrue();
            result.FirstError.Description.Should().Be("The reCAPTCHA token is required.");
        }
    }
}