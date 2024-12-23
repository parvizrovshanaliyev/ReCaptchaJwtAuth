using ErrorOr;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReCaptchaJwtAuth.API.Models;
using ReCaptchaJwtAuth.API.Persistence.Data;

namespace ReCaptchaJwtAuth.API.Services;

public interface IAccountService
{
    Task<ErrorOr<string>> LoginAsync(string email, string password, string recaptchaToken, string action, CancellationToken cancellationToken);
}

public class AccountService : IAccountService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IGoogleReCaptchaService _reCaptchaService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly PasswordHasher<User> _passwordHasher;

    public AccountService(
        ApplicationDbContext dbContext,
        IGoogleReCaptchaService reCaptchaService,
        IJwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _reCaptchaService = reCaptchaService;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = new PasswordHasher<User>();
    }

    public async Task<ErrorOr<string>> LoginAsync(string email, string password, string recaptchaToken, string action, CancellationToken cancellationToken)
    {
        // Step 1: Verify reCAPTCHA
        var reCaptchaResult = await _reCaptchaService.VerifyReCaptchaAsync(recaptchaToken, action, cancellationToken);
        if (reCaptchaResult.IsError)
        {
            return Error.Validation("Recaptcha.Failed", "The reCAPTCHA validation failed.");
        }

        // Step 2: Find the user in the database
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user == null)
        {
            return Error.NotFound("User.NotFound", "User not found.");
        }

        // Step 3: Verify password
        var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (passwordVerificationResult != PasswordVerificationResult.Success)
        {
            return Error.Validation("Credentials.Invalid", "Invalid email or password.");
        }

        // Step 4: Generate JWT token
        var token = _jwtTokenService.GenerateToken(user.Id.ToString(), user.Email);

        return token;
    }
}