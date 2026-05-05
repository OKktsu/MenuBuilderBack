namespace MenuBuilderBack.Models.Dtos
{
    // Projeção mínima e segura — nunca expõe IDs internos, dados de funcionários ou hierarquia.

    public class PublicRestaurantInfoDTO
    {
        public string RestaurantName { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? OpeningHours { get; set; }
    }

    public class PublicMenuResponseDTO
    {
        public string RestaurantName { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public List<PublicMenuDTO> Menus { get; set; } = [];
    }

    public class PublicMenuDTO
    {
        public string Name { get; set; } = string.Empty;
        public string? OpeningHours { get; set; }
        public List<PublicCategoryDTO> Categories { get; set; } = [];
    }

    public class PublicCategoryDTO
    {
        public string Name { get; set; } = string.Empty;
        public List<PublicMenuItemDTO> Items { get; set; } = [];
    }

    public class PublicMenuItemDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Ingredients { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public List<string> Tags { get; set; } = [];
    }
}
