using AuthAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthAPI.Data
{
    // DbContext is like a bridge between your C# code and the database
    public class AppDbContext : DbContext
    {
        // Constructor - this gets called when creating the context
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSet represents a table in the database
        // Users table will contain User records
        public DbSet<User> Users { get; set; }

        // Optional: Configure database rules here
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Make Email unique (no duplicate emails)
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}