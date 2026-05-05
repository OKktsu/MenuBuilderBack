using System.ComponentModel.DataAnnotations;

namespace MenuBuilderBack.Models.Dtos
{
    public class CategoryRequestDTO
    {
        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public int Order { get; set; }

        [Required]
        public int MenuId { get; set; }
    }

    public class CategoryResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public int MenuId { get; set; }
        public List<MenuItemResponseDTO> Items { get; set; } = [];
    }

    public class ReorderItemDTO
    {
        public int Id { get; set; }
        public int Order { get; set; }
    }
}
