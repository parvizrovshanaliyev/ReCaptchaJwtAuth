using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ReCaptchaJwtAuth.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ErrorController : ControllerBase
{
    private readonly ILogger<ErrorController> _logger;

    public ErrorController(ILogger<ErrorController> logger)
    {
        _logger = logger;
    }

    [HttpGet("/error")]
    public IActionResult HandleError()
    {
        var context = HttpContext.Features.Get<IExceptionHandlerFeature>();
        var exception = context?.Error;

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Detail = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()
                ? exception?.ToString() // Include stack trace in development
                : exception?.Message,   // Simplified message in production
            Instance = HttpContext.Request.Path
        };

        return StatusCode(StatusCodes.Status500InternalServerError, problemDetails);
    }
}
