using MenuBuilderBack.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuBuilderBack.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {            
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().ToTable("Users");                                
        }

    }
}