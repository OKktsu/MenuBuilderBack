using MenuBuilderBack.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MenuBuilderBack.Tests.Infrastructure
{
    /// <summary>
    /// Sobe a aplicação real em memória para testes de integração.
    /// Substitui o banco PostgreSQL por um banco InMemory isolado por teste.
    /// </summary>
    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Fornece configurações mínimas para a aplicação inicializar sem segredos reais
            builder.UseSetting("ConnectionStrings:SupabaseConnection", "Host=test;Database=test");
            builder.UseSetting("JwtSettings:SecretKey", "dGVzdGVjaGF2ZXRlbXBvcmFyaWE=");

            builder.ConfigureServices(services =>
            {
                // Remove todos os descritores que registram o AppDbContext
                // (inclui o registro de opções do Npgsql e o próprio contexto)
                var toRemove = services
                    .Where(d =>
                        d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                        d.ServiceType == typeof(DbContextOptions)               ||
                        d.ServiceType == typeof(AppDbContext))
                    .ToList();

                foreach (var d in toRemove)
                    services.Remove(d);

                // Registra o AppDbContext diretamente via factory, ignorando o pipeline
                // de configuração de opções do Npgsql que possa ter sobrado no container.
                services.AddScoped<AppDbContext>(_ =>
                {
                    var options = new DbContextOptionsBuilder<AppDbContext>()
                        .UseInMemoryDatabase(_dbName)
                        .Options;
                    return new AppDbContext(options);
                });
            });
        }

        /// <summary>Cria um escopo e executa uma ação no banco de testes — útil para semear dados.</summary>
        public void SeedDatabase(Action<AppDbContext> seed)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            seed(db);
        }
    }
}
