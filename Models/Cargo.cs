using MenuBuilderBack.Models.Base;

namespace MenuBuilderBack.Models
{
    public class Cargo : BaseEntity
    {
        public string Nome { get; set; } = string.Empty;

        public int EmpresaId { get; set; }
        public Empresa Empresa { get; set; } = null!;

        public ICollection<CargoPermissao> Permissoes { get; set; } = new List<CargoPermissao>();
        public ICollection<User> Usuarios { get; set; } = new List<User>();
    }
}
