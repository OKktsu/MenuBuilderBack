using System.ComponentModel.DataAnnotations;

namespace MenuBuilderBack.Models.Dtos
{
    public class EmpresaRequestDTO
    {
        [Required, MaxLength(150)]
        public required string Nome { get; set; }

        /// <summary>Base64 de nova imagem, URL já salva, ou null para não alterar.</summary>
        public string? LogoUrl { get; set; }
    }

    public class EmpresaResponseDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string CodigoConvite { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int? EmpresaMaeId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SlugUpdateDTO
    {
        [Required, MaxLength(100), RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$",
            ErrorMessage = "Slug deve conter apenas letras minúsculas, números e hífens.")]
        public required string Slug { get; set; }
    }

    public class EmpresaFilhaRequestDTO
    {
        [Required, MaxLength(150)]
        public required string Nome { get; set; }

        public string? LogoUrl { get; set; }
    }

    public class EmpresaArvoreDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public List<EmpresaArvoreDTO> Filhas { get; set; } = [];
    }

    public class MenuItemOverrideRequestDTO
    {
        /// <summary>Novo preço para esta empresa. Null = mantém o preço original.</summary>
        public decimal? Price { get; set; }

        /// <summary>False = oculta o item no cardápio desta empresa.</summary>
        public bool IsActive { get; set; } = true;
    }
}
