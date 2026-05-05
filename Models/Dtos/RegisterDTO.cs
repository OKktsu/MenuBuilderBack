using System.ComponentModel.DataAnnotations;

namespace MenuBuilderBack.Models.Dtos
{
    public class RegisterDTO
    {
        [Required, MaxLength(100)]
        public required string NomeCompleto { get; set; }

        /// <summary>Obrigatório quando ConviteToken é nulo (criação de empresa).</summary>
        [MaxLength(150)]
        public string? NomeEmpresa { get; set; }

        /// <summary>Quando preenchido, o usuário entra na empresa do convite (sem criar uma nova).</summary>
        public string? ConviteToken { get; set; }

        [Required, EmailAddress]
        public required string Email { get; set; }

        [Required, MinLength(6)]
        public required string Password { get; set; }
    }
}
