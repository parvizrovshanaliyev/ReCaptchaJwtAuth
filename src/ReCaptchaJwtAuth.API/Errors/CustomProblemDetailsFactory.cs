using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace ReCaptchaJwtAuth.API.Errors;
/// <summary>
/// A custom implementation of ProblemDetailsFactory for generating Problem Details responses in a standardized way.
/// </summary>
public class CustomProblemDetailsFactory : ProblemDetailsFactory
{
    private readonly ApiBehaviorOptions _options;

    /// <summary>
    /// Initializes a new instance of CustomProblemDetailsFactory.
    /// </summary>
    /// <param name="options">API behavior options for default error mappings.</param>
    public CustomProblemDetailsFactory(IOptions<ApiBehaviorOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Creates a standard ProblemDetails response.
    /// </summary>
    public override ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null)
    {
        statusCode ??= 500;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title ?? GetDefaultTitle(statusCode.Value),
            Type = type ?? GetDefaultType(statusCode.Value),
            Detail = detail,
            Instance = instance ?? httpContext.Request.Path
        };

        ApplyDefaults(httpContext, problemDetails);

        return problemDetails;
    }

    public override ValidationProblemDetails CreateValidationProblemDetails(
        HttpContext httpContext,
        ModelStateDictionary modelStateDictionary,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null)
    {
        var validationProblemDetails = new ValidationProblemDetails(modelStateDictionary)
        {
            Status = statusCode ?? 400,
            Title = title ?? "Validation Error",
            Type = type ?? "https://httpstatuses.com/400",
            Detail = detail,
            Instance = instance ?? httpContext.Request.Path
        };

        validationProblemDetails.Extensions["timestamp"] = DateTime.UtcNow;
        validationProblemDetails.Extensions["docs"] = "https://example.com/api-docs/errors#validation";

        ApplyDefaults(httpContext, validationProblemDetails);

        return validationProblemDetails;
    }



    private void ApplyDefaults(HttpContext httpContext, ProblemDetails problemDetails)
    {
        problemDetails.Instance ??= httpContext.Request.Path;

        if (problemDetails.Status is null)
        {
            problemDetails.Status = 500;
        }

        if (_options.ClientErrorMapping.TryGetValue(problemDetails.Status.Value, out var clientErrorData))
        {
            problemDetails.Title ??= clientErrorData.Title;
            problemDetails.Type ??= clientErrorData.Link;
        }

        // Add a correlation ID to help with distributed tracing
        if (!problemDetails.Extensions.ContainsKey("correlationId"))
        {
            problemDetails.Extensions["correlationId"] = httpContext.TraceIdentifier;
        }
    }

    /// <summary>
    /// Provides default titles for common HTTP status codes.
    /// </summary>
    private static string GetDefaultTitle(int statusCode)
    {
        return statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            500 => "Internal Server Error",
            _ => "An Error Occurred"
        };
    }

    /// <summary>
    /// Provides default types (links) for common HTTP status codes.
    /// </summary>
    private static string GetDefaultType(int statusCode)
    {
        return statusCode switch
        {
            400 => "https://httpstatuses.com/400",
            401 => "https://httpstatuses.com/401",
            403 => "https://httpstatuses.com/403",
            404 => "https://httpstatuses.com/404",
            500 => "https://httpstatuses.com/500",
            _ => "https://httpstatuses.com/unknown"
        };
    }
}
