using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ReCaptchaJwtAuth.API.Controllers;
using ReCaptchaJwtAuth.API.Models;
using ReCaptchaJwtAuth.API.Services;
using ErrorOr;

namespace ReCaptchaJwtAuth.Tests.Controllers
{
    public class AccountControllerTests
    {
        private readonly Mock<IAccountService> _mockAccountService;
        private readonly AccountController _controller;

        public AccountControllerTests()
        {
            _mockAccountService = new Mock<IAccountService>();
            _controller = new AccountController(_mockAccountService.Object);
        }

        [Fact]
        public async Task Login_ValidRequest_ReturnsToken()
        {
            // Arrange
            var loginRequest = CreateLoginRequest("test@example.com", "password", "valid-token");
            
            

            _mockAccountService
                .Setup(s => s.LoginAsync(
                    loginRequest,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync("valid-jwt-token");

            // Act
            var result = await _controller.Login(loginRequest, CancellationToken.None) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);
            result.Value.Should().BeEquivalentTo(new { Token = "valid-jwt-token" });

            // Verify mock interaction
            VerifyLoginAsyncCall(loginRequest, Times.Once());
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var loginRequest = CreateLoginRequest("test@example.com", "wrong-password", "valid-token");

            _mockAccountService
                .Setup(s => s.LoginAsync(
                    loginRequest,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Error.Validation("Credentials.Invalid", "Invalid email or password."));

            // Act
            var result = await _controller.Login(loginRequest, CancellationToken.None) as ObjectResult;

            // Assert
            ValidateProblemDetails(result, 401, "Invalid email or password.");
            VerifyLoginAsyncCall(loginRequest, Times.Once());
        }

        [Fact]
        public async Task Login_InvalidReCaptcha_ReturnsBadRequest()
        {
            // Arrange
            var loginRequest = CreateLoginRequest("test@example.com", "password", "invalid-token");

            _mockAccountService
                .Setup(s => s.LoginAsync(
                    loginRequest,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Error.Validation("ReCaptcha.Failed", "The reCAPTCHA validation failed."));

            // Act
            var result = await _controller.Login(loginRequest, CancellationToken.None) as ObjectResult;

            // Assert
            ValidateProblemDetails(result, 400, "The reCAPTCHA validation failed.");
            VerifyLoginAsyncCall(loginRequest, Times.Once());
        }

        [Fact]
        public async Task Login_NonexistentUser_ReturnsNotFound()
        {
            // Arrange
            var loginRequest = CreateLoginRequest("nonexistent@example.com", "password", "valid-token");

            _mockAccountService
                .Setup(s => s.LoginAsync(
                    loginRequest,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Error.NotFound("User.NotFound", "The user with the specified email was not found."));

            // Act
            var result = await _controller.Login(loginRequest, CancellationToken.None) as ObjectResult;

            // Assert
            ValidateProblemDetails(result, 404, "The user with the specified email was not found.");
            VerifyLoginAsyncCall(loginRequest, Times.Once());
        }

        [Fact]
        public async Task Login_MissingFields_ReturnsBadRequest()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Email = null, // Missing email
                Password = "password",
                ReCaptchaToken = "valid-token"
            };

            // Act
            var result = await _controller.Login(loginRequest, CancellationToken.None) as ObjectResult;

            // Assert
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(400);

            var problemDetails = result.Value as ValidationProblemDetails;
            problemDetails.Should().NotBeNull();
            problemDetails!.Errors.Should().ContainKey(nameof(LoginRequest.Email));
            problemDetails.Errors[nameof(LoginRequest.Email)].Should().Contain("The Email field is required.");
        }

        // Helper: Create a valid LoginRequest
        private LoginRequest CreateLoginRequest(string email, string password, string recaptchaToken)
        {
            return new LoginRequest
            {
                Email = email,
                Password = password,
                ReCaptchaToken = recaptchaToken,
                Action = "login"
            };
        }

        // Helper: Validate ProblemDetails response
        private static void ValidateProblemDetails(ObjectResult? result, int expectedStatusCode, string expectedTitle)
        {
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(expectedStatusCode);

            var problemDetails = result.Value as ProblemDetails;
            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be(expectedTitle);
            problemDetails.Status.Should().Be(expectedStatusCode);
        }

        // Helper: Verify LoginAsync was called with expected parameters
        private void VerifyLoginAsyncCall(LoginRequest loginRequest, Moq.Times times)
        {
            _mockAccountService.Verify(
                s => s.LoginAsync(
                    loginRequest,
                    It.IsAny<CancellationToken>()),
                times);
        }
    }
}
