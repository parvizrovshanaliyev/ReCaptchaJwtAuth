using Microsoft.EntityFrameworkCore;
using ReCaptchaJwtAuth.API.Models;

namespace ReCaptchaJwtAuth.API.Persistence.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
    }
}
