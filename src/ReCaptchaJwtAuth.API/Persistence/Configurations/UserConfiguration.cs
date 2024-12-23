using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using ReCaptchaJwtAuth.API.Models;

namespace ReCaptchaJwtAuth.API.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table name
        builder.ToTable("Users");

        // Primary key
        builder.HasKey(u => u.Id);

        // Email
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(u => u.Email)
            .IsUnique(); // Ensure unique email addresses

        // PasswordHash
        builder.Property(u => u.PasswordHash)
            .IsRequired();

        // Role
        builder.Property(u => u.Role)
            .IsRequired()
            .HasMaxLength(50);
    }
}