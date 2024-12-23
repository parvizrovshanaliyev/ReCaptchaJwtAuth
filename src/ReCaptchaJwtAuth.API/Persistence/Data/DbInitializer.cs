using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReCaptchaJwtAuth.API.Models;

namespace ReCaptchaJwtAuth.API.Persistence.Data
{
    public class DbInitializer
    {
        /// <summary>
        /// Initializes the database by applying migrations and seeding default data.
        /// </summary>
        /// <param name="serviceProvider">The service provider for accessing dependencies.</param>
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DbInitializer>>();
            var passwordHasher = new PasswordHasher<User>();

            try
            {
                // Apply pending migrations
                context.Database.Migrate();

                // Seed data if necessary
                SeedUsers(context, logger, passwordHasher);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing the database.");
                throw; // Optionally rethrow to terminate the app on critical failure
            }
        }

        /// <summary>
        /// Seeds the Users table with default data if empty.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="logger">Logger for recording the seeding process.</param>
        /// <param name="passwordHasher">Password hasher to securely hash user passwords.</param>
        private static void SeedUsers(ApplicationDbContext context, ILogger logger, PasswordHasher<User> passwordHasher)
        {
            if (context.Users.Any())
            {
                logger.LogInformation("The Users table already contains data. Skipping seeding.");
                return;
            }

            logger.LogInformation("Seeding default users into the database...");

            var adminUser = new User
            {
                Email = "admin@example.com",
                Role = "Admin"
            };

            // Hash the password
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Admin@123");

            context.Users.Add(adminUser);
            context.SaveChanges();

            logger.LogInformation("Default users have been seeded successfully.");
        }
    }
}
