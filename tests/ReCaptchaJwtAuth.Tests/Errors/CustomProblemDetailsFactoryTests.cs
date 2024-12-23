using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using FluentAssertions;
using Microsoft.AspNetCore.Http; // Required for DefaultHttpContext
using Xunit;
using ReCaptchaJwtAuth.API.Errors;

namespace ReCaptchaJwtAuth.Tests.Errors
{
    public class CustomProblemDetailsFactoryTests
    {
        private readonly CustomProblemDetailsFactory _factory;

        public CustomProblemDetailsFactoryTests()
        {
            var apiBehaviorOptions = Options.Create(new ApiBehaviorOptions());
            _factory = new CustomProblemDetailsFactory(apiBehaviorOptions);
        }

        private static DefaultHttpContext CreateHttpContext(string path = "/default-path")
        {
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            return context;
        }

        [Fact]
        public void CreateProblemDetails_ValidInputs_ReturnsExpectedProblemDetails()
        {
            // Arrange
            var httpContext = CreateHttpContext("/test-endpoint");
            const int statusCode = (int)HttpStatusCode.BadRequest;
            const string title = "Bad Request";
            const string detail = "Invalid input.";

            // Act
            var problemDetails = _factory.CreateProblemDetails(
                httpContext,
                statusCode: statusCode,
                title: title,
                detail: detail);

            // Assert
            problemDetails.Should().NotBeNull();
            problemDetails.Status.Should().Be(statusCode);
            problemDetails.Title.Should().Be(title);
            problemDetails.Detail.Should().Be(detail);
            problemDetails.Instance.Should().Be("/test-endpoint");
        }

        [Fact]
        public void CreateProblemDetails_NoInputs_ReturnsDefaults()
        {
            // Arrange
            var httpContext = CreateHttpContext();

            // Act
            var problemDetails = _factory.CreateProblemDetails(httpContext);

            // Assert
            problemDetails.Should().NotBeNull();
            problemDetails.Status.Should().Be(500);
            problemDetails.Title.Should().Be("Internal Server Error");
            problemDetails.Detail.Should().BeNull();
            problemDetails.Instance.Should().Be("/default-path");
        }

        [Fact]
        public void CreateValidationProblemDetails_ValidModelState_ReturnsExpectedValidationProblemDetails()
        {
            // Arrange
            var httpContext = CreateHttpContext("/test-validation");
            var modelState = new ModelStateDictionary();
            modelState.AddModelError("Field1", "Field1 is required.");
            modelState.AddModelError("Field2", "Field2 must be a valid email.");

            const int statusCode = (int)HttpStatusCode.BadRequest;
            const string title = "Validation Error";
            const string detail = "There are validation errors.";

            // Act
            var validationProblemDetails = _factory.CreateValidationProblemDetails(
                httpContext,
                modelState,
                statusCode: statusCode,
                title: title,
                detail: detail);

            // Assert
            validationProblemDetails.Should().NotBeNull();
            validationProblemDetails.Status.Should().Be(statusCode);
            validationProblemDetails.Title.Should().Be(title);
            validationProblemDetails.Detail.Should().Be(detail);
            validationProblemDetails.Instance.Should().Be("/test-validation");

            validationProblemDetails.Errors.Should().ContainKey("Field1").WhoseValue.Should().Contain("Field1 is required.");
            validationProblemDetails.Errors.Should().ContainKey("Field2").WhoseValue.Should().Contain("Field2 must be a valid email.");
        }

        [Fact]
        public void CreateValidationProblemDetails_EmptyModelState_ReturnsEmptyErrors()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            var emptyModelState = new ModelStateDictionary();

            // Act
            var validationProblemDetails = _factory.CreateValidationProblemDetails(httpContext, emptyModelState);

            // Assert
            validationProblemDetails.Should().NotBeNull();
            validationProblemDetails.Errors.Should().BeEmpty();
        }

        [Fact]
        public void CreateValidationProblemDetails_NullModelState_ThrowsArgumentNullException()
        {
            // Arrange
            var httpContext = CreateHttpContext();

            // Act
            Action act = () => _factory.CreateValidationProblemDetails(httpContext, null!);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithMessage("*modelStateDictionary*");
        }

        [Fact]
        public void CreateProblemDetails_NullHttpContext_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => _factory.CreateProblemDetails(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithMessage("*httpContext*");
        }

        [Fact]
        public void CreateValidationProblemDetails_NullHttpContext_ThrowsArgumentNullException()
        {
            // Arrange
            var modelState = new ModelStateDictionary();

            // Act
            Action act = () => _factory.CreateValidationProblemDetails(null!, modelState);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithMessage("*httpContext*");
        }
    }
}
