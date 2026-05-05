using System.ComponentModel.DataAnnotations;

namespace MenuBuilderBack.Models.Dtos
{
    public class MenuRequestDTO
    {
        [Required, MaxLength(100)]
        public string RestaurantName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(200)]
        public string? OpeningHours { get; set; }
    }

    public class MenuResponseDTO
    {
        public int Id { get; set; }
        public string RestaurantName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? OpeningHours { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<CategoryResponseDTO> Categories { get; set; } = [];
    }
}
