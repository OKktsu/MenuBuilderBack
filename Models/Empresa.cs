using MenuBuilderBack.Models.Base;

namespace MenuBuilderBack.Models
{
    public class Empresa : BaseEntity
    {
        public string Nome { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }

        /// <summary>Código único gerado na criação — usado para convites por código.</summary>
        public string CodigoConvite { get; set; } = string.Empty;

        /// <summary>Identificador público único — usado na URL do cardápio (/menu/{slug}).</summary>
        public string Slug { get; set; } = string.Empty;

        /// <summary>FK para a empresa mãe. Null = empresa raiz (sem hierarquia acima).</summary>
        public int? EmpresaMaeId { get; set; }
        public Empresa? EmpresaMae { get; set; }
        public ICollection<Empresa> Filhas { get; set; } = new List<Empresa>();

        public ICollection<User> Funcionarios { get; set; } = new List<User>();
        public ICollection<Cargo> Cargos { get; set; } = new List<Cargo>();
        public ICollection<Menu> Menus { get; set; } = new List<Menu>();
        public ICollection<ConviteEmpresa> Convites { get; set; } = new List<ConviteEmpresa>();
    }
}
