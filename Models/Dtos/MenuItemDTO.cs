using System.ComponentModel.DataAnnotations;

namespace MenuBuilderBack.Models.Dtos
{
    public class MenuItemRequestDTO
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public int Order { get; set; }

        [MaxLength(1000)]
        public string? Ingredients { get; set; }

        [Required, Range(0.01, 99999.99)]
        public decimal Price { get; set; }

        public string? ImagePath { get; set; }

        public List<string> Tags { get; set; } = [];

        public List<int> CategoryIds { get; set; } = [];
    }

    public class MenuItemResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Order { get; set; }
        public string? Ingredients { get; set; }
        public decimal Price { get; set; }
        public string? ImagePath { get; set; }
        public List<string> Tags { get; set; } = [];
    }
}
