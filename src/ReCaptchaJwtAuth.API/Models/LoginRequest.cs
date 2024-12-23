namespace ReCaptchaJwtAuth.API.Models
{
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ReCaptchaToken { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }
}
