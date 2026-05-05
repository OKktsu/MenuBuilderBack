namespace MenuBuilderBack.Models
{
    /// <summary>
    /// Sobrescrita de um item de menu para uma empresa filha.
    /// Permite ocultar o item (IsActive = false) ou cobrar preço diferente (Price != null).
    /// Imutável após criação — sempre substituído por um novo registro se alterado.
    /// </summary>
    public class MenuItemOverride
    {
        public int Id { get; set; }

        public int EmpresaId { get; set; }
        public Empresa Empresa { get; set; } = null!;

        public int MenuItemId { get; set; }
        public MenuItem MenuItem { get; set; } = null!;

        /// <summary>Preço local. Null = usa o preço original do item.</summary>
        public decimal? Price { get; set; }

        /// <summary>False = item oculto no cardápio desta empresa.</summary>
        public bool IsActive { get; set; } = true;
    }
}
