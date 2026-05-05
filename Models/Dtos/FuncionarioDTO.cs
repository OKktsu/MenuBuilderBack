using System.ComponentModel.DataAnnotations;

namespace MenuBuilderBack.Models.Dtos
{
    public class FuncionarioResponseDTO
    {
        public string UserId { get; set; } = string.Empty;
        public string NomeCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsOwner { get; set; }
        public int? CargoId { get; set; }
        public string? CargoNome { get; set; }
        public DateTime DataEntrada { get; set; }
    }

    public class AlterarCargoDTO
    {
        [Required]
        public int CargoId { get; set; }
    }
}
