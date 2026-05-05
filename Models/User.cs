using Microsoft.AspNetCore.Identity;

namespace MenuBuilderBack.Models
{
    public class User : IdentityUser
    {
        public string NomeCompleto { get; set; } = string.Empty;
        public string? FotoUrl { get; set; }
        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

        /// <summary>true = dono da empresa (acesso total); false = funcionário com cargo definido.</summary>
        public bool IsOwner { get; set; } = false;

        public int? EmpresaId { get; set; }
        public Empresa? Empresa { get; set; }

        /// <summary>Null quando IsOwner = true.</summary>
        public int? CargoId { get; set; }
        public Cargo? Cargo { get; set; }
    }
}