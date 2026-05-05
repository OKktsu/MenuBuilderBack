using System.ComponentModel.DataAnnotations;

namespace MenuBuilderBack.Models.Dtos
{
    public class ConviteRequestDTO
    {
        [Required, EmailAddress]
        public required string Email { get; set; }

        [Required]
        public int CargoId { get; set; }
    }

    public class ConviteResponseDTO
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public int CargoId { get; set; }
        public string CargoNome { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Retornado pelo endpoint público /info/{token} — sem dados sensíveis.</summary>
    public class ConviteInfoDTO
    {
        public string EmpresaNome { get; set; } = string.Empty;
        public string CargoNome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool Valido { get; set; }
    }
}
