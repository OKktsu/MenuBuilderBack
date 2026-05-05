using MenuBuilderBack.Models.Enums;

namespace MenuBuilderBack.Models
{
    public class CargoPermissao
    {
        public int Id { get; set; }

        public int CargoId { get; set; }
        public Cargo Cargo { get; set; } = null!;

        public Permissao Permissao { get; set; }
    }
}
