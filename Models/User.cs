using Microsoft.AspNetCore.Identity;

namespace MenuBuilderBack.Models
{
    public class User : IdentityUser
    {
        public string NomeCompleto { get; set; }
        public string? FotoUrl { get; set; }
        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;
    }
}