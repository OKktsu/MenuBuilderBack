// arquivo: Data/AppDbContextFactory.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MenuBuilderBack.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            
            // Cole sua connection string aqui só para migrations locais
            optionsBuilder.UseNpgsql("Host=db.ohaxnpoaznfccydasljv.supabase.co;Database=postgres;Username=postgres;Password=1WfCnW9cKWJAT9OS;SSL Mode=Require;Trust Server Certificate=true");

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}