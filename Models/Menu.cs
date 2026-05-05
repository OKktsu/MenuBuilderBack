using MenuBuilderBack.Models.Base;

namespace MenuBuilderBack.Models
{
    public class Menu : BaseEntity
    {
        public string RestaurantName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? OpeningHours { get; set; }

        /// <summary>Quando true, empresas filhas podem herdar este menu (com overrides).</summary>
        public bool Compartilhado { get; set; } = false;

        public int EmpresaId { get; set; }
        public Empresa Empresa { get; set; } = null!;

        public ICollection<Category> Categories { get; set; } = new List<Category>();
    }
}