using Microsoft.EntityFrameworkCore;
using ReCaptchaJwtAuth.API.Persistence.Data;

namespace ReCaptchaJwtAuth.Tests.Setups
{
    public static class DatabaseContextSetup
    {
        /// <summary>
        /// Creates an ApplicationDbContext with an in-memory database for testing.
        /// Each test gets a unique database instance to avoid state interference.
        /// </summary>
        /// <returns>An ApplicationDbContext instance backed by an in-memory database.</returns>
        public static ApplicationDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB for each test
                .Options;

            var context = new ApplicationDbContext(options);

            // Seed the database with common data if necessary
            SeedData(context);

            return context;
        }

        /// <summary>
        /// Seeds the database with initial data for testing purposes.
        /// </summary>
        /// <param name="context">The ApplicationDbContext to seed.</param>
        private static void SeedData(ApplicationDbContext context)
        {
            // Ensure the database is created
            context.Database.EnsureCreated();

            // Add initial test data if required
            // Example: Adding an Admin user
            var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<ReCaptchaJwtAuth.API.Models.User>();
            var adminUser = new ReCaptchaJwtAuth.API.Models.User
            {
                Email = "admin@example.com",
                PasswordHash = passwordHasher.HashPassword(null, "Admin@123"),
                Role = "Admin"
            };

            if (!context.Users.Any(u => u.Email == adminUser.Email))
            {
                context.Users.Add(adminUser);
                context.SaveChanges();
            }
        }
    }
}