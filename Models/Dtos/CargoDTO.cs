using System.ComponentModel.DataAnnotations;
using MenuBuilderBack.Models.Enums;

namespace MenuBuilderBack.Models.Dtos
{
    public class CargoRequestDTO
    {
        [Required, MaxLength(100)]
        public required string Nome { get; set; }

        public List<Permissao> Permissoes { get; set; } = [];
    }

    public class CargoResponseDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int EmpresaId { get; set; }
        public List<string> Permissoes { get; set; } = [];
        public int TotalFuncionarios { get; set; }
    }
}
