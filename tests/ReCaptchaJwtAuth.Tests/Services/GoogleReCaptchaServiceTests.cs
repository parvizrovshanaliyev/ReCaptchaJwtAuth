using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using ReCaptchaJwtAuth.API.Models;
using ReCaptchaJwtAuth.API.Services;
using ReCaptchaJwtAuth.API.Settings;
using Xunit;

namespace ReCaptchaJwtAuth.Tests.Services
{
    public class GoogleReCaptchaServiceTests
    {
        private readonly GoogleReCaptchaV3Settings _defaultSettings = new GoogleReCaptchaV3Settings
        {
            Threshold = 0.5m
        };

        /// <summary>
        /// Creates a mock HttpClient with the specified response message.
        /// </summary>
        private static HttpClient CreateMockHttpClient(HttpResponseMessage responseMessage)
        {
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync", // Target the protected method
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            return new HttpClient(mockHttpMessageHandler.Object);
        }

        [Fact]
        public async Task VerifyReCaptchaAsync_ValidToken_ReturnsTrue()
        {
            // Arrange
            var validResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(new GoogleReCaptchaResponse
                {
                    Success = true,
                    Score = 0.9m,
                    Action = "login"
                }))
            };

            var httpClient = CreateMockHttpClient(validResponse);
            var service = new GoogleReCaptchaService(httpClient, Options.Create(_defaultSettings));

            // Act
            var result = await service.VerifyReCaptchaAsync("valid-token", "login");

            // Assert
            result.IsError.Should().BeFalse("because a valid token should pass reCAPTCHA validation");
            result.Value.Should().BeTrue("because the response indicates success with a high enough score");
        }

        [Fact]
        public async Task VerifyReCaptchaAsync_LowScore_ReturnsError()
        {
            // Arrange
            var lowScoreResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(new GoogleReCaptchaResponse
                {
                    Success = true,
                    Score = 0.2m,
                    Action = "login"
                }))
            };

            var httpClient = CreateMockHttpClient(lowScoreResponse);
            var service = new GoogleReCaptchaService(httpClient, Options.Create(_defaultSettings));

            // Act
            var result = await service.VerifyReCaptchaAsync("valid-token", "login");

            // Assert
            result.IsError.Should().BeTrue("because the reCAPTCHA score is below the threshold");
            result.FirstError.Description.Should().Be("The reCAPTCHA validation failed.");
        }

        [Fact]
        public async Task VerifyReCaptchaAsync_InvalidResponse_ReturnsError()
        {
            // Arrange
            var invalidResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest
            };

            var httpClient = CreateMockHttpClient(invalidResponse);
            var service = new GoogleReCaptchaService(httpClient, Options.Create(_defaultSettings));

            // Act
            var result = await service.VerifyReCaptchaAsync("invalid-token", "login");

            // Assert
            result.IsError.Should().BeTrue("because the HTTP response indicates a client error");
            result.FirstError.Description.Should().Be("The reCAPTCHA validation failed.");
        }

        [Fact]
        public async Task VerifyReCaptchaAsync_MissingToken_ReturnsError()
        {
            // Arrange
            var httpClient = CreateMockHttpClient(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

            var service = new GoogleReCaptchaService(httpClient, Options.Create(_defaultSettings));

            // Act
            Func<Task> action = async () => await service.VerifyReCaptchaAsync(null, "login");

            // Assert
            await action.Should().ThrowAsync<ArgumentNullException>("because the token cannot be null or empty");
        }
    }
}
