namespace ReCaptchaJwtAuth.API.Models;

public class User
{
    public int Id { get; set; } // Primary Key
    public string Email { get; set; } = string.Empty; // Unique Email
    public string PasswordHash { get; set; } = string.Empty; // Hashed Password
    public string Role { get; set; } = string.Empty; // Role (e.g., Admin, User)
}