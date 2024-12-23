using Microsoft.AspNetCore.Mvc;
using ReCaptchaJwtAuth.API.Errors;
using ReCaptchaJwtAuth.API.Services;
using ReCaptchaJwtAuth.API.Models;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Identity;

namespace ReCaptchaJwtAuth.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        request.Action = "login";
        
        var result = await _accountService.LoginAsync(request, cancellationToken);

        if (result.IsError)
        {
            var firstError = result.FirstError;
            return Problem(statusCode: 400, title: firstError.Description);
        }

        return Ok(new { Token = result.Value });
    }
}

