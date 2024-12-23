using ErrorOr;

namespace ReCaptchaJwtAuth.API.Errors;

public static class ErrorTypes
{
    public static Error InvalidReCaptcha => Error.Validation(
        code: "ReCaptcha.Invalid",
        description: "The reCAPTCHA validation failed.");

    public static Error InvalidCredentials => Error.Validation(
        code: "Auth.InvalidCredentials",
        description: "Invalid email or password.");
}